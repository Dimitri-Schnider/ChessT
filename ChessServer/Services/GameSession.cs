// File: [SolutionDir]\ChessServer\Services\GameSession.cs
using ChessLogic;
using ChessLogic.Moves;
using ChessLogic.Utilities;
using ChessNetwork.Configuration;
using ChessNetwork.DTOs;
using ChessServer.Hubs;
using ChessServer.Services.CardEffects;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Chess.Logging;
using Microsoft.Extensions.Logging;


namespace ChessServer.Services
{
    public sealed class GameSession : IDisposable
    {
        private readonly GameState _state;
        private readonly Dictionary<Guid, (string Name, Player Color)> _players = new();
        private Player _firstPlayerActualColor;
        private Guid _firstPlayerId = Guid.Empty;
        private Guid? _playerWhiteId;
        private Guid? _playerBlackId;
        private readonly Guid _gameIdInternal;
        private readonly IHubContext<ChessHub> _hubContext;
        private readonly IChessLogger _logger;
        private readonly GameTimerService _timerServiceInternal;
        private readonly List<PlayedMoveDto> _playedMoves = new List<PlayedMoveDto>();
        private int _moveCounter;
        private readonly GameHistoryDto _gameHistory;
        private readonly Dictionary<Guid, HashSet<string>> _usedGlobalCardsPerPlayer = new();
        private readonly Dictionary<Guid, string?> _pendingCardEffectForNextMove = new();
        private readonly Dictionary<Guid, int> _playerMoveCounts = new();
        private readonly List<PlayedCardDto> _playedCardsHistory = new List<PlayedCardDto>();
        private readonly Dictionary<string, ICardEffect> _cardEffects;
        private readonly ILoggerFactory _loggerFactoryForEffects;
        private readonly SemaphoreSlim _activateCardSemaphore = new SemaphoreSlim(1, 1);
        private readonly Dictionary<Player, List<PieceType>> _capturedPieces = new()
        {
            { Player.White, new List<PieceType>() },
            { Player.Black, new List<PieceType>() }
        };
        private readonly List<CardDto> _allCardDefinitions;
        private readonly Dictionary<Guid, List<CardDto>> _playerDrawPiles = new Dictionary<Guid, List<CardDto>>();
        private readonly Dictionary<Guid, List<CardDto>> _playerHands = new Dictionary<Guid, List<CardDto>>();
        private readonly Random _random = new Random();
        private const int InitialHandSize = 3;
        private readonly object _sessionLock = new object();

        public GameState CurrentGameState => _state;
        public GameTimerService TimerService => _timerServiceInternal;
        public Guid GameId => _gameIdInternal;

        public int PlayerCount
        {
            get
            {
                lock (_sessionLock)
                {
                    return _players.Count;
                }
            }
        }
        public Guid FirstPlayerId
        {
            get
            {
                lock (_sessionLock)
                {
                    return _firstPlayerId;
                }
            }
        }
        public Player FirstPlayerColor
        {
            get
            {
                lock (_sessionLock)
                {
                    return _firstPlayerActualColor;
                }
            }
        }
        public bool HasOpponent
        {
            get
            {
                lock (_sessionLock)
                {
                    return _players.Count > 1;
                }
            }
        }

        public GameSession(Guid gameId, Player initialCreatorColorPreference, string creatorName, int initialMinutes, IHubContext<ChessHub> hubContext, IChessLogger logger, ILoggerFactory loggerFactory)
        {
            _gameIdInternal = gameId;
            _firstPlayerActualColor = initialCreatorColorPreference;
            _state = new GameState(Player.White, Board.Initial());
            _hubContext = hubContext;
            _logger = logger;
            _loggerFactoryForEffects = loggerFactory;
            _timerServiceInternal = new GameTimerService(gameId, TimeSpan.FromMinutes(initialMinutes), loggerFactory.CreateLogger<GameTimerService>());
            _timerServiceInternal.OnTimeUpdated += HandleTimeUpdated;
            _timerServiceInternal.OnTimeExpired += HandleTimeExpired;
            _gameHistory = new GameHistoryDto { GameId = _gameIdInternal, InitialTimeMinutes = initialMinutes, DateTimeStartedUtc = DateTime.UtcNow };
            _allCardDefinitions = new List<CardDto>
            {
                new() { InstanceId = Guid.Empty, Id = CardConstants.ExtraZug, Name = "Extrazug", Description = "Du darfst sofort einen weiteren Schachzug ausführen. (Einmal pro Spiel)", ImageUrl = "img/cards/art/1-Extrazug_Art.png" },
                new() { InstanceId = Guid.Empty, Id = CardConstants.Teleport, Name = "Teleportation", Description = "Eine eigene Figur darf auf ein beliebiges leeres Feld auf dem Schachbrett gestellt werden.", ImageUrl = "img/cards/art/2-Teleportation_Art.png" },
                new() { InstanceId = Guid.Empty, Id = CardConstants.Positionstausch, Name = "Positionstausch", Description = "Zwei eigene Figuren tauschen ihre Plätze.", ImageUrl = "img/cards/art/3-Positionstausch_Art.png" },
                new() { InstanceId = Guid.Empty, Id = CardConstants.AddTime, Name = "Zeitgutschrift", Description = "Fügt deiner Bedenkzeit 2 Minuten hinzu.", ImageUrl = "img/cards/art/11-AddTime_Art.png" },
                new() { InstanceId = Guid.Empty, Id = CardConstants.SubtractTime, Name = "Zeitdiebstahl", Description = "Zieht der gegnerischen Bedenkzeit 2 Minuten ab (minimal 1 Minute Restzeit).", ImageUrl = "img/cards/art/12-SubtractTime_Art.png" },
                new() { InstanceId = Guid.Empty, Id = CardConstants.TimeSwap, Name = "Zeittausch", Description = "Tauscht die aktuellen Restbedenkzeiten mit deinem Gegner (minimal 1 Minute Restzeit für jeden).", ImageUrl = "img/cards/art/13-Zeittausch_Art.png" },
                new() { InstanceId = Guid.Empty, Id = CardConstants.Wiedergeburt, Name = "Wiedergeburt", Description = "Eine eigene, geschlagene Nicht-Bauern-Figur wird auf einem ihrer ursprünglichen Startfelder wiederbelebt. Ist das gewählte Feld besetzt, schlägt der Effekt fehl und die Karte ist verbraucht.", ImageUrl = "img/cards/art/5-Wiedergeburt_Art.png" },
                new() { InstanceId = Guid.Empty, Id = CardConstants.CardSwap, Name = "Kartentausch", Description = "Wähle eine deiner Handkarten. Diese wird mit einer zufälligen Handkarte deines Gegners getauscht. Hat der Gegner keine Karten, verfällt deine Karte ohne Effekt.", ImageUrl = "img/cards/art/14-Kartentausch_Art.png" },
                new() { InstanceId = Guid.Empty, Id = CardConstants.SacrificeEffect, Name = "Opfergabe", Description = "Wähle einen eigenen Bauern. Entferne ihn vom Spiel. Du darfst sofort eine neue Karte ziehen.", ImageUrl = "img/cards/art/15-Opergabe_Art.png" }
            };

            _cardEffects = new Dictionary<string, ICardEffect>
            {
                { CardConstants.ExtraZug, new ExtraZugEffect(new Chess.Logging.ChessLogger<ExtraZugEffect>(_loggerFactoryForEffects.CreateLogger<ExtraZugEffect>())) },
                { CardConstants.Teleport, new TeleportEffect(new Chess.Logging.ChessLogger<TeleportEffect>(_loggerFactoryForEffects.CreateLogger<TeleportEffect>())) },
                { CardConstants.Positionstausch, new PositionSwapEffect(new Chess.Logging.ChessLogger<PositionSwapEffect>(_loggerFactoryForEffects.CreateLogger<PositionSwapEffect>())) },
                { CardConstants.AddTime, new AddTimeEffect(new Chess.Logging.ChessLogger<AddTimeEffect>(_loggerFactoryForEffects.CreateLogger<AddTimeEffect>())) },
                { CardConstants.SubtractTime, new SubtractTimeEffect(new Chess.Logging.ChessLogger<SubtractTimeEffect>(_loggerFactoryForEffects.CreateLogger<SubtractTimeEffect>())) },
                { CardConstants.TimeSwap, new TimeSwapEffect(new Chess.Logging.ChessLogger<TimeSwapEffect>(_loggerFactoryForEffects.CreateLogger<TimeSwapEffect>())) },
                { CardConstants.Wiedergeburt, new RebirthEffect(new Chess.Logging.ChessLogger<RebirthEffect>(_loggerFactoryForEffects.CreateLogger<RebirthEffect>())) },
                { CardConstants.CardSwap, new CardSwapEffect(new Chess.Logging.ChessLogger<CardSwapEffect>(_loggerFactoryForEffects.CreateLogger<CardSwapEffect>())) },
                { CardConstants.SacrificeEffect, new SacrificeEffect(new Chess.Logging.ChessLogger<SacrificeEffect>(_loggerFactoryForEffects.CreateLogger<SacrificeEffect>())) }
            };
        }

