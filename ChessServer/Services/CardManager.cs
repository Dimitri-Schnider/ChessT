using Chess.Logging;
using ChessLogic;
using ChessNetwork.Configuration;
using ChessNetwork.DTOs;
using ChessServer.Services.CardEffects;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ChessServer.Services
{
    public class CardManager : ICardManager, IDisposable
    {
        private readonly GameSession _session;
        private readonly IChessLogger _logger;
        private readonly ILoggerFactory _loggerFactoryForEffects;
        private readonly object _sessionLock;
        private readonly IHistoryManager _historyManager;
        private readonly Random _random = new();
        private readonly List<CardDto> _allCardDefinitions;
        private readonly Dictionary<string, ICardEffect> _cardEffects;
        private readonly Dictionary<Guid, List<CardDto>> _playerDrawPiles = new();
        private readonly Dictionary<Guid, List<CardDto>> _playerHands = new();
        private readonly Dictionary<Player, List<PieceType>> _capturedPieces = new() { { Player.White, new() }, { Player.Black, new() } };
        private readonly Dictionary<Guid, string?> _pendingCardEffectForNextMove = new();
        private readonly Dictionary<Guid, HashSet<string>> _usedGlobalCardsPerPlayer = new();
        private readonly Dictionary<Guid, int> _playerMoveCounts = new();
        private readonly SemaphoreSlim _activateCardSemaphore = new(1, 1);
        private const int InitialHandSize = 3;
        public CardManager(GameSession session, object sessionLock, IHistoryManager historyManager, IChessLogger logger, ILoggerFactory loggerFactory)
        {
            _session = session;
            _sessionLock = sessionLock;
            _historyManager = historyManager;
            _logger = logger;
            _loggerFactoryForEffects = loggerFactory;

            _allCardDefinitions = CreateCardDefinitions();
            _cardEffects = CreateCardEffects(_loggerFactoryForEffects);
        }

        public void InitializeDecksForPlayer(Guid playerId)
        {
            _playerMoveCounts[playerId] = 0;
            InitializeAndShufflePlayerDeck(playerId);
            if (!_playerHands.ContainsKey(playerId))
            {
                _playerHands[playerId] = new List<CardDto>();
                for (int i = 0; i < InitialHandSize; i++) { DrawCardForPlayer(playerId); }
            }
            var playerColor = _session.GetPlayerColor(playerId);
            if (!_capturedPieces.ContainsKey(playerColor))
            {
                _capturedPieces[playerColor] = new List<PieceType>();
            }
        }

        public async Task<ServerCardActivationResultDto> ActivateCard(Guid playerId, ActivateCardRequestDto dto)
        {
            await _activateCardSemaphore.WaitAsync();
            try
            {
                var activatingPlayerColor = _session.GetPlayerColor(playerId);
                if (activatingPlayerColor != _session.CurrentGameState.CurrentPlayer)
                {
                    _logger.LogSessionCardActivationFailed(_session.GameId, playerId, dto.CardTypeId, "Spieler ist nicht am Zug.");
                    return new ServerCardActivationResultDto { Success = false, ErrorMessage = "Nicht dein Zug.", CardId = dto.CardTypeId };
                }
                CardDto? playedCardInstance;
                lock (_sessionLock)
                {
                    playedCardInstance = _playerHands.TryGetValue(playerId, out var hand) ? hand.FirstOrDefault(c => c.InstanceId == dto.CardInstanceId) : null;
                }
                if (playedCardInstance == null || playedCardInstance.Id != dto.CardTypeId)
                {
                    _logger.LogCardInstanceNotFoundInHand(dto.CardInstanceId, playerId, _session.GameId.ToString());
                    return new ServerCardActivationResultDto { Success = false, ErrorMessage = "Gespielte Karte (Instanz oder Typ) nicht auf der Hand.", CardId = dto.CardTypeId };
                }
                if (!_cardEffects.TryGetValue(dto.CardTypeId, out var effect))
                {
                    _logger.LogSessionCardActivationFailed(_session.GameId, playerId, dto.CardTypeId, "Unbekannte Karte.");
                    return new ServerCardActivationResultDto { Success = false, ErrorMessage = "Unbekannte Karte.", CardId = dto.CardTypeId };
                }
                var timerWasManuallyPaused = _session.PauseTimerForAction();
                string? param1ForEffect = (dto.CardTypeId == CardConstants.Wiedergeburt) ? dto.PieceTypeToRevive?.ToString() : (dto.CardTypeId == CardConstants.CardSwap) ? dto.CardInstanceIdToSwapFromHand?.ToString() : dto.FromSquare;
                string? param2ForEffect = (dto.CardTypeId == CardConstants.Wiedergeburt) ? dto.TargetRevivalSquare : dto.ToSquare;
                var effectResult = effect.Execute(_session, playerId, activatingPlayerColor, _historyManager, dto.CardTypeId, param1ForEffect, param2ForEffect);
                var finalResult = await _session.HandleCardActivationResult(effectResult, playerId, playedCardInstance, dto.CardTypeId, timerWasManuallyPaused);
                return finalResult;
            }
            finally
            {
                if (_activateCardSemaphore.CurrentCount == 0)
                {
                    _activateCardSemaphore.Release();
                }
            }
        }

        public void IncrementPlayerMoveCount(Guid playerId)
        {
            lock (_sessionLock)
            {
                if (!_playerMoveCounts.TryAdd(playerId, 1))
                {
                    _playerMoveCounts[playerId]++;
                }
                _logger.LogPlayerMoveCountIncreased(_session.GameId, playerId, _playerMoveCounts[playerId]);
            }
        }

        public (bool ShouldDraw, CardDto? DrawnCard) CheckAndProcessCardDraw(Guid playerId)
        {
            lock (_sessionLock)
            {
                if (_playerMoveCounts.TryGetValue(playerId, out int count) && count > 0 && count % 5 == 0)
                {
                    var drawnCard = DrawCardForPlayer(playerId);
                    if (drawnCard != null && !drawnCard.Name.Contains(CardConstants.NoMoreCardsName))
                    {
                        _logger.LogPlayerCardDrawIndicated(_session.GameId, playerId);
                    }
                    return (true, drawnCard);
                }
                return (false, null);
            }
        }

        public void AddCapturedPiece(Player ownerColor, PieceType pieceType)
        {
            lock (_capturedPieces)
            {
                if (_capturedPieces.TryGetValue(ownerColor, out var list))
                {
                    list.Add(pieceType);
                    _logger.LogCapturedPieceAdded(_session.GameId, pieceType, ownerColor);
                }
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

        public bool RemoveCardFromPlayerHand(Guid playerId, Guid cardInstanceIdToRemove)
        {
            lock (_sessionLock)
            {
                if (_playerHands.TryGetValue(playerId, out var hand))
                {
                    var cardInstance = hand.FirstOrDefault(c => c.InstanceId == cardInstanceIdToRemove);
                    if (cardInstance != null) { return hand.Remove(cardInstance); }
                }
                _logger.LogCardInstanceNotFoundInHand(cardInstanceIdToRemove, playerId, _session.GameId.ToString());
                return false;
            }
        }

        public void AddCardToPlayerHand(Guid playerId, CardDto cardToAdd)
        {
            lock (_sessionLock)
            {
                if (!_playerHands.TryGetValue(playerId, out var hand))
                {
                    hand = new List<CardDto>();
                    _playerHands[playerId] = hand;
                }
                hand.Add(cardToAdd);
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

        public void SetPendingCardEffectForNextMove(Guid playerId, string cardTypeId)
        {
            lock (_sessionLock)
            {
                _pendingCardEffectForNextMove[playerId] = cardTypeId;
            }
        }

        public string? GetAndClearPendingCardEffect(Guid playerId)
        {
            lock (_sessionLock)
            {
                if (_pendingCardEffectForNextMove.TryGetValue(playerId, out var effect))
                {
                    _pendingCardEffectForNextMove.Remove(playerId);
                    return effect;
                }
                return null;
            }
        }

        public List<CardDto> GetPlayerHand(Guid playerId)
        {
            lock (_sessionLock)
            {
                return _playerHands.TryGetValue(playerId, out var hand) ? new List<CardDto>(hand) : new List<CardDto>();
            }
        }

        public int GetDrawPileCount(Guid playerId)
        {
            lock (_sessionLock)
            {
                return _playerDrawPiles.TryGetValue(playerId, out var pile) ? pile.Count : 0;
            }
        }

        public IEnumerable<CapturedPieceTypeDto> GetCapturedPieceTypesOfPlayer(Player playerColor)
        {
            lock (_capturedPieces)
            {
                return _capturedPieces.TryGetValue(playerColor, out var list) ? list.Select(t => new CapturedPieceTypeDto(t)).ToList() : Enumerable.Empty<CapturedPieceTypeDto>();
            }
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
                    ImageUrl = definition.ImageUrl,
                    AnimationDelayMs = definition.AnimationDelayMs
                };
            }
            _logger.LogClientCriticalServicesNullOnInit($"[CardManager] GetCardDefinitionForAnimation: Kartendefinition für ID '{cardTypeId}' nicht gefunden.");
            return null;
        }

        private void InitializeAndShufflePlayerDeck(Guid playerId)
        {
            var newDeck = _allCardDefinitions.Select(cardDef => new CardDto
            {
                InstanceId = Guid.NewGuid(),
                Id = cardDef.Id,
                Name = cardDef.Name,
                Description = cardDef.Description,
                ImageUrl = cardDef.ImageUrl,
                AnimationDelayMs = cardDef.AnimationDelayMs
            }).ToList();
            int n = newDeck.Count;
            while (n > 1) { n--; int k = _random.Next(n + 1); (newDeck[k], newDeck[n]) = (newDeck[n], newDeck[k]); }
            _playerDrawPiles[playerId] = newDeck;
            _logger.LogPlayerDeckInitialized(playerId, _session.GameId, newDeck.Count);
        }

        // KORREKTUR: Sichtbarkeit von 'private' auf 'public' geändert.
        public CardDto? DrawCardForPlayer(Guid playerId)
        {
            lock (_sessionLock)
            {
                if (_session.GetPlayerName(playerId) == null) { _logger.LogDrawAttemptUnknownPlayer(playerId, _session.GameId); return null; }
                if (!_playerDrawPiles.TryGetValue(playerId, out var specificDrawPile))
                {
                    _logger.LogNoDrawPileForPlayer(playerId, _session.GameId);
                    InitializeAndShufflePlayerDeck(playerId);
                    if (!_playerDrawPiles.TryGetValue(playerId, out specificDrawPile) || specificDrawPile == null)
                        return new CardDto { InstanceId = Guid.NewGuid(), Id = $"{CardConstants.FallbackCardIdPrefix}error", Name = "Fehler", Description = "Deck nicht initialisiert.", ImageUrl = CardConstants.DefaultCardBackImageUrl };
                }
                if (specificDrawPile.Count == 0)
                {
                    _logger.LogPlayerDrawPileEmpty(playerId, _session.GameId);
                    return new CardDto { InstanceId = Guid.NewGuid(), Id = $"{CardConstants.FallbackCardIdPrefix}empty_{playerId}", Name = CardConstants.NoMoreCardsName, Description = "Dein Nachziehstapel ist leer.", ImageUrl = CardConstants.DefaultCardBackImageUrl };
                }
                CardDto drawnCard = specificDrawPile.First();
                specificDrawPile.RemoveAt(0);
                if (!_playerHands.TryGetValue(playerId, out var hand)) { hand = new List<CardDto>(); _playerHands[playerId] = hand; }
                hand.Add(drawnCard);
                _logger.LogPlayerDrewCardFromOwnDeck(playerId, drawnCard.Name, drawnCard.Id.ToString(), _session.GameId, specificDrawPile.Count);
                return drawnCard;
            }
        }

        private static List<CardDto> CreateCardDefinitions() => new()
        {
            new() { InstanceId = Guid.Empty, Id = CardConstants.ExtraZug, Name = "Extrazug", Description = "Du darfst sofort einen weiteren Schachzug ausführen. (Einmal pro Spiel)", ImageUrl = "img/cards/art/1-Extrazug_Art.png", AnimationDelayMs = 0 },
            new() { InstanceId = Guid.Empty, Id = CardConstants.Teleport, Name = "Teleportation", Description = "Eine eigene Figur darf auf ein beliebiges leeres Feld auf dem Schachbrett gestellt werden.", ImageUrl = "img/cards/art/2-Teleportation_Art.png", AnimationDelayMs = 3000 },
            new() { InstanceId = Guid.Empty, Id = CardConstants.Positionstausch, Name = "Positionstausch", Description = "Zwei eigene Figuren tauschen ihre Plätze.", ImageUrl = "img/cards/art/3-Positionstausch_Art.png", AnimationDelayMs = 3000 },
            new() { InstanceId = Guid.Empty, Id = CardConstants.AddTime, Name = "Zeitgutschrift", Description = "Fügt deiner Bedenkzeit 2 Minuten hinzu.", ImageUrl = "img/cards/art/11-AddTime_Art.png", AnimationDelayMs = 3000 },
             new() { InstanceId = Guid.Empty, Id = CardConstants.SubtractTime, Name = "Zeitdiebstahl", Description = "Zieht der gegnerischen Bedenkzeit 2 Minuten ab (minimal 1 Minute Restzeit).", ImageUrl = "img/cards/art/12-SubtractTime_Art.png", AnimationDelayMs = 3000 },
            new() { InstanceId = Guid.Empty, Id = CardConstants.TimeSwap, Name = "Zeittausch", Description = "Tauscht die aktuellen Restbedenkzeiten mit deinem Gegner (minimal 1 Minute Restzeit für jeden).", ImageUrl = "img/cards/art/13-Zeittausch_Art.png", AnimationDelayMs = 3000 },
            new() { InstanceId = Guid.Empty, Id = CardConstants.Wiedergeburt, Name = "Wiedergeburt", Description = "Eine eigene, geschlagene Nicht-Bauern-Figur wird auf einem ihrer ursprünglichen Startfelder wiederbelebt. Ist das gewählte Feld besetzt, schlägt der Effekt fehl und die Karte ist verbraucht.", ImageUrl = "img/cards/art/5-Wiedergeburt_Art.png", AnimationDelayMs = 3000 },
            new() { InstanceId = Guid.Empty, Id = CardConstants.CardSwap, Name = "Kartentausch", Description = "Wähle eine deiner Handkarten. Diese wird mit einer zufälligen Handkarte deines Gegners getauscht. Hat der Gegner keine Karten, verfällt deine Karte ohne Effekt.", ImageUrl = "img/cards/art/14-Kartentausch_Art.png", AnimationDelayMs = 5000 },
            new() { InstanceId = Guid.Empty, Id = CardConstants.SacrificeEffect, Name = "Opfergabe", Description = "Wähle einen eigenen Bauern. Entferne ihn vom Spiel. Du darfst sofort eine neue Karte ziehen.", ImageUrl = "img/cards/art/15-Opergabe_Art.png", AnimationDelayMs = 3000 }
        };

        private static Dictionary<string, ICardEffect> CreateCardEffects(ILoggerFactory loggerFactory) => new()
        {
            { CardConstants.ExtraZug, new ExtraZugEffect(new ChessLogger<ExtraZugEffect>(loggerFactory.CreateLogger<ExtraZugEffect>())) },
            { CardConstants.Teleport, new TeleportEffect(new ChessLogger<TeleportEffect>(loggerFactory.CreateLogger<TeleportEffect>())) },
            { CardConstants.Positionstausch, new PositionSwapEffect(new ChessLogger<PositionSwapEffect>(loggerFactory.CreateLogger<PositionSwapEffect>())) },
             { CardConstants.AddTime, new AddTimeEffect(new ChessLogger<AddTimeEffect>(loggerFactory.CreateLogger<AddTimeEffect>())) },
            { CardConstants.SubtractTime, new SubtractTimeEffect(new ChessLogger<SubtractTimeEffect>(loggerFactory.CreateLogger<SubtractTimeEffect>())) },
            { CardConstants.TimeSwap, new TimeSwapEffect(new ChessLogger<TimeSwapEffect>(loggerFactory.CreateLogger<TimeSwapEffect>())) },
            { CardConstants.Wiedergeburt, new RebirthEffect(new ChessLogger<RebirthEffect>(loggerFactory.CreateLogger<RebirthEffect>())) },
            { CardConstants.CardSwap, new CardSwapEffect(new ChessLogger<CardSwapEffect>(loggerFactory.CreateLogger<CardSwapEffect>())) },
            { CardConstants.SacrificeEffect, new SacrificeEffect(new ChessLogger<SacrificeEffect>(loggerFactory.CreateLogger<SacrificeEffect>())) }
        };
        public void Dispose()
        {
            _activateCardSemaphore.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}