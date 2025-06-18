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
    // Verwaltet alle Aspekte des Kartensystems innerhalb einer Spielsitzung (`GameSession`).
    // Dazu gehören die Decks, Handkarten, Karteneffekte und das Ziehen von Karten.
    public class CardManager : ICardManager, IDisposable
    {
        private readonly GameSession _session;                                                  // Referenz zur übergeordneten Spielsitzung.
        private readonly IChessLogger _logger;                                                  // Dienst für das Logging.
        private readonly ILoggerFactory _loggerFactoryForEffects;                               // Factory, um Logger für die Karteneffekt-Klassen zu erstellen.
        private readonly object _sessionLock;                                                   // Sperrobjekt der GameSession, um Thread-Sicherheit zu gewährleisten.
        private readonly IHistoryManager _historyManager;                                       // Dienst zur Protokollierung von Spielereignissen.
        private readonly Random _random = new();                                                // Zufallsgenerator zum Mischen der Decks.
        private readonly List<CardDto> _allCardDefinitions;                                     // Eine Master-Liste aller im Spiel verfügbaren Kartentypen.
        private readonly Dictionary<string, ICardEffect> _cardEffects;                          // Dictionary, das Karten-IDs auf ihre Effekt-Implementierungen (Strategy Pattern) abbildet.
        private readonly Dictionary<Guid, List<CardDto>> _playerDrawPiles = new();              // Nachziehstapel für jeden Spieler.
        private readonly Dictionary<Guid, List<CardDto>> _playerHands = new();                  // Handkarten für jeden Spieler.
        private readonly Dictionary<Player, List<PieceType>> _capturedPieces = new() { { Player.White, new() }, { Player.Black, new() } }; // Liste der geschlagenen Figuren pro Farbe.
        private readonly Dictionary<Guid, string?> _pendingCardEffectForNextMove = new();       // Speichert anstehende Effekte wie "Extrazug".
        private readonly Dictionary<Guid, HashSet<string>> _usedGlobalCardsPerPlayer = new();   // Verfolgt global limitierte Karten (z.B. Extrazug).
        private readonly Dictionary<Guid, int> _playerMoveCounts = new();                       // Zählt die Züge jedes Spielers, um das Kartenziehen auszulösen.
        private readonly SemaphoreSlim _activateCardSemaphore = new(1, 1);                      // Stellt sicher, dass immer nur eine Kartenaktivierung gleichzeitig verarbeitet wird.
        private const int InitialHandSize = 3;                                                  // Anzahl der Karten auf der Starthand.

        // Konstruktor: Initialisiert alle Dienste und erstellt die Karten- und Effekt-Definitionen.
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

        // Initialisiert die Kartendecks für einen Spieler zu Beginn des Spiels.
        public void InitializeDecksForPlayer(Guid playerId, int initialTimeMinutes)
        {
            _playerMoveCounts[playerId] = 0;
            // Filtert Zeit-Karten bei kurzen Partien heraus.
            var availableCards = new List<CardDto>(_allCardDefinitions);
            if (initialTimeMinutes <= 3)
            {
                availableCards.RemoveAll(c => c.Category == CardCategory.Time);
            }

            InitializeAndShufflePlayerDeck(playerId, availableCards);
            // Zieht die Starthand für den Spieler.
            if (!_playerHands.ContainsKey(playerId))
            {
                _playerHands[playerId] = new List<CardDto>();
                var deck = _playerDrawPiles.GetValueOrDefault(playerId, new List<CardDto>());
                var hand = _playerHands[playerId];
                // Stellt sicher, dass maximal eine Zeitkarte in der Starthand ist, um die Balance zu wahren.
                var timeCardsInDeck = deck.Where(c => c.Category == CardCategory.Time).ToList();
                var otherCardsInDeck = deck.Where(c => c.Category != CardCategory.Time).ToList();

                if (timeCardsInDeck.Count != 0)
                {
                    var timeCardToAdd = timeCardsInDeck.First();
                    hand.Add(timeCardToAdd);
                    deck.Remove(timeCardToAdd);
                }

                int cardsToDraw = InitialHandSize - hand.Count;
                var otherCardsToAdd = deck.Where(c => c.Category != CardCategory.Time).Take(cardsToDraw).ToList();
                foreach (var card in otherCardsToAdd)
                {
                    hand.Add(card);
                    deck.Remove(card);
                }
            }
            var playerColor = _session.GetPlayerColor(playerId);
            if (!_capturedPieces.ContainsKey(playerColor))
            {
                _capturedPieces[playerColor] = new List<PieceType>();
            }
        }

        // Die zentrale Methode zur Aktivierung einer Karte. Sie ist der Einstiegspunkt für den komplexen Aktivierungsprozess.
        public async Task<ServerCardActivationResultDto> ActivateCard(Guid playerId, ActivateCardRequestDto dto)
        {
            await _activateCardSemaphore.WaitAsync();
            try
            {
                var activatingPlayerColor = _session.GetPlayerColor(playerId);
                // Validierung: Ist der Spieler am Zug?
                if (activatingPlayerColor != _session.CurrentGameState.CurrentPlayer)
                {
                    _logger.LogSessionCardActivationFailed(_session.GameId, playerId, dto.CardTypeId, "Spieler ist nicht am Zug.");
                    return new ServerCardActivationResultDto { Success = false, ErrorMessage = "Nicht dein Zug.", CardId = dto.CardTypeId };
                }

                // Validierung: Darf die Karte gespielt werden, wenn der Spieler im Schach steht?
                bool isInCheck = _session.CurrentGameState.Board.IsInCheck(activatingPlayerColor);
                if (isInCheck && !IsCardBoardAltering(dto.CardTypeId))
                {
                    _logger.LogSessionCardActivationFailed(_session.GameId, playerId, dto.CardTypeId, "Spieler ist im Schach und die Karte hebt das Schach nicht auf.");
                    return new ServerCardActivationResultDto
                    {
                        Success = false,
                        ErrorMessage = "Du stehst im Schach! Nur Karten, die Figuren bewegen, sind jetzt erlaubt.",
                        CardId = dto.CardTypeId
                    };
                }

                // Validierung: Hat der Spieler die Karte auf der Hand?
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
                // Holt die passende Effekt-Implementierung aus dem Dictionary.
                if (!_cardEffects.TryGetValue(dto.CardTypeId, out var effect))
                {
                    _logger.LogSessionCardActivationFailed(_session.GameId, playerId, dto.CardTypeId, "Unbekannte Karte.");
                    return new ServerCardActivationResultDto { Success = false, ErrorMessage = "Unbekannte Karte.", CardId = dto.CardTypeId };
                }

                var timerWasPaused = _session.PauseTimerForAction();

                var context = new CardExecutionContext(
                    _session,
                    playerId,
                    activatingPlayerColor,
                    _historyManager,
                    dto // Das komplette DTO wird übergeben
                );
                var effectResult = effect.Execute(context);

                // Leitet das Ergebnis an die GameSession weiter, die die Folgeaktionen (Timer, Benachrichtigungen) koordiniert.
                var finalResult = await _session.HandleCardActivationResult(effectResult, playerId, playedCardInstance, dto.CardTypeId, timerWasPaused);
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

        // Erhöht den Zugzähler eines Spielers.
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

        // Prüft, ob der Spieler (nach jeweils 5 Zügen) eine neue Karte ziehen darf.
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

        // Fügt eine geschlagene Figur der entsprechenden Liste hinzu.
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

        // Prüft, ob eine global limitierte Karte von einem Spieler bereits verwendet wurde.
        public bool IsCardUsableGlobal(Guid playerId, string cardTypeId)
        {
            lock (_sessionLock)
            {
                return !(_usedGlobalCardsPerPlayer.TryGetValue(playerId, out var usedCards) && usedCards.Contains(cardTypeId));
            }
        }

        // Markiert eine global limitierte Karte als verbraucht.
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

        // Entfernt eine spezifische Karte aus der Hand eines Spielers.
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

        // Fügt eine Karte zur Hand eines Spielers hinzu (z.B. beim Kartentausch).
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

        // Entfernt eine wiederbelebte Figur aus der Liste der geschlagenen Figuren.
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

        // Setzt einen anstehenden Karteneffekt für den nächsten Zug.
        public void SetPendingCardEffectForNextMove(Guid playerId, string cardTypeId)
        {
            lock (_sessionLock)
            {
                _pendingCardEffectForNextMove[playerId] = cardTypeId;
            }
        }

        // Überprüft, ob ein Effekt für den nächsten Zug ansteht, ohne ihn zu entfernen.
        public string? PeekPendingCardEffect(Guid playerId)
        {
            lock (_sessionLock)
            {
                _pendingCardEffectForNextMove.TryGetValue(playerId, out var effect);
                return effect;
            }
        }

        // Entfernt einen anstehenden Effekt, nachdem er angewendet wurde.
        public void ClearPendingCardEffect(Guid playerId)
        {
            lock (_sessionLock)
            {
                _pendingCardEffectForNextMove.Remove(playerId);
            }
        }

        // Gibt eine Kopie der Handkarten eines Spielers zurück.
        public List<CardDto> GetPlayerHand(Guid playerId)
        {
            lock (_sessionLock)
            {
                return _playerHands.TryGetValue(playerId, out var hand) ? new List<CardDto>(hand) : new List<CardDto>();
            }
        }

        // Gibt die Anzahl der Karten im Nachziehstapel eines Spielers zurück.
        public int GetDrawPileCount(Guid playerId)
        {
            lock (_sessionLock)
            {
                return _playerDrawPiles.TryGetValue(playerId, out var pile) ? pile.Count : 0;
            }
        }

        // Gibt die Typen der geschlagenen Figuren eines Spielers zurück (relevant für die Wiedergeburt).
        public IEnumerable<CapturedPieceTypeDto> GetCapturedPieceTypesOfPlayer(Player playerColor)
        {
            lock (_capturedPieces)
            {
                return _capturedPieces.TryGetValue(playerColor, out var list) ? list.Select(t => new CapturedPieceTypeDto(t)).ToList() : Enumerable.Empty<CapturedPieceTypeDto>();
            }
        }

        // Holt die vollständige Definition einer Karte (z.B. für Animationen).
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

        // Erstellt ein neues, gemischtes Deck für einen Spieler.
        private void InitializeAndShufflePlayerDeck(Guid playerId, List<CardDto> availableCardDefinitions)
        {
            var newDeck = availableCardDefinitions.Select(cardDef => new CardDto
            {
                InstanceId = Guid.NewGuid(),
                Id = cardDef.Id,
                Name = cardDef.Name,
                Description = cardDef.Description,
                ImageUrl = cardDef.ImageUrl,
                AnimationDelayMs = cardDef.AnimationDelayMs,
                Category = cardDef.Category
            }).ToList();
            // Fisher-Yates-Shuffle-Algorithmus zum Mischen.
            int n = newDeck.Count;
            while (n > 1) { n--; int k = _random.Next(n + 1); (newDeck[k], newDeck[n]) = (newDeck[n], newDeck[k]); }
            _playerDrawPiles[playerId] = newDeck;
            _logger.LogPlayerDeckInitialized(playerId, _session.GameId, newDeck.Count);
        }

        // Zieht die oberste Karte vom Deck eines Spielers und fügt sie seiner Hand hinzu.
        public CardDto? DrawCardForPlayer(Guid playerId)
        {
            lock (_sessionLock)
            {
                if (_session.GetPlayerName(playerId) == null)
                {
                    _logger.LogDrawAttemptUnknownPlayer(playerId, _session.GameId);
                    return null;
                }
                if (!_playerDrawPiles.TryGetValue(playerId, out var specificDrawPile))
                {
                    _logger.LogNoDrawPileForPlayer(playerId, _session.GameId);
                    // Fallback: Deck neu initialisieren, falls es aus irgendeinem Grund nicht existiert.
                    InitializeAndShufflePlayerDeck(playerId, _allCardDefinitions);
                    if (!_playerDrawPiles.TryGetValue(playerId, out specificDrawPile) || specificDrawPile == null)
                        return new CardDto { InstanceId = Guid.NewGuid(), Id = $"{CardConstants.FallbackCardIdPrefix}error", Name = "Fehler", Description = "Deck nicht initialisiert.", ImageUrl = CardConstants.DefaultCardBackImageUrl };
                }
                // Wenn der Nachziehstapel leer ist, wird eine spezielle "Keine Karten mehr"-Karte zurückgegeben.
                if (specificDrawPile.Count == 0)
                {
                    _logger.LogPlayerDrawPileEmpty(playerId, _session.GameId);
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
                _logger.LogPlayerDrewCardFromOwnDeck(playerId, drawnCard.Name, drawnCard.Id.ToString(), _session.GameId, specificDrawPile.Count);
                return drawnCard;
            }
        }

        // Definiert die Master-Liste aller Karten im Spiel.
        private static List<CardDto> CreateCardDefinitions() => new()
        {
            new() { InstanceId = Guid.Empty, Id = CardConstants.ExtraZug, Name = "Extrazug", Description = "Du darfst sofort einen weiteren Schachzug ausführen. (Einmal pro Spiel)", ImageUrl = "img/cards/art/1-Extrazug_Art.png", AnimationDelayMs = 0, Category = CardCategory.Gameplay },
            new() { InstanceId = Guid.Empty, Id = CardConstants.Teleport, Name = "Teleportation", Description = "Eine eigene Figur darf auf ein beliebiges leeres Feld auf dem Schachbrett gestellt werden.", ImageUrl = "img/cards/art/2-Teleportation_Art.png", AnimationDelayMs = 3000, Category = CardCategory.Gameplay },
            new() { InstanceId = Guid.Empty, Id = CardConstants.Positionstausch, Name = "Positionstausch", Description = "Zwei eigene Figuren tauschen ihre Plätze.", ImageUrl = "img/cards/art/3-Positionstausch_Art.png", AnimationDelayMs = 3000, Category = CardCategory.Gameplay },
            new() { InstanceId = Guid.Empty, Id = CardConstants.AddTime, Name = "Zeitgutschrift", Description = "Fügt deiner Bedenkzeit 2 Minuten hinzu.", ImageUrl = "img/cards/art/11-AddTime_Art.png", AnimationDelayMs = 3000, Category = CardCategory.Time },
            new() { InstanceId = Guid.Empty, Id = CardConstants.SubtractTime, Name = "Zeitdiebstahl", Description = "Zieht der gegnerischen Bedenkzeit 2 Minuten ab (minimal 1 Minute Restzeit).", ImageUrl = "img/cards/art/12-SubtractTime_Art.png", AnimationDelayMs = 3000, Category = CardCategory.Time },
            new() { InstanceId = Guid.Empty, Id = CardConstants.TimeSwap, Name = "Zeittausch", Description = "Tauscht die aktuellen Restbedenkzeiten mit deinem Gegner (minimal 1 Minute Restzeit für jeden).", ImageUrl = "img/cards/art/13-Zeittausch_Art.png", AnimationDelayMs = 3000, Category = CardCategory.Time },
            new() { InstanceId = Guid.Empty, Id = CardConstants.Wiedergeburt, Name = "Wiedergeburt", Description = "Eine eigene, geschlagene Nicht-Bauern-Figur wird auf einem ihrer ursprünglichen Startfelder wiederbelebt. Ist das gewählte Feld besetzt, schlägt der Effekt fehl und die Karte ist verbraucht.", ImageUrl = "img/cards/art/5-Wiedergeburt_Art.png", AnimationDelayMs = 3000, Category = CardCategory.Gameplay },
            new() { InstanceId = Guid.Empty, Id = CardConstants.CardSwap, Name = "Kartentausch", Description = "Wähle eine deiner Handkarten. Diese wird mit einer zufälligen Handkarte deines Gegners getauscht. Hat der Gegner keine Karten, verfällt deine Karte ohne Effekt.", ImageUrl = "img/cards/art/14-Kartentausch_Art.png", AnimationDelayMs = 5000, Category = CardCategory.Utility },
            new() { InstanceId = Guid.Empty, Id = CardConstants.SacrificeEffect, Name = "Opfergabe", Description = "Wähle einen eigenen Bauern. Entferne ihn vom Spiel. Du darfst sofort eine neue Karte ziehen.", ImageUrl = "img/cards/art/15-Opergabe_Art.png", AnimationDelayMs = 3000, Category = CardCategory.Gameplay }
        };

        // Weist jeder Karten-ID die entsprechende Effekt-Implementierung zu (Strategy Pattern).
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

        // Prüft, ob eine Karte das Schachbrett verändert (relevant für die Schach-Regel).
        private bool IsCardBoardAltering(string cardTypeId)
        {
            return cardTypeId switch
            {
                CardConstants.Teleport => true,
                CardConstants.Positionstausch => true,
                CardConstants.Wiedergeburt => true,
                CardConstants.SacrificeEffect => true,
                _ => false
            };
        }

        // Gibt die vom Semaphore belegten Ressourcen frei.
        public void Dispose()
        {
            _activateCardSemaphore.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}