        public CardDto? GetCardDefinitionForAnimation(string cardTypeId)
        {
            var definition = _allCardDefinitions.FirstOrDefault(c => c.Id == cardTypeId);
            if (definition != null)
            {
                return new CardDto
                {
                    InstanceId = Guid.NewGuid(),
                    Id = definition.Id,
                    Name = definition.Name,
                    Description = definition.Description,
                    ImageUrl = definition.ImageUrl
                };
            }
            _logger.LogClientCriticalServicesNullOnInit($"[GameSession] GetCardDefinitionForAnimation: Kartendefinition für ID '{cardTypeId}' nicht gefunden.");
            return null;
        }

        public bool RemoveCardFromPlayerHand(Guid playerId, Guid cardInstanceIdToRemove)
        {
            lock (_sessionLock)
            {
                if (_playerHands.TryGetValue(playerId, out var hand))
                {
                    var cardInstance = hand.FirstOrDefault(c => c.InstanceId == cardInstanceIdToRemove);
                    if (cardInstance != null)
                    {
                        bool removed = hand.Remove(cardInstance);
                        return removed;
                    }
                }
                _logger.LogCardInstanceNotFoundInHand(cardInstanceIdToRemove, playerId, _gameIdInternal.ToString());
                return false;
            }
        }

        public bool AddCardToPlayerHand(Guid playerId, CardDto cardToAdd)
        {
            lock (_sessionLock)
            {
                if (!_playerHands.TryGetValue(playerId, out var hand))
                {
                    _logger.LogDrawAttemptUnknownPlayer(playerId, _gameIdInternal);
                    hand = new List<CardDto>();
                    _playerHands[playerId] = hand;
                }
                hand.Add(cardToAdd);
                _logger.LogPlayerDrewCardFromOwnDeck(playerId, cardToAdd.Name, cardToAdd.Id.ToString(), _gameIdInternal, _playerDrawPiles.TryGetValue(playerId, out var pile) ? pile.Count : 0);
                return true;
            }
        }

        private void InitializeAndShufflePlayerDeck(Guid playerId)
        {
            var newDeckForPlayer = _allCardDefinitions.Select(cardDef => new CardDto
            {
                InstanceId = Guid.NewGuid(),
                Id = cardDef.Id,
                Name = cardDef.Name,
                Description = cardDef.Description,
                ImageUrl = cardDef.ImageUrl
            }).ToList();
            int n = newDeckForPlayer.Count;
            while (n > 1)
            {
                n--;
                int k = _random.Next(n + 1);
                (newDeckForPlayer[k], newDeckForPlayer[n]) = (newDeckForPlayer[n], newDeckForPlayer[k]);
            }
            _playerDrawPiles[playerId] = newDeckForPlayer;
            _logger.LogPlayerDeckInitialized(playerId, _gameIdInternal, newDeckForPlayer.Count);
        }

        public CardDto? DrawCardForPlayer(Guid playerId)
        {
            lock (_sessionLock)
            {
                if (!_players.ContainsKey(playerId))
                {
                    _logger.LogDrawAttemptUnknownPlayer(playerId, _gameIdInternal);
                    return null;
                }

                if (!_playerDrawPiles.TryGetValue(playerId, out var specificDrawPile))
                {
                    _logger.LogNoDrawPileForPlayer(playerId, _gameIdInternal);
                    InitializeAndShufflePlayerDeck(playerId);
                    _playerDrawPiles.TryGetValue(playerId, out specificDrawPile);
                    if (specificDrawPile == null) return new CardDto { InstanceId = Guid.NewGuid(), Id = $"{CardConstants.FallbackCardIdPrefix}error", Name = "Fehler", Description = "Deck nicht initialisiert.", ImageUrl = "" };
                }

                if (specificDrawPile.Count == 0)
                {
                    _logger.LogPlayerDrawPileEmpty(playerId, _gameIdInternal);
                    return new CardDto { InstanceId = Guid.NewGuid(), Id = $"{CardConstants.FallbackCardIdPrefix}empty_{playerId}", Name = CardConstants.NoMoreCardsName, Description = "Dein Nachziehstapel ist leer.", ImageUrl = CardConstants.DefaultCardBackImageUrl };
                }

                CardDto drawnCard = specificDrawPile.First();
                specificDrawPile.RemoveAt(0);
                if (!_playerHands.TryGetValue(playerId, out var hand))
                {
                    hand = new List<CardDto>();
                    _playerHands[playerId] = hand;
                }
                hand.Add(drawnCard);
                _logger.LogPlayerDrewCardFromOwnDeck(playerId, drawnCard.Name, drawnCard.Id.ToString(), _gameIdInternal, specificDrawPile.Count);
                return drawnCard;
            }
        }

        public int GetDrawPileCount(Guid playerId)
        {
            lock (_sessionLock)
            {
                if (_playerDrawPiles.TryGetValue(playerId, out var specificDrawPile))
                {
                    return specificDrawPile.Count;
                }
                _logger.LogCannotFindPlayerDrawPileForCount(playerId, _gameIdInternal);
                return 0;
            }
        }

        public List<CardDto> GetPlayerHand(Guid playerId)
        {
            lock (_sessionLock)
            {
                if (_playerHands.TryGetValue(playerId, out var hand))
                {
                    return new List<CardDto>(hand);
                }
                return new List<CardDto>();
            }
        }

        public (Guid PlayerId, Player Color) Join(string playerName, Player? preferredColor = null)
        {
            Guid newPlayerId = Guid.NewGuid();
            Player assignedColor = Join(newPlayerId, playerName, preferredColor);
            return (newPlayerId, assignedColor);
        }
        public Player Join(Guid playerId, string playerName, Player? preferredColorForCreator = null)
        {
            Player assignedColor;
            lock (_sessionLock)
            {
                if (_players.TryGetValue(playerId, out var existingPlayerData))
                {
                    assignedColor = existingPlayerData.Color;
                }
                else
                {
                    if (_players.Count >= 2)
                    {
                        throw new InvalidOperationException("Spiel ist bereits voll.");
                    }

                    if (_players.Count == 0)
                    {
                        assignedColor = preferredColorForCreator ?? _firstPlayerActualColor;
                        _firstPlayerActualColor = assignedColor;
                        _firstPlayerId = playerId;

                        if (assignedColor == Player.White) { _gameHistory.PlayerWhiteName = playerName; _playerWhiteId = playerId; }
                        else { _gameHistory.PlayerBlackName = playerName; _playerBlackId = playerId; }
                    }
                    else
                    {
                        assignedColor = _firstPlayerActualColor.Opponent();
                        if (GetPlayerIdByColor(assignedColor) == null)
                        {
                            if (assignedColor == Player.White) { _gameHistory.PlayerWhiteName = playerName; _playerWhiteId = playerId; }
                            else { _gameHistory.PlayerBlackName = playerName; _playerBlackId = playerId; }
                        }
                        else
                        {
                            string? existingPlayerName = assignedColor == Player.White ? _gameHistory.PlayerWhiteName : _gameHistory.PlayerBlackName;
                            throw new InvalidOperationException($"Farbe {assignedColor} ist bereits durch Spieler {existingPlayerName ?? "Unbekannt"} belegt.");
                        }
                    }
                    _players[playerId] = (playerName, assignedColor);
                    _playerMoveCounts[playerId] = 0;
                    InitializeAndShufflePlayerDeck(playerId);
                    if (!_playerHands.ContainsKey(playerId))
                    {
                        _playerHands[playerId] = new List<CardDto>();
                        for (int i = 0; i < InitialHandSize; i++)
                        {
                            DrawCardForPlayer(playerId);
                        }
                    }
                    if (!_capturedPieces.ContainsKey(assignedColor))
                    {
                        _capturedPieces[assignedColor] = new List<PieceType>();
                    }
                }
            }
            return assignedColor;
        }

        public async Task<ServerCardActivationResultDto> ActivateCard(Guid playerId, ActivateCardRequestDto dto)
        {
            Guid cardInstanceId = dto.CardInstanceId;
            string cardTypeId = dto.CardTypeId;
            Guid? cardInstanceIdToSwapFromHand = dto.CardInstanceIdToSwapFromHand;

            await _activateCardSemaphore.WaitAsync();
            bool timerWasManuallyPaused = false;
            CardDto? newlyDrawnCardByEffect = null;
            CardDto? cardGivenForSwapEffectResult = null;
            CardDto? cardReceivedForSwapEffectResult = null;
            try
            {
                _logger.LogSessionCardActivationAttempt(_gameIdInternal, playerId, cardTypeId);
                Player playerDataColor;
                string playerOriginalName;
                bool playerWasInCheckBeforeCard;

                lock (_sessionLock)
                {
                    if (!_players.TryGetValue(playerId, out var pData))
                    {
                        _logger.LogSessionCardActivationFailed(_gameIdInternal, playerId, cardTypeId, "Spieler nicht gefunden.");
                        _activateCardSemaphore.Release();
                        return new ServerCardActivationResultDto { Success = false, ErrorMessage = "Spieler nicht in dieser Session.", CardId = cardTypeId };
                    }
                    playerDataColor = pData.Color;
                    playerOriginalName = pData.Name;
                    playerWasInCheckBeforeCard = _state.Board.IsInCheck(playerDataColor);

                    if (playerWasInCheckBeforeCard)
                    {
                        _logger.LogPlayerAttemptingCardWhileInCheck(_gameIdInternal, playerId, playerDataColor, cardTypeId);
                    }
                }

                CardDto? playedCardInstance = null;
                lock (_sessionLock)
                {
                    if (_playerHands.TryGetValue(playerId, out var hand))
                    {
                        playedCardInstance = hand.FirstOrDefault(c => c.InstanceId == cardInstanceId);
                    }
                }

                if (playedCardInstance == null || playedCardInstance.Id != cardTypeId)
                {
                    _logger.LogCardInstanceNotFoundInHand(cardInstanceId, playerId, _gameIdInternal.ToString());
                    _activateCardSemaphore.Release();
                    return new ServerCardActivationResultDto { Success = false, ErrorMessage = "Gespielte Karte (Instanz oder Typ) nicht auf der Hand des Spielers oder inkonsistent.", CardId = cardTypeId };
                }

                if (playerDataColor != _state.CurrentPlayer)
                {
                    _logger.LogSessionCardActivationFailed(_gameIdInternal, playerId, cardTypeId, "Spieler ist nicht am Zug.");
                    _activateCardSemaphore.Release();
                    return new ServerCardActivationResultDto { Success = false, ErrorMessage = "Nicht dein Zug.", CardId = cardTypeId };
                }
                if (!_cardEffects.TryGetValue(cardTypeId, out var effect))
                {
                    _logger.LogSessionCardActivationFailed(_gameIdInternal, playerId, cardTypeId, "Unbekannte oder nicht serverseitig implementierte Karte.");
                    _activateCardSemaphore.Release();
                    return new ServerCardActivationResultDto { Success = false, ErrorMessage = "Unbekannte oder nicht implementierte Karte.", CardId = cardTypeId };
                }
                if (!_state.IsGameOver() && _state.CurrentPlayer == playerDataColor && !_timerServiceInternal.IsPaused)
                {
                    _timerServiceInternal.PauseTimer();
                    timerWasManuallyPaused = true;
                }

                string? param1ForEffect;
                string? param2ForEffect;

                if (cardTypeId == CardConstants.Wiedergeburt)
                {
                    param1ForEffect = dto.PieceTypeToRevive?.ToString();
                    param2ForEffect = dto.TargetRevivalSquare;
                }
                else if (cardTypeId == CardConstants.CardSwap)
                {
                    param1ForEffect = dto.CardInstanceIdToSwapFromHand?.ToString();
                    param2ForEffect = null;
                }
                else // Gilt auch für Opfergabe, die FromSquare in param1ForEffect erwartet
                {
                    param1ForEffect = dto.FromSquare;
                    param2ForEffect = dto.ToSquare;
                }

                // *** NEUE VALIDIERUNG für Opfergabe ***
                if (cardTypeId == CardConstants.SacrificeEffect)
                {
                    if (string.IsNullOrEmpty(param1ForEffect)) // param1ForEffect sollte FromSquare (Bauernposition) sein
                    {
                        _logger.LogSessionCardActivationFailed(_gameIdInternal, playerId, cardTypeId, "FromSquare (Bauernposition) für Opfergabe nicht angegeben.");
                        _activateCardSemaphore.Release();
                        return new ServerCardActivationResultDto { Success = false, ErrorMessage = "Bauernposition für Opfergabe nicht angegeben.", CardId = cardTypeId };
                    }
                    try
                    {
                        GameSession.ParsePos(param1ForEffect); // Versuche zu parsen, wirft Exception bei ungültigem Format
                    }
                    catch (ArgumentException)
                    {
                        // Log-Meldung für diesen Fehler ist schon in SacrificeEffect, aber hier nochmal für Kontext
                        _logger.LogSessionCardActivationFailed(_gameIdInternal, playerId, cardTypeId, $"Ungültiges FromSquare-Format für Opfergabe: {param1ForEffect}");
                        _activateCardSemaphore.Release();
                        return new ServerCardActivationResultDto { Success = false, ErrorMessage = $"Ungültiges Format für Bauernposition: {param1ForEffect}", CardId = cardTypeId };
                    }
                }
                // *** ENDE NEUE VALIDIERUNG ***


                CardActivationResult effectResult = effect.Execute(this, playerId, playerDataColor, cardTypeId, param1ForEffect, param2ForEffect);

                if (effectResult.Success && playedCardInstance != null) // Sicherstellen, dass playedCardInstance nicht null ist
                {
                    RemoveCardFromPlayerHand(playerId, cardInstanceId);
                    _logger.LogCardInstancePlayed(cardInstanceId, playerId, cardTypeId, _gameIdInternal.ToString());
                    PlayedCardDto currentPlayedCardEntry = new PlayedCardDto { MoveNumberWhenActivated = _moveCounter + 1, PlayerId = playerId, PlayerName = playerOriginalName, PlayerColor = playerDataColor, CardId = cardTypeId, CardName = playedCardInstance.Name, TimestampUtc = DateTime.UtcNow };
                    lock (_sessionLock) { _playedCardsHistory.Add(currentPlayedCardEntry); }
                    MarkCardAsUsedGlobal(playerId, cardTypeId);
                    await _hubContext.Clients.Group(_gameIdInternal.ToString()).SendAsync("OnCardPlayed", playerId, playedCardInstance);
                }

                if (!effectResult.Success)
                {
                    _logger.LogSessionCardActivationFailed(_gameIdInternal, playerId, cardTypeId,
                        effectResult.ErrorMessage ?? "Kartenaktivierung durch Effektimplementierung fehlgeschlagen.");
                    if (timerWasManuallyPaused) _timerServiceInternal.ResumeTimer();

                    bool consumeCardOnFailure = cardTypeId == CardConstants.Wiedergeburt &&
                                               (effectResult.ErrorMessage?.Contains("besetzt") == true ||
                                                effectResult.ErrorMessage?.Contains("Ursprungsfeld") == true ||
                                                effectResult.ErrorMessage?.Contains("Keine wiederbelebungsfähigen") == true);
                    if (consumeCardOnFailure && playedCardInstance != null) // Sicherstellen, dass playedCardInstance nicht null ist
                    {
                        RemoveCardFromPlayerHand(playerId, cardInstanceId);
                        MarkCardAsUsedGlobal(playerId, cardTypeId);
                        PlayedCardDto failedCardEntry = new PlayedCardDto { MoveNumberWhenActivated = _moveCounter + 1, PlayerId = playerId, PlayerName = playerOriginalName, PlayerColor = playerDataColor, CardId = cardTypeId, CardName = playedCardInstance.Name, TimestampUtc = DateTime.UtcNow };
                        lock (_sessionLock) { _playedCardsHistory.Add(failedCardEntry); }
                        await _hubContext.Clients.Group(_gameIdInternal.ToString()).SendAsync("OnCardPlayed", playerId, playedCardInstance);
                        _activateCardSemaphore.Release();
                        return new ServerCardActivationResultDto { Success = true, ErrorMessage = effectResult.ErrorMessage, CardId = cardTypeId, EndsPlayerTurn = true, BoardUpdatedByCardEffect = false };
                    }
                    _activateCardSemaphore.Release();
                    return new ServerCardActivationResultDto { Success = false, ErrorMessage = effectResult.ErrorMessage ?? "Kartenaktivierung fehlgeschlagen.", CardId = cardTypeId };
                }

                bool playerStillInCheckAfterCardExecution;
                lock (_sessionLock)
                {
                    playerStillInCheckAfterCardExecution = _state.Board.IsInCheck(playerDataColor);
                }

                if (playerWasInCheckBeforeCard && playerStillInCheckAfterCardExecution && !_state.IsGameOver())
                {
                    _logger.LogPlayerStillInCheckAfterCardTurnNotEnded(_gameIdInternal, playerId, cardTypeId);
                    _state.SetResult(Result.Win(playerDataColor.Opponent(), EndReason.Checkmate));

                    UpdateHistoryOnGameOver();
                    NotifyTimerGameOver();

                    await SendOnTurnChangedNotification(ToBoardDto(), _state.CurrentPlayer.Opponent(), GameStatusDto.Checkmate, null, null, effectResult.AffectedSquaresByCard);
                    _activateCardSemaphore.Release();
                    return new ServerCardActivationResultDto
                    {
                        Success = true,
                        ErrorMessage = $"Du warst im Schach, hast '{playedCardInstance?.Name ?? cardTypeId}' gespielt und stehst immer noch im Schach. Du hast verloren!", // Null-Check für playedCardInstance
                        CardId = cardTypeId,
                        AffectedSquaresByCard = effectResult.AffectedSquaresByCard,
                        EndsPlayerTurn = true,
                        BoardUpdatedByCardEffect = effectResult.BoardUpdatedByCardEffect,
                        PlayerIdToSignalCardDraw = null,
                        NewlyDrawnCard = null
                    };
                }

                _logger.LogNotifyingOpponentOfCardPlay(_gameIdInternal, playerId, cardTypeId);
                if (cardTypeId == CardConstants.CardSwap)
                {
                    cardGivenForSwapEffectResult = effectResult.CardGivenByPlayerForSwapEffect;
                    cardReceivedForSwapEffectResult = effectResult.CardReceivedByPlayerForSwapEffect;
                }

                List<AffectedSquareInfo>? sigRHighlightCardSquares = effectResult.AffectedSquaresByCard;
                bool affectsRepetitionHistoryForThisCard = effectResult.BoardUpdatedByCardEffect;
                Move? cardEffectAsMove = null;

                bool turnActuallyEnds = effectResult.EndsPlayerTurn;

                lock (_sessionLock)
                {
                    if (turnActuallyEnds)
                    {
                        // Für Karten wie Opfergabe, die eine Figur entfernen, sollte der erste Parameter true sein.
                        bool captureOrPawnLikeEffect = affectsRepetitionHistoryForThisCard && (cardTypeId == CardConstants.SacrificeEffect /*|| andere Karten die Figuren entfernen */);
                        _state.UpdateStateAfterMove(captureOrPawnLikeEffect, affectsRepetitionHistoryForThisCard, cardEffectAsMove);
                    }
                    else
                    {
                        if (affectsRepetitionHistoryForThisCard)
                        {
                            _state.RecordCurrentStateForRepetition(cardEffectAsMove);
                        }
                        if (!_state.IsGameOver()) _state.CheckForGameOver();
                    }

                    if (_state.IsGameOver())
                    {
                        UpdateHistoryOnGameOver();
                        NotifyTimerGameOver();
                    }
                    else
                    {
                        if (turnActuallyEnds)
                        {
                            SwitchPlayerTimer();
                        }
                        else
                        {
                            if (timerWasManuallyPaused) _timerServiceInternal.ResumeTimer();
                            else StartGameTimer();
                        }
                    }
                }

                if (effectResult.PlayerIdToSignalDraw.HasValue)
                {
                    newlyDrawnCardByEffect = DrawCardForPlayer(effectResult.PlayerIdToSignalDraw.Value);
                }

                _logger.LogSessionCardActivationSuccess(_gameIdInternal, playerId, cardTypeId);
                return new ServerCardActivationResultDto
                {
                    Success = true,
                    ErrorMessage = effectResult.ErrorMessage,
                    CardId = cardTypeId,
                    AffectedSquaresByCard = sigRHighlightCardSquares,
                    EndsPlayerTurn = turnActuallyEnds,
                    BoardUpdatedByCardEffect = effectResult.BoardUpdatedByCardEffect,
                    PlayerIdToSignalCardDraw = effectResult.PlayerIdToSignalDraw,
                    NewlyDrawnCard = newlyDrawnCardByEffect,
                    CardGivenByPlayerForSwap = cardGivenForSwapEffectResult,
                    CardReceivedByPlayerForSwap = cardReceivedForSwapEffectResult
                };
            }
            catch (Exception ex)
            {
                _logger.LogSessionCardActivationFailed(_gameIdInternal, playerId, cardTypeId, $"Unerwarteter Fehler: {ex.Message} - StackTrace: {ex.StackTrace}");
                if (timerWasManuallyPaused) _timerServiceInternal.ResumeTimer();
                return new ServerCardActivationResultDto { Success = false, ErrorMessage = "Ein unerwarteter interner Fehler ist aufgetreten.", CardId = cardTypeId };
            }
            finally
            {
                if (_activateCardSemaphore.CurrentCount == 0) // Nur freigeben, wenn es gehalten wird
                {
                    _activateCardSemaphore.Release();
                }
            }
        }

        public MoveResultDto MakeMove(MoveDto dto, Guid playerIdCalling)
        {
            Player playerColorMakingTheMove;
            lock (_sessionLock)
            {
                try { playerColorMakingTheMove = GetPlayerColor(playerIdCalling); }
                catch (InvalidOperationException ex)
                {
                    _logger.LogSessionErrorGetNameByColor(_gameIdInternal, ex);
                    return new MoveResultDto { IsValid = false, ErrorMessage = "Interner Fehler: Spieler-ID nicht dem Spiel zugeordnet.", NewBoard = ToBoardDto(), Status = GameStatusDto.None };
                }
                if (playerColorMakingTheMove != _state.CurrentPlayer)
                {
                    return new MoveResultDto { IsValid = false, ErrorMessage = "Nicht dein Zug.", NewBoard = ToBoardDto(), IsYourTurn = IsPlayerTurn(playerIdCalling), Status = GameStatusDto.None };
                }
            }

            var fromPos = ParsePos(dto.From);
            var toPos = ParsePos(dto.To);
            Move? legalMove = null;
            bool isInCheckBeforeMove;

            lock (_sessionLock)
            {
                isInCheckBeforeMove = _state.Board.IsInCheck(_state.CurrentPlayer);
            }

            IEnumerable<Move> candidateMoves;
            lock (_sessionLock)
            {
                candidateMoves = _state.LegalMovesForPiece(fromPos);
            }

            if (dto.PromotionTo.HasValue)
            {
                _logger.LogPawnPromotionMoveSelection(_gameIdInternal, dto.From, dto.To, dto.PromotionTo);
                legalMove = candidateMoves.OfType<PawnPromotion>().FirstOrDefault(m => m.ToPos == toPos && m.PromotionTo == dto.PromotionTo.Value);
                if (legalMove != null)
                {
                    _logger.LogPawnPromotionMoveFound(_gameIdInternal, dto.From, dto.To, dto.PromotionTo.Value);
                }
                else
                {
                    _logger.LogPawnPromotionMoveNotFound(_gameIdInternal, dto.From, dto.To, dto.PromotionTo);
                }
            }
            else
            {
                legalMove = candidateMoves.FirstOrDefault(m => m.ToPos == toPos && !(m is PawnPromotion));
            }

            if (legalMove == null)
            {
                if (isInCheckBeforeMove)
                {
                    _logger.LogPlayerInCheckTriedInvalidMove(_gameIdInternal, playerIdCalling, playerColorMakingTheMove, dto.From, dto.To);
                }
                return new MoveResultDto { IsValid = false, ErrorMessage = "Ungültiger Zug.", NewBoard = ToBoardDto(), IsYourTurn = IsPlayerTurn(playerIdCalling), Status = GameStatusDto.None };
            }

            Guid playerIdWhoMadeTheMove = playerIdCalling;
            Piece? pieceBeingMoved;
            Piece? pieceAtDestination;
            bool captureOrPawn;
            Move moveForHistory = legalMove;

            lock (_sessionLock)
            {
                pieceBeingMoved = _state.Board[fromPos];
                pieceAtDestination = _state.Board[toPos];
                captureOrPawn = legalMove.Execute(_state.Board);
                _state.UpdateStateAfterMove(captureOrPawn, updateRepetitionHistory: true, move: moveForHistory);
            }

            DateTime moveTimestampUtc = DateTime.UtcNow;
            TimeSpan elapsedSinceLastTick = _timerServiceInternal.StopAndCalculateElapsedTime();
            lock (_capturedPieces)
            {
                if (pieceAtDestination != null && pieceAtDestination.Type != PieceType.Pawn)
                {
                    _capturedPieces[pieceAtDestination.Color].Add(pieceAtDestination.Type);
                    _logger.LogCapturedPieceAdded(_gameIdInternal, pieceAtDestination.Type, pieceAtDestination.Color);
                }
            }

            bool extraTurnGrantedByCard = false;
            lock (_sessionLock)
            {
                if (playerIdWhoMadeTheMove != Guid.Empty && _pendingCardEffectForNextMove.TryGetValue(playerIdWhoMadeTheMove, out var effectCardId))
                {
                    if (effectCardId == CardConstants.ExtraZug)
                    {
                        _state.SetCurrentPlayerOverride(playerColorMakingTheMove);
                        _pendingCardEffectForNextMove.Remove(playerIdWhoMadeTheMove);
                        extraTurnGrantedByCard = true;
                        _logger.LogExtraTurnEffectApplied(_gameIdInternal, playerIdWhoMadeTheMove, effectCardId);
                    }
                }
                Guid? playerIdToSignalDraw = null;
                CardDto? newlyDrawnCardFromMove = null;
                if (playerIdWhoMadeTheMove != Guid.Empty)
                {
                    _playerMoveCounts[playerIdWhoMadeTheMove]++;
                    _logger.LogPlayerMoveCountIncreased(_gameIdInternal, playerIdWhoMadeTheMove, _playerMoveCounts[playerIdWhoMadeTheMove]);
                    if (_playerMoveCounts[playerIdWhoMadeTheMove] % 5 == 0)
                    {
                        playerIdToSignalDraw = playerIdWhoMadeTheMove;
                        newlyDrawnCardFromMove = DrawCardForPlayer(playerIdWhoMadeTheMove);
                        if (newlyDrawnCardFromMove != null && !newlyDrawnCardFromMove.Name.Contains(CardConstants.NoMoreCardsName))
                        {
                            _logger.LogPlayerCardDrawIndicated(_gameIdInternal, playerIdWhoMadeTheMove);
                        }
                    }
                }

                var statusAfterMove = GetStatusForPlayer(playerColorMakingTheMove);
                _moveCounter++;
                var playedMove = new PlayedMoveDto { MoveNumber = _moveCounter, PlayerId = playerIdWhoMadeTheMove, PlayerColor = playerColorMakingTheMove, From = dto.From, To = dto.To, ActualMoveType = legalMove.Type, PromotionPiece = (legalMove as PawnPromotion)?.PromotionTo, TimestampUtc = moveTimestampUtc, TimeTaken = elapsedSinceLastTick, RemainingTimeWhite = _timerServiceInternal.GetCurrentTimeUpdateDto().WhiteTime, RemainingTimeBlack = _timerServiceInternal.GetCurrentTimeUpdateDto().BlackTime, PieceMoved = pieceBeingMoved?.ToString(), CapturedPiece = pieceAtDestination?.ToString() };
                _playedMoves.Add(playedMove);

                if (_state.IsGameOver())
                {
                    UpdateHistoryOnGameOver();
                    NotifyTimerGameOver();
                }
                else
                {
                    if (!extraTurnGrantedByCard) { SwitchPlayerTimer(); }
                    else { StartGameTimer(); }
                }
                return new MoveResultDto
                {
                    IsValid = true,
                    ErrorMessage = null,
                    NewBoard = ToBoardDto(),
                    IsYourTurn = extraTurnGrantedByCard,
                    Status = statusAfterMove,
                    PlayerIdToSignalCardDraw = playerIdToSignalDraw,
                    NewlyDrawnCard = newlyDrawnCardFromMove,
                    LastMoveFrom = dto.From,
                    LastMoveTo = dto.To,
                    CardEffectSquares = null
                };
            }
        }

        public bool IsCardUsableGlobal(Guid playerId, string cardTypeId)
        {
            lock (_sessionLock)
            {
                return !(_usedGlobalCardsPerPlayer.TryGetValue(playerId, out var usedCards) && usedCards.Contains(cardTypeId));
            }
        }

        public void MarkCardAsUsedGlobal(Guid playerId, string cardTypeId)
        {
            lock (_sessionLock)
            {
                if (!_usedGlobalCardsPerPlayer.TryGetValue(playerId, out var usedCards))
                {
                    usedCards = new HashSet<string>();
                    _usedGlobalCardsPerPlayer[playerId] = usedCards;
                }
                usedCards.Add(cardTypeId);
            }
        }
        public void SetPendingCardEffectForNextMove(Guid playerId, string cardTypeId)
        {
            lock (_sessionLock)
            {
                _pendingCardEffectForNextMove[playerId] = cardTypeId;
            }
        }

        public static Position ParsePos(string alg)
        {
            if (string.IsNullOrWhiteSpace(alg) || alg.Length != 2) throw new ArgumentException("Ungültiges algebraisches Format für Position.", nameof(alg));
            int col = alg[0] - 'a';
            if (!int.TryParse(alg[1].ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int rankValue)) throw new ArgumentException("Ungültiger Rang in algebraischer Notation.", nameof(alg));
            int row = 8 - rankValue;
            if (col < 0 || col > 7 || row < 0 || row > 7) throw new ArgumentException("Position ausserhalb des Schachbretts.", nameof(alg));
            return new Position(row, col);
        }

        public Guid? GetPlayerIdByColor(Player color)
        {
            lock (_sessionLock)
            {
                if (color == Player.White) return _playerWhiteId;
                if (color == Player.Black) return _playerBlackId;
                return null;
            }
        }

        public OpponentInfoDto? GetOpponentDetails(Guid currentPlayerId)
        {
            lock (_sessionLock)
            {
                if (!_players.TryGetValue(currentPlayerId, out var currentPlayerDetails))
                {
                    _logger.LogCurrentPlayerNotFoundForOpponentDetails(currentPlayerId, _gameIdInternal);
                    return null;
                }

                Player opponentColor = currentPlayerDetails.Color.Opponent();
                foreach (var entry in _players)
                {
                    if (entry.Value.Color == opponentColor)
                    {
                        return new OpponentInfoDto(entry.Key, entry.Value.Name, opponentColor);
                    }
                }
                _logger.LogNoOpponentFoundForPlayer(currentPlayerId, currentPlayerDetails.Color, _gameIdInternal);
                return null;
            }
        }

        private async Task SendOnTurnChangedNotification(BoardDto board, Player nextPlayer, GameStatusDto status, string? lastMoveFrom, string? lastMoveTo, List<AffectedSquareInfo>? cardEffectSquares)
        {
            string cardEffectTypeString = "None";
            if (cardEffectSquares != null && cardEffectSquares.Count > 0)
            {
                cardEffectTypeString = cardEffectSquares.First().Type ?? "UnknownType";
            }
            _logger.LogOnTurnChangedFromSession(_gameIdInternal, lastMoveFrom, lastMoveTo, cardEffectTypeString);
            await _hubContext.Clients.Group(_gameIdInternal.ToString())
                                     .SendAsync("OnTurnChanged", board, nextPlayer, status, lastMoveFrom, lastMoveTo, cardEffectSquares);
        }

        public string? GetPlayerName(Guid playerId)
        {
            lock (_sessionLock)
            {
                if (_players.TryGetValue(playerId, out var playerData))
                {
                    return playerData.Name;
                }
                return null;
            }
        }

        private void HandleTimeUpdated(TimeUpdateDto timeUpdateDto)
        {
            _logger.LogSessionSendTimeUpdate(_gameIdInternal, timeUpdateDto.WhiteTime, timeUpdateDto.BlackTime, timeUpdateDto.PlayerWhoseTurnItIs);
            _hubContext.Clients.Group(_gameIdInternal.ToString()).SendAsync("OnTimeUpdate", timeUpdateDto);
        }

        private void HandleTimeExpired(Player expiredPlayer)
        {
            lock (_sessionLock)
            {
                _logger.LogGameEndedByTimeoutInSession(_gameIdInternal, expiredPlayer);
                if (!_state.IsGameOver())
                {
                    _state.SetResult(Result.Win(expiredPlayer.Opponent(), EndReason.TimeOut));
                    UpdateHistoryOnGameOver();
                    BoardDto currentBoardDto = ToBoardDto();
                    Player winner = expiredPlayer.Opponent();
                    var finalTimeUpdateOutsideLock = _timerServiceInternal.GetCurrentTimeUpdateDto();
                    Task.Run(async () =>
                    {
                        await _hubContext.Clients.Group(_gameIdInternal.ToString())
                            .SendAsync("OnTurnChanged", currentBoardDto, winner, GameStatusDto.TimeOut, null, null, null);
                        await _hubContext.Clients.Group(_gameIdInternal.ToString())
                            .SendAsync("OnTimeUpdate", finalTimeUpdateOutsideLock);
                    });
                }
            }
        }
        public bool HasCapturedPieceOfType(Player ownerColor, PieceType type)
        {
            lock (_capturedPieces)
            {
                return _capturedPieces.TryGetValue(ownerColor, out var list) && list.Contains(type);
            }
        }

        public void RemoveCapturedPieceOfType(Player ownerColor, PieceType type)
        {
            lock (_capturedPieces)
            {
                if (_capturedPieces.TryGetValue(ownerColor, out var list))
                {
                    list.Remove(type);
                }
            }
        }

        public IEnumerable<CapturedPieceTypeDto> GetCapturedPieceTypesOfPlayer(Player playerColor)
        {
            lock (_capturedPieces)
            {
                if (_capturedPieces.TryGetValue(playerColor, out var capturedList))
                {
                    return capturedList.Select(type => new CapturedPieceTypeDto(type)).ToList();
                }
                return Enumerable.Empty<CapturedPieceTypeDto>();
            }
        }
        private void UpdateHistoryOnGameOver()
        {
            _gameHistory.Winner = _state.Result?.Winner;
            _gameHistory.ReasonForGameEnd = _state.Result?.Reason;
            if (!_gameHistory.DateTimeEndedUtc.HasValue)
            {
                _gameHistory.DateTimeEndedUtc = DateTime.UtcNow;
            }
        }
        public GameHistoryDto GetGameHistory()
        {
            lock (_sessionLock)
            {
                _gameHistory.PlayerWhiteId = _playerWhiteId;
                _gameHistory.PlayerBlackId = _playerBlackId;

                if (_playerWhiteId.HasValue && _players.TryGetValue(_playerWhiteId.Value, out var whitePlayerInfo))
                {
                    _gameHistory.PlayerWhiteName = whitePlayerInfo.Name;
                }
                if (_playerBlackId.HasValue && _players.TryGetValue(_playerBlackId.Value, out var blackPlayerInfo))
                {
                    _gameHistory.PlayerBlackName = blackPlayerInfo.Name;
                }
                if (string.IsNullOrEmpty(_gameHistory.PlayerWhiteName)) _gameHistory.PlayerWhiteName = GetNameByPlayerId(_playerWhiteId, allowNotFound: true);
                if (string.IsNullOrEmpty(_gameHistory.PlayerBlackName)) _gameHistory.PlayerBlackName = GetNameByPlayerId(_playerBlackId, allowNotFound: true);

                _gameHistory.Moves = new List<PlayedMoveDto>(_playedMoves);
                _gameHistory.PlayedCards = new List<PlayedCardDto>(_playedCardsHistory);
                if (!_state.IsGameOver())
                {
                    _gameHistory.Winner = null;
                    _gameHistory.ReasonForGameEnd = null;
                    _gameHistory.DateTimeEndedUtc = null;
                }
                else
                {
                    UpdateHistoryOnGameOver();
                }
                return _gameHistory;
            }
        }

        public bool IsGameReallyOver() { lock (_sessionLock) { return _state.IsGameOver(); } }
        public void StartGameTimer() { _timerServiceInternal.StartPlayerTimer(GetCurrentTurnPlayerLogic(), _state.IsGameOver()); }
        public void SwitchPlayerTimer() { _timerServiceInternal.StartPlayerTimer(GetCurrentTurnPlayerLogic(), _state.IsGameOver()); }
        public void NotifyTimerGameOver() { _timerServiceInternal.SetGameOver(); }

        public void Dispose()
        {
            _timerServiceInternal.Dispose();
            _activateCardSemaphore.Dispose();
            GC.SuppressFinalize(this);
        }

        public IEnumerable<string> GetLegalMoves(string fromAlg)
        {
            var pos = ParsePos(fromAlg);
            lock (_sessionLock)
            {
                return _state.LegalMovesForPiece(pos).Select(m => PieceHelper.ToAlgebraic(m.ToPos));
            }
        }

        public BoardDto ToBoardDto()
        {
            lock (_sessionLock)
            {
                var arr = new PieceDto?[8][];
                for (int r = 0; r < 8; r++)
                {
                    arr[r] = new PieceDto?[8];
                    for (int c = 0; c < 8; c++)
                        if (_state.Board[r, c] is { } piece) arr[r][c] = piece.ToDto();
                }
                return new BoardDto(arr);
            }
        }

        public bool IsPlayerTurn(Guid playerId)
        {
            try
            {
                Player playerColor = GetPlayerColor(playerId);
                lock (_sessionLock)
                {
                    return playerColor == _state.CurrentPlayer;
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogSessionErrorIsPlayerTurn(_gameIdInternal, ex);
                return false;
            }
        }

        public Player GetPlayerColor(Guid playerId)
        {
            lock (_sessionLock)
            {
                if (_players.TryGetValue(playerId, out var playerData))
                {
                    return playerData.Color;
                }
                if (playerId == _firstPlayerId && _players.Count <= 1)
                {
                    return _firstPlayerActualColor;
                }

                _logger.LogSessionColorNotDetermined(_gameIdInternal, playerId, _players.Count);
                throw new InvalidOperationException($"Spieler mit ID {playerId} nicht in der Session {_gameIdInternal} gefunden oder Farbe noch nicht eindeutig zugewiesen.");
            }
        }

        private string? GetNameByPlayerId(Guid? playerId, bool allowNotFound = false)
        {
            lock (_sessionLock)
            {
                if (playerId.HasValue && _players.TryGetValue(playerId.Value, out var playerData))
                {
                    return playerData.Name;
                }
                if (allowNotFound) return null;
                throw new InvalidOperationException($"Name für Spieler ID {playerId?.ToString() ?? "null"} nicht gefunden in Spiel {_gameIdInternal}.");
            }
        }

        public GameStatusDto GetStatus(Guid playerId)
        {
            Player color = GetPlayerColor(playerId);
            return GetStatusForPlayer(color);
        }

        private GameStatusDto GetStatusForPlayer(Player color)
        {
            lock (_sessionLock)
            {
                var result = _state.Result;
                if (_state.IsGameOver())
                {
                    if (result == null) return GameStatusDto.None;
                    if (result.Winner == Player.None) return MapEndReasonToGameStatusDto(result.Reason);
                    if (result.Winner == color) return GameStatusDto.None;
                    return MapEndReasonToGameStatusDto(result.Reason, true);
                }
                return _state.Board.IsInCheck(color) ? GameStatusDto.Check : GameStatusDto.None;
            }
        }
        private static GameStatusDto MapEndReasonToGameStatusDto(EndReason? reason, bool forLoserOrOpponentOfWinner = false)
        {
            if (reason == null) return GameStatusDto.None;
            return reason switch
            {
                EndReason.Checkmate => forLoserOrOpponentOfWinner ? GameStatusDto.Checkmate : GameStatusDto.None,
                EndReason.Stalemate => GameStatusDto.Stalemate,
                EndReason.FiftyMoveRule => GameStatusDto.Draw50MoveRule,
                EndReason.InsufficientMaterial => GameStatusDto.DrawInsufficientMaterial,
                EndReason.ThreefoldRepetition => GameStatusDto.DrawThreefoldRepetition,
                EndReason.TimeOut => forLoserOrOpponentOfWinner ? GameStatusDto.TimeOut : GameStatusDto.None,
                _ => GameStatusDto.None
            };
        }

        public Player GetCurrentTurnPlayerLogic()
        {
            lock (_sessionLock)
            {
                return _state.CurrentPlayer;
            }
        }
        public GameStatusDto GetStatusForOpponentOf(Guid lastPlayerId)
        {
            Player lastPlayerColor = GetPlayerColor(lastPlayerId);
            Player opponentColor = lastPlayerColor.Opponent();
            return GetStatusForPlayer(opponentColor);
        }

        public TimeUpdateDto GetCurrentTimeUpdateDto() => _timerServiceInternal.GetCurrentTimeUpdateDto();
    }
}