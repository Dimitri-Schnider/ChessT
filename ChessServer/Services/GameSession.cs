using Chess.Logging;
using ChessLogic;
using ChessLogic.Moves;
using ChessLogic.Utilities;
using ChessNetwork.Configuration;
using ChessNetwork.DTOs;
using ChessServer.Hubs;
using ChessServer.Services.CardEffects;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ChessServer.Services
{
    // Verwaltet den gesamten Zustand und die Logik einer einzelnen Schachpartie.
    // Diese Klasse ist versiegelt (sealed), um Vererbung zu verhindern und die Stabilität zu gewährleisten.
    public sealed class GameSession : IDisposable
    {
        #region Private Fields

        // --- Kernzustand & Services ---
        private readonly Guid _gameIdInternal;                                          // Die eindeutige ID dieser Spielsitzung.
        private readonly GameState _state;                                              // Der logische Kernzustand des Spiels (Brett, aktueller Spieler).
        private readonly GameTimerService _timerServiceInternal;                        // Der Dienst zur Verwaltung der Bedenkzeiten.
        private readonly IHubContext<ChessHub> _hubContext;                             // SignalR-Hub-Kontext für Echtzeit-Benachrichtigungen.
        private readonly IChessLogger _logger;                                          // Dienst für strukturiertes Logging von Spielereignissen.
        private readonly ILoggerFactory _loggerFactoryForEffects;                       // Factory zur Erstellung von Loggern für Karteneffekte.
        private readonly IHttpClientFactory _httpClientFactory;                         // Factory zur Erstellung von HttpClient-Instanzen für API-Aufrufe.
        private readonly object _sessionLock = new object();                            // Lock-Objekt zur Gewährleistung der Threadsicherheit bei Zustandsänderungen.
        private readonly SemaphoreSlim _activateCardSemaphore = new SemaphoreSlim(1, 1); // Semaphor zur Verhinderung gleichzeitiger Kartenaktivierungen.

        // --- Spieler & Gegner ---
        private readonly Dictionary<Guid, (string Name, Player Color)> _players = new(); // Speichert Spieler-IDs mit ihren Namen und Farben.
        private Guid _firstPlayerId = Guid.Empty;                                       // Die ID des ersten Spielers (Ersteller).
        private Player _firstPlayerActualColor;                                         // Die tatsächlich zugewiesene Farbe des ersten Spielers.
        private Guid? _playerWhiteId;                                                   // Die ID des weissen Spielers.
        private Guid? _playerBlackId;                                                   // Die ID des schwarzen Spielers.
        private readonly string _opponentType;                                          // Der Typ des Gegners ("Human" oder "Computer").
        private readonly string _computerDifficultyString;                              // Die Schwierigkeitsstufe des Computers.
        private Guid? _computerPlayerId;                                                // Die ID des Computer-Gegners, falls vorhanden.

        // --- Spielverlauf & Züge ---
        private int _moveCounter;                                                       // Zähler für die Anzahl der getätigten Züge.
        private readonly GameHistoryDto _gameHistory;                                   // DTO zur Speicherung des gesamten Spielverlaufs.
        private readonly List<PlayedMoveDto> _playedMoves = new List<PlayedMoveDto>();   // Liste aller ausgeführten Züge.

        // --- Kartenlogik ---
        private readonly Random _random = new Random();                                 // Zufallsgenerator, z.B. für Kartentausch.
        private readonly List<CardDto> _allCardDefinitions;                             // Eine Master-Liste aller verfügbaren Kartentypen.
        private readonly Dictionary<string, ICardEffect> _cardEffects;                  // Abbildung von Karten-IDs auf ihre Effekt-Implementierungen.
        private readonly Dictionary<Guid, List<CardDto>> _playerDrawPiles = new();      // Nachziehstapel für jeden Spieler.
        private readonly Dictionary<Guid, List<CardDto>> _playerHands = new();          // Handkarten für jeden Spieler.
        private readonly Dictionary<Player, List<PieceType>> _capturedPieces = new()    // Liste der geschlagenen Figuren pro Spielerfarbe.
        {
            { Player.White, new List<PieceType>() },
            { Player.Black, new List<PieceType>() }
        };
        private readonly Dictionary<Guid, string?> _pendingCardEffectForNextMove = new(); // Speichert anstehende Karteneffekte (z.B. Extrazug).
        private readonly Dictionary<Guid, HashSet<string>> _usedGlobalCardsPerPlayer = new(); // Verfolgt global verwendete Karten pro Spieler.
        private readonly Dictionary<Guid, int> _playerMoveCounts = new();               // Zählt die Züge jedes Spielers, um Karten zu verdienen.
        private readonly List<PlayedCardDto> _playedCardsHistory = new();               // Liste aller im Spiel aktivierten Karten.
        private const int InitialHandSize = 3;                                          // Die Anzahl der Karten, die jeder Spieler zu Beginn erhält.

        #endregion

        #region Public Properties

        // Gibt die eindeutige ID dieser Spielsitzung zurück.
        public Guid GameId => _gameIdInternal;
        // Gibt die aktuelle Anzahl der Spieler in der Sitzung zurück.
        public int PlayerCount { get { lock (_sessionLock) { return _players.Count; } } }
        // Gibt die ID des ersten Spielers (Ersteller) zurück.
        public Guid FirstPlayerId { get { lock (_sessionLock) { return _firstPlayerId; } } }
        // Gibt die Farbe des ersten Spielers zurück.
        public Player FirstPlayerColor { get { lock (_sessionLock) { return _firstPlayerActualColor; } } }
        // Gibt an, ob ein Gegner beigetreten ist (d.h. zwei Spieler in der Session sind).
        public bool HasOpponent { get { lock (_sessionLock) { return _players.Count > 1; } } }
        // Gibt den aktuellen Kernzustand des Spiels zurück.
        public GameState CurrentGameState => _state;
        // Gibt den Timer-Service für diese Sitzung zurück.
        public GameTimerService TimerService => _timerServiceInternal;

        #endregion

        #region Constructor

        // Initialisiert eine neue Spielsitzung mit allen notwendigen Parametern und Abhängigkeiten.
        public GameSession(Guid gameId, Player initialCreatorColorPreference, string creatorName, int initialMinutes,
                           IHubContext<ChessHub> hubContext, IChessLogger logger, ILoggerFactory loggerFactory,
                           IHttpClientFactory httpClientFactory,
                           string opponentType = "Human", string computerDifficulty = "Medium")
        {
            _gameIdInternal = gameId;
            _state = new GameState(Player.White, Board.Initial());
            _hubContext = hubContext;
            _logger = logger;
            _loggerFactoryForEffects = loggerFactory;
            _httpClientFactory = httpClientFactory;
            _opponentType = opponentType;
            _computerDifficultyString = computerDifficulty;

            // Initialisiert den Timer-Service und abonniert seine Events.
            _timerServiceInternal = new GameTimerService(gameId, TimeSpan.FromMinutes(initialMinutes), loggerFactory.CreateLogger<GameTimerService>());
            _timerServiceInternal.OnTimeUpdated += HandleTimeUpdated;
            _timerServiceInternal.OnTimeExpired += HandleTimeExpired;

            // Initialisiert das DTO für den Spielverlauf.
            _gameHistory = new GameHistoryDto { GameId = _gameIdInternal, InitialTimeMinutes = initialMinutes, DateTimeStartedUtc = DateTime.UtcNow };

            // Definiert alle im Spiel verfügbaren Karten.
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

            // Ordnet jeder Karten-ID eine Implementierung des Karteneffekts zu.
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

        #endregion

        #region Game Lifecycle & Player Management

        // Ermöglicht einem Spieler, der Sitzung beizutreten. Dies ist die primäre Methode, die von aussen aufgerufen wird.
        public (Guid PlayerId, Player Color) Join(string playerName, Player? preferredColor = null)
        {
            Guid newPlayerId = Guid.NewGuid();
            Player assignedColor = Join(newPlayerId, playerName, preferredColor);
            return (newPlayerId, assignedColor);
        }

        // Interne Join-Logik, die die Spielerdaten in der Session registriert.
        public Player Join(Guid playerId, string playerName, Player? preferredColorForCreator = null)
        {
            Player assignedColor;
            bool computerInitializationShouldBeTriggered = false;

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

                    if (_players.Count == 0) // Der erste Spieler (menschlicher Ersteller) tritt bei
                    {
                        assignedColor = preferredColorForCreator ?? Player.White;
                        _firstPlayerActualColor = assignedColor;
                        _firstPlayerId = playerId;

                        if (assignedColor == Player.White) { _gameHistory.PlayerWhiteName = playerName; _playerWhiteId = playerId; }
                        else { _gameHistory.PlayerBlackName = playerName; _playerBlackId = playerId; }

                        _players[playerId] = (playerName, assignedColor);
                        _playerMoveCounts[playerId] = 0;
                        InitializeAndShufflePlayerDeck(playerId);
                        if (!_playerHands.ContainsKey(playerId))
                        {
                            _playerHands[playerId] = new List<CardDto>();
                            for (int i = 0; i < InitialHandSize; i++) { DrawCardForPlayer(playerId); }
                        }
                        if (!_capturedPieces.ContainsKey(assignedColor))
                        {
                            _capturedPieces[assignedColor] = new List<PieceType>();
                        }

                        if (_opponentType == "Computer" && _computerPlayerId == null)
                        {
                            computerInitializationShouldBeTriggered = true;
                        }
                    }
                    else // Der zweite Spieler tritt bei.
                    {
                        if (_opponentType == "Human") // Zweiter menschlicher Spieler
                        {
                            assignedColor = _firstPlayerActualColor.Opponent();
                            if ((assignedColor == Player.White && _playerWhiteId != null) || (assignedColor == Player.Black && _playerBlackId != null))
                            {
                                throw new InvalidOperationException($"Farbe {assignedColor} ist unerwartet bereits belegt.");
                            }

                            if (assignedColor == Player.White) { _gameHistory.PlayerWhiteName = playerName; _playerWhiteId = playerId; }
                            else { _gameHistory.PlayerBlackName = playerName; _playerBlackId = playerId; }

                            _players[playerId] = (playerName, assignedColor);
                            _playerMoveCounts[playerId] = 0;
                            InitializeAndShufflePlayerDeck(playerId);
                            if (!_playerHands.ContainsKey(playerId)) { _playerHands[playerId] = new List<CardDto>(); for (int i = 0; i < InitialHandSize; i++) { DrawCardForPlayer(playerId); } }
                            if (!_capturedPieces.ContainsKey(assignedColor)) { _capturedPieces[assignedColor] = new List<PieceType>(); }
                        }
                        else // Computerspiel, und ein zweiter Mensch versucht beizutreten -> Fehler
                        {
                            throw new InvalidOperationException("Ein Computerspiel kann nur einen menschlichen Spieler haben, ein zweiter menschlicher Spieler kann nicht beitreten.");
                        }
                    }
                }
            }

            if (computerInitializationShouldBeTriggered)
            {
                InitializeComputerPlayer();
            }

            return assignedColor;
        }

        // Gibt zurück, ob die Sitzung beendet ist.
        public bool IsGameReallyOver() { lock (_sessionLock) { return _state.IsGameOver(); } }

        // Gibt die Farbe des Spielers anhand seiner ID zurück.
        public Player GetPlayerColor(Guid playerId)
        {
            lock (_sessionLock)
            {
                if (_players.TryGetValue(playerId, out var playerData))
                {
                    return playerData.Color;
                }

                if (_opponentType == "Computer" && _computerPlayerId.HasValue && playerId == _computerPlayerId.Value)
                {
                    if (_players.TryGetValue(_firstPlayerId, out var humanData))
                    {
                        return humanData.Color.Opponent();
                    }
                }
                if (playerId == _firstPlayerId && _players.Count <= 1)
                {
                    return _firstPlayerActualColor;
                }

                _logger.LogSessionColorNotDetermined(_gameIdInternal, playerId, _players.Count);
                throw new InvalidOperationException($"Spieler mit ID {playerId} nicht in der Session {_gameIdInternal} gefunden oder Farbe noch nicht eindeutig zugewiesen. Aktuelle Spieler: {string.Join(", ", _players.Select(p => $"{p.Key}={p.Value.Name}({p.Value.Color})"))}");
            }
        }

        // Gibt die ID des Spielers anhand seiner Farbe zurück.
        public Guid? GetPlayerIdByColor(Player color)
        {
            lock (_sessionLock)
            {
                if (color == Player.White) return _playerWhiteId;
                if (color == Player.Black) return _playerBlackId;
                _logger.LogGetPlayerIdByColorFailed(_gameIdInternal, color, _playerWhiteId, _playerBlackId);
                return null;
            }
        }

        // Gibt den Namen des Spielers anhand seiner ID zurück.
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

        // Gibt Informationen über den Gegner zurück.
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
                Guid? opponentId = GetPlayerIdByColor(opponentColor);

                if (opponentId.HasValue && _players.TryGetValue(opponentId.Value, out var opponentData))
                {
                    return new OpponentInfoDto(opponentId.Value, opponentData.Name, opponentColor);
                }

                _logger.LogNoOpponentFoundForPlayer(currentPlayerId, currentPlayerDetails.Color, _gameIdInternal);
                return null;
            }
        }

        #endregion

        #region Move & Card Logic

        // Verarbeitet einen vom Client gesendeten Zug, validiert ihn und aktualisiert den Spielzustand.
        public MoveResultDto MakeMove(MoveDto dto, Guid playerIdCalling)
        {
            Player playerColorMakingTheMove;
            lock (_sessionLock)
            {
                try { playerColorMakingTheMove = GetPlayerColor(playerIdCalling); }
                catch (InvalidOperationException ex)
                {
                    _logger.LogSessionErrorGetNameByColor(_gameIdInternal, ex);
                    return new MoveResultDto { IsValid = false, ErrorMessage = "Interner Fehler: Spieler-ID nicht dem Spiel zugeordnet.", NewBoard = ToBoardDto(), Status = GameStatusDto.None, IsYourTurn = false };
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

            // Sonderfall: Bauernumwandlung, bei der die Figur auf dem Feld bleibt (z.B. nach Karteneffekt).
            if (dto.PromotionTo.HasValue && fromPos == toPos)
            {
                Piece? pieceOnSquare;
                lock (_sessionLock) { pieceOnSquare = _state.Board[fromPos]; }

                if (pieceOnSquare != null && pieceOnSquare.Type == PieceType.Pawn && pieceOnSquare.Color == playerColorMakingTheMove)
                {
                    int promotionRank = (playerColorMakingTheMove == Player.White) ? 0 : 7;
                    if (fromPos.Row == promotionRank)
                    {
                        legalMove = new PawnPromotion(fromPos, toPos, dto.PromotionTo.Value);
                        Board boardCopy;
                        lock (_sessionLock) { boardCopy = _state.Board.Copy(); }
                        legalMove.Execute(boardCopy);
                        if (boardCopy.IsInCheck(playerColorMakingTheMove))
                        {
                            _logger.LogPlayerInCheckTriedInvalidMove(_gameIdInternal, playerIdCalling, playerColorMakingTheMove, dto.From, dto.To);
                            return new MoveResultDto { IsValid = false, ErrorMessage = "Umwandlung nicht möglich, da eigener König dadurch ins Schach geraten würde.", NewBoard = ToBoardDto(), IsYourTurn = true, Status = GetStatusForPlayer(playerColorMakingTheMove) };
                        }
                        _logger.LogPawnPromotionMoveFound(_gameIdInternal, dto.From, dto.To, dto.PromotionTo.Value);
                    }
                }
            }

            // Findet den legalen Zug aus den Kandidaten.
            if (legalMove == null)
            {
                IEnumerable<Move> candidateMoves;
                lock (_sessionLock)
                {
                    candidateMoves = _state.LegalMovesForPiece(fromPos);
                }

                if (dto.PromotionTo.HasValue)
                {
                    _logger.LogPawnPromotionMoveSelection(_gameIdInternal, dto.From, dto.To, dto.PromotionTo);
                    legalMove = candidateMoves.OfType<PawnPromotion>().FirstOrDefault(m => m.ToPos == toPos && m.PromotionTo == dto.PromotionTo.Value);
                    if (legalMove != null) _logger.LogPawnPromotionMoveFound(_gameIdInternal, dto.From, dto.To, dto.PromotionTo.Value);
                    else _logger.LogPawnPromotionMoveNotFound(_gameIdInternal, dto.From, dto.To, dto.PromotionTo);
                }
                else
                {
                    legalMove = candidateMoves.FirstOrDefault(m => m.ToPos == toPos && !(m is PawnPromotion));
                }
            }

            if (legalMove == null)
            {
                if (isInCheckBeforeMove) _logger.LogPlayerInCheckTriedInvalidMove(_gameIdInternal, playerIdCalling, playerColorMakingTheMove, dto.From, dto.To);
                return new MoveResultDto { IsValid = false, ErrorMessage = "Ungültiger Zug.", NewBoard = ToBoardDto(), IsYourTurn = IsPlayerTurn(playerIdCalling), Status = GameStatusDto.None };
            }

            // Prüft, ob der Zug Teil eines Extrazug-Effekts ist und ob er den Gegner ins Schach setzt.
            bool isFirstMoveOfExtraTurn = false;
            lock (_sessionLock)
            {
                if (playerIdCalling != Guid.Empty && _pendingCardEffectForNextMove.TryGetValue(playerIdCalling, out var effectCardId))
                {
                    if (effectCardId == CardConstants.ExtraZug)
                    {
                        isFirstMoveOfExtraTurn = true;
                    }
                }
            }

            if (isFirstMoveOfExtraTurn)
            {
                Board boardCopy;
                lock (_sessionLock) { boardCopy = _state.Board.Copy(); }
                legalMove.Execute(boardCopy);
                if (boardCopy.IsInCheck(playerColorMakingTheMove.Opponent()))
                {
                    _logger.LogExtraTurnFirstMoveCausesCheck(_gameIdInternal, playerIdCalling, dto.From, dto.To);
                    return new MoveResultDto
                    {
                        IsValid = false,
                        ErrorMessage = "Erster Zug der Extrazug-Karte darf den gegnerischen König nicht ins Schach setzen.",
                        NewBoard = ToBoardDto(),
                        IsYourTurn = true,
                        Status = GetStatusForPlayer(playerColorMakingTheMove)
                    };
                }
            }

            // Führt den Zug aus und aktualisiert den Zustand.
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

            // Fügt geschlagene Figuren der Liste hinzu.
            lock (_capturedPieces)
            {
                if (pieceAtDestination != null && pieceAtDestination.Type != PieceType.Pawn && legalMove.Type != MoveType.EnPassant)
                {
                    _capturedPieces[pieceAtDestination.Color].Add(pieceAtDestination.Type);
                    _logger.LogCapturedPieceAdded(_gameIdInternal, pieceAtDestination.Type, pieceAtDestination.Color);
                }
                else if (legalMove.Type == MoveType.EnPassant)
                {
                    _capturedPieces[playerColorMakingTheMove.Opponent()].Add(PieceType.Pawn);
                    _logger.LogCapturedPieceAdded(_gameIdInternal, PieceType.Pawn, playerColorMakingTheMove.Opponent());
                }
            }

            // Behandelt Nebeneffekte des Zugs, wie Kartenziehen oder Extrazüge.
            bool extraTurnGrantedByCard = false;
            Guid? playerIdToSignalDraw = null;
            CardDto? newlyDrawnCardFromMove = null;
            MoveResultDto moveResultDtoToReturn;
            lock (_sessionLock)
            {
                if (isFirstMoveOfExtraTurn)
                {
                    _state.SetCurrentPlayerOverride(playerColorMakingTheMove);
                    _pendingCardEffectForNextMove.Remove(playerIdWhoMadeTheMove);
                    extraTurnGrantedByCard = true;
                    _logger.LogExtraTurnEffectApplied(_gameIdInternal, playerIdWhoMadeTheMove, CardConstants.ExtraZug);
                }

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

                string? movedPieceString = pieceBeingMoved != null ? $"{pieceBeingMoved.Color} {pieceBeingMoved.Type}" : "Unknown";
                string? capturedPieceString = null;
                if (legalMove.Type == MoveType.EnPassant) capturedPieceString = $"{playerColorMakingTheMove.Opponent()} Pawn (EP)";
                else if (pieceAtDestination != null) capturedPieceString = $"{pieceAtDestination.Color} {pieceAtDestination.Type}";

                // Aktualisiert die Zughistorie.
                var playedMove = new PlayedMoveDto
                {
                    MoveNumber = _moveCounter,
                    PlayerId = playerIdWhoMadeTheMove,
                    PlayerColor = playerColorMakingTheMove,
                    From = dto.From,
                    To = dto.To,
                    ActualMoveType = legalMove.Type,
                    PromotionPiece = (legalMove is PawnPromotion promoMove) ? promoMove.PromotionTo : null,
                    TimestampUtc = moveTimestampUtc,
                    TimeTaken = elapsedSinceLastTick,
                    RemainingTimeWhite = _timerServiceInternal.GetCurrentTimeForPlayer(Player.White),
                    RemainingTimeBlack = _timerServiceInternal.GetCurrentTimeForPlayer(Player.Black),
                    PieceMoved = movedPieceString,
                    CapturedPiece = capturedPieceString
                };
                _playedMoves.Add(playedMove);

                bool isGameOverNow = _state.IsGameOver();

                if (isGameOverNow)
                {
                    UpdateHistoryOnGameOver();
                    NotifyTimerGameOver();
                }

                moveResultDtoToReturn = new MoveResultDto
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
                if (!extraTurnGrantedByCard)
                {
                    SwitchPlayerTimer();
                }
                else
                {
                    StartGameTimer();
                }
            }

            // Prüft, ob nach diesem Zug der Computer an der Reihe ist.
            bool computerShouldMoveAfterThis = false;
            lock (_sessionLock)
            {
                Player currentTurnPlayerNow = _state.CurrentPlayer;
                Guid? currentComputerPlayerId = _computerPlayerId;
                Player? computerActualColor = null;
                if (currentComputerPlayerId.HasValue && _players.TryGetValue(currentComputerPlayerId.Value, out var compData))
                {
                    computerActualColor = compData.Color;
                }

                if (!extraTurnGrantedByCard &&
                    _opponentType == "Computer" &&
                    currentComputerPlayerId.HasValue &&
                    computerActualColor.HasValue &&
                    currentTurnPlayerNow == computerActualColor.Value &&
                    !_state.IsGameOver())
                {
                    computerShouldMoveAfterThis = true;
                }
            }

            if (computerShouldMoveAfterThis)
            {
                Task.Run(async () => await ProcessComputerTurnIfNeeded());
            }

            return moveResultDtoToReturn;
        }

        // Startet das Spiel und den Timer, wenn zwei Spieler beigetreten sind und das Spiel nicht beendet ist.
        public void StartTheGameAndTimer()
        {
            lock (_sessionLock)
            {
                if (_players.Count == 2 && !_state.IsGameOver())
                {
                    var turn = GetCurrentTurnPlayerLogic();
                    var opponentData = _players.Values.FirstOrDefault(p => p.Color == turn);
                    var opponentName = opponentData.Name ?? "Unbekannt";

                    _logger.LogMgrPlayerJoinedGameTimerStart(opponentName, _gameIdInternal, turn);
                    StartGameTimer();
                }
            }
        }

        // Verarbeitet die Aktivierung einer Spezialkarte.
        public async Task<ServerCardActivationResultDto> ActivateCard(Guid playerId, ActivateCardRequestDto dto)
        {
            Guid cardInstanceId = dto.CardInstanceId;
            string cardTypeId = dto.CardTypeId;

            await _activateCardSemaphore.WaitAsync();
            bool timerWasManuallyPaused = false;
            CardDto? newlyDrawnCardByEffect = null;
            CardDto? cardGivenForSwapEffectResult = null;
            CardDto? cardReceivedForSwapEffectResult = null;
            Position? promotionSquareAfterCard = null;
            bool turnActuallyEnds;

            Player activatingPlayerDataColor;
            string activatingPlayerOriginalName;
            BoardDto? boardDtoForSignalR = null;
            Player nextPlayerForSignalR_CardActivation = Player.None;
            GameStatusDto statusForNextPlayerSignalR_CardActivation = GameStatusDto.None;
            List<AffectedSquareInfo>? affectedSquaresForSignalR_CardActivation = null;
            CardDto? cardPlayedForSignalR_CardActivation = null;
            ServerCardActivationResultDto serverResultDtoToReturn;
            try
            {
                _logger.LogSessionCardActivationAttempt(_gameIdInternal, playerId, cardTypeId);
                bool playerWasInCheckBeforeCard;

                lock (_sessionLock)
                {
                    if (!_players.TryGetValue(playerId, out var pData))
                    {
                        _logger.LogSessionCardActivationFailed(_gameIdInternal, playerId, cardTypeId, "Spieler nicht gefunden.");
                        return new ServerCardActivationResultDto { Success = false, ErrorMessage = "Spieler nicht in dieser Session.", CardId = cardTypeId };
                    }
                    activatingPlayerDataColor = pData.Color;
                    activatingPlayerOriginalName = pData.Name;
                    playerWasInCheckBeforeCard = _state.Board.IsInCheck(activatingPlayerDataColor);

                    if (playerWasInCheckBeforeCard)
                    {
                        _logger.LogPlayerAttemptingCardWhileInCheck(_gameIdInternal, playerId, activatingPlayerDataColor, cardTypeId);
                    }
                }

                // Validiert die Kartenaktivierung.
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
                    return new ServerCardActivationResultDto { Success = false, ErrorMessage = "Gespielte Karte (Instanz oder Typ) nicht auf der Hand des Spielers oder inkonsistent.", CardId = cardTypeId };
                }

                if (activatingPlayerDataColor != _state.CurrentPlayer)
                {
                    _logger.LogSessionCardActivationFailed(_gameIdInternal, playerId, cardTypeId, "Spieler ist nicht am Zug.");
                    return new ServerCardActivationResultDto { Success = false, ErrorMessage = "Nicht dein Zug.", CardId = cardTypeId };
                }
                if (!_cardEffects.TryGetValue(cardTypeId, out var effect))
                {
                    _logger.LogSessionCardActivationFailed(_gameIdInternal, playerId, cardTypeId, "Unbekannte oder nicht serverseitig implementierte Karte.");
                    return new ServerCardActivationResultDto { Success = false, ErrorMessage = "Unbekannte oder nicht implementierte Karte.", CardId = cardTypeId };
                }

                lock (_sessionLock)
                {
                    if (!_state.IsGameOver() && _state.CurrentPlayer == activatingPlayerDataColor && !_timerServiceInternal.IsPaused)
                    {
                        _timerServiceInternal.PauseTimer();
                        timerWasManuallyPaused = true;
                    }
                }

                // Führt den spezifischen Karteneffekt aus.
                string? param1ForEffect;
                string? param2ForEffect;

                if (cardTypeId == CardConstants.Wiedergeburt) { param1ForEffect = dto.PieceTypeToRevive?.ToString(); param2ForEffect = dto.TargetRevivalSquare; }
                else if (cardTypeId == CardConstants.CardSwap) { param1ForEffect = dto.CardInstanceIdToSwapFromHand?.ToString(); param2ForEffect = null; }
                else { param1ForEffect = dto.FromSquare; param2ForEffect = dto.ToSquare; }

                if (cardTypeId == CardConstants.SacrificeEffect) { /* ... */ }

                CardActivationResult effectResult = effect.Execute(this, playerId, activatingPlayerDataColor, cardTypeId, param1ForEffect, param2ForEffect);
                turnActuallyEnds = effectResult.EndsPlayerTurn;


                if (effectResult.Success && playedCardInstance != null)
                {
                    RemoveCardFromPlayerHand(playerId, cardInstanceId);
                    _logger.LogCardInstancePlayed(cardInstanceId, playerId, cardTypeId, _gameIdInternal.ToString());
                    PlayedCardDto currentPlayedCardEntry = new PlayedCardDto { MoveNumberWhenActivated = _moveCounter + 1, PlayerId = playerId, PlayerName = activatingPlayerOriginalName, PlayerColor = activatingPlayerDataColor, CardId = cardTypeId, CardName = playedCardInstance.Name, TimestampUtc = DateTime.UtcNow };
                    lock (_sessionLock) { _playedCardsHistory.Add(currentPlayedCardEntry); }
                    MarkCardAsUsedGlobal(playerId, cardTypeId);
                    cardPlayedForSignalR_CardActivation = playedCardInstance;
                }

                if (!effectResult.Success)
                {
                    _logger.LogSessionCardActivationFailed(_gameIdInternal, playerId, cardTypeId, effectResult.ErrorMessage ?? "Kartenaktivierung durch Effektimplementierung fehlgeschlagen.");
                    lock (_sessionLock) { if (timerWasManuallyPaused) _timerServiceInternal.ResumeTimer(); }

                    // Spezialfall: Karte wird bei Fehlschlag verbraucht (z.B. Wiedergeburt auf besetztes Feld).
                    bool consumeCardOnFailure = cardTypeId == CardConstants.Wiedergeburt &&
                                               (effectResult.ErrorMessage?.Contains("besetzt") == true ||
                                                effectResult.ErrorMessage?.Contains("Ursprungsfeld") == true ||
                                                effectResult.ErrorMessage?.Contains("Keine wiederbelebungsfähigen") == true);
                    if (consumeCardOnFailure && playedCardInstance != null)
                    {
                        RemoveCardFromPlayerHand(playerId, cardInstanceId);
                        MarkCardAsUsedGlobal(playerId, cardTypeId);
                        PlayedCardDto failedCardEntry = new PlayedCardDto { MoveNumberWhenActivated = _moveCounter + 1, PlayerId = playerId, PlayerName = activatingPlayerOriginalName, PlayerColor = activatingPlayerDataColor, CardId = cardTypeId, CardName = playedCardInstance.Name, TimestampUtc = DateTime.UtcNow };
                        lock (_sessionLock) { _playedCardsHistory.Add(failedCardEntry); }
                        cardPlayedForSignalR_CardActivation = playedCardInstance;
                        serverResultDtoToReturn = new ServerCardActivationResultDto { Success = true, ErrorMessage = effectResult.ErrorMessage, CardId = cardTypeId, EndsPlayerTurn = true, BoardUpdatedByCardEffect = false };
                    }
                    else
                    {
                        serverResultDtoToReturn = new ServerCardActivationResultDto { Success = false, ErrorMessage = effectResult.ErrorMessage ?? "Kartenaktivierung fehlgeschlagen.", CardId = cardTypeId };
                    }
                }
                else
                {
                    // Behandelt den Zustand nach erfolgreicher Kartenaktivierung.
                    if (effectResult.BoardUpdatedByCardEffect)
                    {
                        promotionSquareAfterCard = CheckForPawnPromotion(activatingPlayerDataColor);
                        if (promotionSquareAfterCard != null)
                        {
                            _logger.LogPawnPromotionPendingAfterCard(_gameIdInternal, activatingPlayerDataColor, PieceHelper.ToAlgebraic(promotionSquareAfterCard), cardTypeId);
                            turnActuallyEnds = false;
                        }
                    }

                    bool playerStillInCheckAfterCardExecution;
                    lock (_sessionLock)
                    {
                        playerStillInCheckAfterCardExecution = _state.Board.IsInCheck(activatingPlayerDataColor);
                    }

                    // Prüft, ob der Spieler sich aus einem Schach befreien konnte.
                    if (playerWasInCheckBeforeCard && playerStillInCheckAfterCardExecution && !_state.IsGameOver())
                    {
                        _logger.LogPlayerStillInCheckAfterCardTurnNotEnded(_gameIdInternal, playerId, cardTypeId);
                        lock (_sessionLock)
                        {
                            _state.SetResult(Result.Win(activatingPlayerDataColor.Opponent(), EndReason.Checkmate));
                            UpdateHistoryOnGameOver();
                            NotifyTimerGameOver();
                            boardDtoForSignalR = ToBoardDto();
                            nextPlayerForSignalR_CardActivation = _state.CurrentPlayer.Opponent();
                            statusForNextPlayerSignalR_CardActivation = GameStatusDto.Checkmate;
                            affectedSquaresForSignalR_CardActivation = effectResult.AffectedSquaresByCard;
                        }
                        serverResultDtoToReturn = new ServerCardActivationResultDto { Success = true, ErrorMessage = $"Du warst im Schach, hast '{playedCardInstance?.Name ?? cardTypeId}' gespielt und stehst immer noch im Schach. Du hast verloren!", CardId = cardTypeId, AffectedSquaresByCard = effectResult.AffectedSquaresByCard, EndsPlayerTurn = true, BoardUpdatedByCardEffect = effectResult.BoardUpdatedByCardEffect, PlayerIdToSignalCardDraw = null, NewlyDrawnCard = null, PawnPromotionPendingAt = null };
                    }
                    else
                    {
                        _logger.LogNotifyingOpponentOfCardPlay(_gameIdInternal, playerId, cardTypeId);
                        if (cardTypeId == CardConstants.CardSwap)
                        {
                            cardGivenForSwapEffectResult = effectResult.CardGivenByPlayerForSwapEffect;
                            cardReceivedForSwapEffectResult = effectResult.CardReceivedByPlayerForSwapEffect;
                        }

                        affectedSquaresForSignalR_CardActivation = effectResult.AffectedSquaresByCard;
                        bool affectsRepetitionHistoryForThisCard = effectResult.BoardUpdatedByCardEffect;
                        Move? cardEffectAsMove = null;

                        lock (_sessionLock)
                        {
                            if (turnActuallyEnds)
                            {
                                bool captureOrPawnLikeEffect = affectsRepetitionHistoryForThisCard && (cardTypeId == CardConstants.SacrificeEffect);
                                _state.UpdateStateAfterMove(captureOrPawnLikeEffect, affectsRepetitionHistoryForThisCard, cardEffectAsMove);
                            }
                            else
                            {
                                if (affectsRepetitionHistoryForThisCard)
                                {
                                    _state.RecordCurrentStateForRepetition(cardEffectAsMove);
                                }
                            }

                            if (!_state.IsGameOver()) _state.CheckForGameOver();
                            if (_state.IsGameOver())
                            {
                                UpdateHistoryOnGameOver();
                                NotifyTimerGameOver();
                            }
                            else
                            {
                                if (turnActuallyEnds) SwitchPlayerTimer();
                                else
                                {
                                    if (timerWasManuallyPaused) _timerServiceInternal.ResumeTimer();
                                    else StartGameTimer();
                                }
                            }
                            boardDtoForSignalR = ToBoardDto();
                            nextPlayerForSignalR_CardActivation = _state.CurrentPlayer;
                            statusForNextPlayerSignalR_CardActivation = GetStatusForPlayer(nextPlayerForSignalR_CardActivation);
                        }

                        if (effectResult.PlayerIdToSignalDraw.HasValue)
                        {
                            newlyDrawnCardByEffect = DrawCardForPlayer(effectResult.PlayerIdToSignalDraw.Value);
                        }
                        _logger.LogSessionCardActivationSuccess(_gameIdInternal, playerId, cardTypeId);
                        serverResultDtoToReturn = new ServerCardActivationResultDto
                        {
                            Success = true,
                            ErrorMessage = effectResult.ErrorMessage,
                            CardId = cardTypeId,
                            AffectedSquaresByCard = affectedSquaresForSignalR_CardActivation,
                            EndsPlayerTurn = turnActuallyEnds,
                            BoardUpdatedByCardEffect = effectResult.BoardUpdatedByCardEffect,
                            PlayerIdToSignalCardDraw = effectResult.PlayerIdToSignalDraw,
                            NewlyDrawnCard = newlyDrawnCardByEffect,
                            CardGivenByPlayerForSwap = cardGivenForSwapEffectResult,
                            CardReceivedByPlayerForSwap = cardReceivedForSwapEffectResult,
                            PawnPromotionPendingAt = promotionSquareAfterCard != null ? new PositionDto(promotionSquareAfterCard.Row, promotionSquareAfterCard.Column) : null
                        };
                    }
                }

                // Sendet Benachrichtigungen an die Clients.
                if (cardPlayedForSignalR_CardActivation != null)
                {
                    await _hubContext.Clients.Group(_gameIdInternal.ToString()).SendAsync("OnCardPlayed", playerId, cardPlayedForSignalR_CardActivation);
                }

                if (serverResultDtoToReturn.Success)
                {
                    boardDtoForSignalR ??= ToBoardDto();
                    if (nextPlayerForSignalR_CardActivation == Player.None && serverResultDtoToReturn.Success)
                    {
                        lock (_sessionLock) { nextPlayerForSignalR_CardActivation = _state.CurrentPlayer; }
                        statusForNextPlayerSignalR_CardActivation = GetStatusForPlayer(nextPlayerForSignalR_CardActivation);
                    }

                    await SendOnTurnChangedNotification(boardDtoForSignalR, nextPlayerForSignalR_CardActivation, statusForNextPlayerSignalR_CardActivation, null, null, affectedSquaresForSignalR_CardActivation);
                    if (serverResultDtoToReturn.PlayerIdToSignalCardDraw.HasValue && serverResultDtoToReturn.NewlyDrawnCard != null)
                    {
                        await SignalCardDrawnToPlayer(serverResultDtoToReturn.PlayerIdToSignalCardDraw.Value, serverResultDtoToReturn.NewlyDrawnCard, "ActivateCard");
                    }
                }

                // Prüft, ob nach der Kartenaktion der Computergegner am Zug ist.
                bool humanTurnEndedEffectively = serverResultDtoToReturn.Success && turnActuallyEnds;
                bool computerIsNextAndGameIsPvC;
                Player computerColorForTurnCheck = Player.None;

                lock (_sessionLock)
                {
                    Player currentPlayerAfterCardLogic = _state.CurrentPlayer;
                    Guid? currentComputerId = _computerPlayerId;
                    Player? actualComputerColor = null;
                    if (currentComputerId.HasValue && _players.TryGetValue(currentComputerId.Value, out var compData))
                    {
                        actualComputerColor = compData.Color;
                        if (actualComputerColor.HasValue)
                        {
                            computerColorForTurnCheck = actualComputerColor.Value;
                        }
                    }

                    computerIsNextAndGameIsPvC = _opponentType == "Computer" &&
                                             currentComputerId.HasValue &&
                                             actualComputerColor.HasValue &&
                                             currentPlayerAfterCardLogic == actualComputerColor.Value &&
                                             !_state.IsGameOver();
                }

                if (humanTurnEndedEffectively && computerIsNextAndGameIsPvC)
                {
                    if (computerColorForTurnCheck != Player.None)
                    {
                        _timerServiceInternal.PauseTimer();
                        _logger.LogComputerTimerPausedForAnimation(_gameIdInternal, computerColorForTurnCheck);
                    }


                    TimeSpan animationDelay;
                    string cardIdForDelay = cardTypeId;

                    if (cardIdForDelay == CardConstants.CardSwap)
                    {
                        animationDelay = TimeSpan.FromSeconds(6.0);
                        _logger.LogComputerTurnDelayCardSwap(_gameIdInternal, animationDelay.TotalSeconds);
                    }
                    else
                    {
                        animationDelay = TimeSpan.FromSeconds(3.5);
                        _logger.LogComputerTurnDelayAfterCard(_gameIdInternal, cardIdForDelay, animationDelay.TotalSeconds);
                    }
                    await Task.Delay(animationDelay);

                    bool stillComputersTurnAndNotGameOverAndCorrectColor;
                    lock (_sessionLock)
                    {
                        stillComputersTurnAndNotGameOverAndCorrectColor = !_state.IsGameOver() &&
                                                                           _state.CurrentPlayer == computerColorForTurnCheck &&
                                                                          computerColorForTurnCheck != Player.None;
                    }

                    if (stillComputersTurnAndNotGameOverAndCorrectColor)
                    {
                        if (computerColorForTurnCheck != Player.None)
                        {
                            _timerServiceInternal.ResumeTimer();
                            _logger.LogComputerTimerResumedAfterAnimation(_gameIdInternal, computerColorForTurnCheck);
                        }
                        await ProcessComputerTurnIfNeeded();
                    }
                    else
                    {
                        _logger.LogComputerSkippingTurnAfterAnimationDelay(_gameIdInternal, cardIdForDelay);
                    }
                }
                return serverResultDtoToReturn;

            }
            catch (Exception ex)
            {
                _logger.LogSessionCardActivationFailed(_gameIdInternal, playerId, cardTypeId, $"Unerwarteter Fehler: {ex.Message} - StackTrace: {ex.StackTrace}");
                lock (_sessionLock) { if (timerWasManuallyPaused) _timerServiceInternal.ResumeTimer(); }
                return new ServerCardActivationResultDto { Success = false, ErrorMessage = "Ein unerwarteter interner Fehler ist aufgetreten.", CardId = cardTypeId };
            }
            finally
            {
                if (_activateCardSemaphore.CurrentCount == 0)
                {
                    _activateCardSemaphore.Release();
                }
            }
        }

        #endregion

        #region Card Management

        // Initialisiert das Kartendeck für einen Spieler und mischt es.
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
            while (n > 1) { n--; int k = _random.Next(n + 1); (newDeckForPlayer[k], newDeckForPlayer[n]) = (newDeckForPlayer[n], newDeckForPlayer[k]); }
            _playerDrawPiles[playerId] = newDeckForPlayer;
            _logger.LogPlayerDeckInitialized(playerId, _gameIdInternal, newDeckForPlayer.Count);
        }

        // Zieht die oberste Karte vom Nachziehstapel eines Spielers und fügt sie seiner Hand hinzu.
        public CardDto? DrawCardForPlayer(Guid playerId)
        {
            lock (_sessionLock)
            {
                if (!_players.ContainsKey(playerId)) { _logger.LogDrawAttemptUnknownPlayer(playerId, _gameIdInternal); return null; }
                if (!_playerDrawPiles.TryGetValue(playerId, out var specificDrawPile))
                {
                    _logger.LogNoDrawPileForPlayer(playerId, _gameIdInternal);
                    InitializeAndShufflePlayerDeck(playerId);
                    if (!_playerDrawPiles.TryGetValue(playerId, out specificDrawPile) || specificDrawPile == null)
                        return new CardDto { InstanceId = Guid.NewGuid(), Id = $"{CardConstants.FallbackCardIdPrefix}error", Name = "Fehler", Description = "Deck nicht initialisiert.", ImageUrl = CardConstants.DefaultCardBackImageUrl };
                }
                if (specificDrawPile.Count == 0) { _logger.LogPlayerDrawPileEmpty(playerId, _gameIdInternal); return new CardDto { InstanceId = Guid.NewGuid(), Id = $"{CardConstants.FallbackCardIdPrefix}empty_{playerId}", Name = CardConstants.NoMoreCardsName, Description = "Dein Nachziehstapel ist leer.", ImageUrl = CardConstants.DefaultCardBackImageUrl }; }
                CardDto drawnCard = specificDrawPile.First();
                specificDrawPile.RemoveAt(0);
                if (!_playerHands.TryGetValue(playerId, out var hand)) { hand = new List<CardDto>(); _playerHands[playerId] = hand; }
                hand.Add(drawnCard);
                _logger.LogPlayerDrewCardFromOwnDeck(playerId, drawnCard.Name, drawnCard.Id.ToString(), _gameIdInternal, specificDrawPile.Count);
                return drawnCard;
            }
        }

        // Fügt eine spezifische Karte zur Hand eines Spielers hinzu.
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

        // Entfernt eine Karte von der Hand eines Spielers anhand ihrer Instanz-ID.
        public bool RemoveCardFromPlayerHand(Guid playerId, Guid cardInstanceIdToRemove)
        {
            lock (_sessionLock)
            {
                if (_playerHands.TryGetValue(playerId, out var hand))
                {
                    var cardInstance = hand.FirstOrDefault(c => c.InstanceId == cardInstanceIdToRemove);
                    if (cardInstance != null) { return hand.Remove(cardInstance); }
                }
                _logger.LogCardInstanceNotFoundInHand(cardInstanceIdToRemove, playerId, _gameIdInternal.ToString());
                return false;
            }
        }

        // Gibt die Handkarten eines Spielers zurück.
        public List<CardDto> GetPlayerHand(Guid playerId)
        {
            lock (_sessionLock) { if (_playerHands.TryGetValue(playerId, out var hand)) return new List<CardDto>(hand); return new List<CardDto>(); }
        }

        // Gibt die Anzahl der verbleibenden Karten im Nachziehstapel eines Spielers zurück.
        public int GetDrawPileCount(Guid playerId)
        {
            lock (_sessionLock) { if (_playerDrawPiles.TryGetValue(playerId, out var pile)) return pile.Count; _logger.LogCannotFindPlayerDrawPileForCount(playerId, _gameIdInternal); return 0; }
        }

        public void SetPendingCardEffectForNextMove(Guid playerId, string cardTypeId)
        {
            lock (_sessionLock)
            {
                _pendingCardEffectForNextMove[playerId] = cardTypeId;
            }
        }

        // Ruft eine Kartendefinition für die Animationsanzeige ab.
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

        // Prüft, ob eine global limitierte Karte vom Spieler noch verwendet werden darf.
        public bool IsCardUsableGlobal(Guid playerId, string cardTypeId)
        {
            lock (_sessionLock)
            {
                return !(_usedGlobalCardsPerPlayer.TryGetValue(playerId, out var usedCards) && usedCards.Contains(cardTypeId));
            }
        }

        // Markiert eine global limitierte Karte als verwendet.
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

        #endregion

        #region Captured Pieces Management

        // Prüft, ob ein Spieler eine Figur eines bestimmten Typs geschlagen hat.
        public bool HasCapturedPieceOfType(Player ownerColor, PieceType type)
        {
            lock (_capturedPieces)
            {
                return _capturedPieces.TryGetValue(ownerColor, out var list) && list.Contains(type);
            }
        }

        // Entfernt eine geschlagene Figur aus der Liste eines Spielers (z.B. nach Wiedergeburt).
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

        // Ruft alle geschlagenen Figuren für einen Spieler ab.
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

        #endregion

        #region Computer Player Logic

        // Initialisiert den Computergegner, weist ihm eine Farbe und ein Deck zu und startet ggf. seinen ersten Zug.
        private void InitializeComputerPlayer()
        {
            Player computerColorToAssign;
            string computerNameToAssign;
            Guid computerIdToAssign;
            bool shouldComputerMoveNow = false;
            Player currentTurnAfterSetup = Player.None;
            lock (_sessionLock)
            {
                if (!(_players.Count == 1 && _computerPlayerId == null && _opponentType == "Computer"))
                {
                    return;
                }

                Player humanPlayerColor = _firstPlayerActualColor;
                computerColorToAssign = humanPlayerColor.Opponent();

                computerIdToAssign = Guid.NewGuid();
                _computerPlayerId = computerIdToAssign;
                computerNameToAssign = $"Computer ({_computerDifficultyString})";

                _players[_computerPlayerId.Value] = (computerNameToAssign, computerColorToAssign);
                _playerMoveCounts[_computerPlayerId.Value] = 0;
                if (computerColorToAssign == Player.White)
                {
                    _gameHistory.PlayerWhiteName = computerNameToAssign;
                    _playerWhiteId = _computerPlayerId;
                    if (_playerBlackId == null)
                    {
                        _playerBlackId = _firstPlayerId;
                        if (_players.TryGetValue(_firstPlayerId, out var humanData)) _gameHistory.PlayerBlackName = humanData.Name;
                    }
                }
                else
                {
                    _gameHistory.PlayerBlackName = computerNameToAssign;
                    _playerBlackId = _computerPlayerId;
                    if (_playerWhiteId == null)
                    {
                        _playerWhiteId = _firstPlayerId;
                        if (_players.TryGetValue(_firstPlayerId, out var humanData)) _gameHistory.PlayerWhiteName = humanData.Name;
                    }
                }

                InitializeAndShufflePlayerDeck(_computerPlayerId.Value);
                if (!_playerHands.ContainsKey(_computerPlayerId.Value))
                {
                    _playerHands[_computerPlayerId.Value] = new List<CardDto>();
                    for (int i = 0; i < InitialHandSize; i++)
                    {
                        DrawCardForPlayer(_computerPlayerId.Value);
                    }
                }
                if (!_capturedPieces.ContainsKey(computerColorToAssign))
                {
                    _capturedPieces[computerColorToAssign] = new List<PieceType>();
                }
            }

            _logger.LogPlayerJoinedGame(computerNameToAssign, _gameIdInternal);
            bool timerStartedForGame = false;
            lock (_sessionLock)
            {
                if (_players.Count == 2 && !_state.IsGameOver())
                {
                    currentTurnAfterSetup = GetCurrentTurnPlayerLogic();
                    _logger.LogMgrPlayerJoinedGameTimerStart(computerNameToAssign, _gameIdInternal, currentTurnAfterSetup);
                    StartGameTimer();
                    timerStartedForGame = true;

                    if (currentTurnAfterSetup == computerColorToAssign)
                    {
                        shouldComputerMoveNow = true;
                    }
                }
            }
            if (shouldComputerMoveNow && timerStartedForGame)
            {
                _logger.LogComputerStartingInitialMove(_gameIdInternal, computerColorToAssign, currentTurnAfterSetup);
                Task.Run(async () => await ProcessComputerTurnIfNeeded());
            }
        }

        // Startet den Prozess für den Zug des Computers, falls dieser am Zug ist.
        public async Task ProcessComputerTurnIfNeeded()
        {
            bool isComputerTurn;
            Guid currentComputerPlayerId = Guid.Empty;
            Player humanPlayerColorOpponent = Player.None;
            Player computerColorForTurn = Player.None;
            lock (_sessionLock)
            {
                isComputerTurn = _opponentType == "Computer" &&
                                 _computerPlayerId.HasValue &&
                                 _players.ContainsKey(_computerPlayerId.Value) &&
                                 _players[_computerPlayerId.Value].Color == _state.CurrentPlayer &&
                                 !_state.IsGameOver();
                if (isComputerTurn)
                {
                    currentComputerPlayerId = _computerPlayerId!.Value;
                    var computerData = _players[currentComputerPlayerId];
                    computerColorForTurn = computerData.Color;
                    humanPlayerColorOpponent = computerData.Color.Opponent();
                }
            }

            if (isComputerTurn)
            {
                await Task.Delay(TimeSpan.FromSeconds(1)); // Kleine Verzögerung, um realistischer zu wirken
                string? apiMoveString = await GetComputerApiMoveAsync();

                BoardDto boardAfterComputerMoveForSignalR;
                Player nextPlayerTurnAfterComputerMoveForSignalR;
                GameStatusDto statusForNextPlayerAfterComputerMoveForSignalR;
                string? lastMoveFromComputerForSignalR = null;
                string? lastMoveToComputerForSignalR = null;
                List<AffectedSquareInfo>? cardEffectsFromComputerMoveForSignalR = null;
                Guid? playerIdToDrawAfterComputerMoveForSignalR = null;
                CardDto? cardDrawnByComputerMoveForSignalR = null;
                bool computerMoveIsValid = false;
                if (apiMoveString != null && apiMoveString.Length >= 4)
                {
                    string fromAlg = apiMoveString.Substring(0, 2);
                    string toAlg = apiMoveString.Substring(2, 2);
                    PieceType? promotion = apiMoveString.Length == 5 ? ParsePromotionChar(apiMoveString[4]) : null;
                    MoveDto computerMoveDto = new MoveDto(fromAlg, toAlg, currentComputerPlayerId, promotion);
                    _logger.LogComputerMakingMove(_gameIdInternal, fromAlg, toAlg);

                    MoveResultDto result = MakeMove(computerMoveDto, currentComputerPlayerId);
                    computerMoveIsValid = result.IsValid;
                    boardAfterComputerMoveForSignalR = result.NewBoard;
                    nextPlayerTurnAfterComputerMoveForSignalR = GetCurrentTurnPlayerLogic();

                    Guid? nextPlayerId = GetPlayerIdByColor(nextPlayerTurnAfterComputerMoveForSignalR);
                    statusForNextPlayerAfterComputerMoveForSignalR = nextPlayerId.HasValue ? GetStatus(nextPlayerId.Value) : GameStatusDto.None;

                    lastMoveFromComputerForSignalR = result.LastMoveFrom;
                    lastMoveToComputerForSignalR = result.LastMoveTo;
                    cardEffectsFromComputerMoveForSignalR = result.CardEffectSquares;
                    playerIdToDrawAfterComputerMoveForSignalR = result.PlayerIdToSignalCardDraw;
                    cardDrawnByComputerMoveForSignalR = result.NewlyDrawnCard;
                    if (computerMoveIsValid)
                    {
                        await _hubContext.Clients.Group(_gameIdInternal.ToString()).SendAsync("OnTurnChanged",
                            boardAfterComputerMoveForSignalR,
                            nextPlayerTurnAfterComputerMoveForSignalR,
                            statusForNextPlayerAfterComputerMoveForSignalR,
                            lastMoveFromComputerForSignalR,
                            lastMoveToComputerForSignalR,
                            cardEffectsFromComputerMoveForSignalR);
                        _logger.LogOnTurnChangedSentToHub(_gameIdInternal);

                        var timeUpdate = _timerServiceInternal.GetCurrentTimeUpdateDto();
                        await _hubContext.Clients.Group(_gameIdInternal.ToString()).SendAsync("OnTimeUpdate", timeUpdate);
                        _logger.LogOnTimeUpdateSentAfterMove(_gameIdInternal);

                        if (playerIdToDrawAfterComputerMoveForSignalR.HasValue && cardDrawnByComputerMoveForSignalR != null)
                        {
                            await SignalCardDrawnToPlayer(playerIdToDrawAfterComputerMoveForSignalR.Value, cardDrawnByComputerMoveForSignalR, "ComputerMove");
                        }
                    }
                    else
                    {
                        _logger.LogComputerMoveError(_gameIdInternal, fen: apiMoveString, depth: 0, $"Computer API schlug ungültigen Zug vor: {apiMoveString}. Fehler: {result.ErrorMessage}");
                    }
                }
                else // apiMoveString war null oder zu kurz
                {
                    _logger.LogComputerMoveError(_gameIdInternal, "N/A", 0, $"Computer API lieferte keinen gültigen Zugstring oder fehlerhafte Antwort (apiMoveString: {apiMoveString}). Computer macht in dieser Runde keinen Zug.");
                }
            }
        }

        // Fragt eine externe API nach dem besten Zug für die aktuelle Brettstellung.
        private async Task<string?> GetComputerApiMoveAsync()
        {
            string fen;
            Player computerColorActual;
            int currentHalfMoveClock;
            int currentFullMoveNumber;

            lock (_sessionLock)
            {
                if (!_computerPlayerId.HasValue || !_players.TryGetValue(_computerPlayerId.Value, out var computerData))
                {
                    _logger.LogComputerMoveError(_gameIdInternal, "N/A", 0, "Computer-Spieler nicht initialisiert für API-Zugriff.");
                    return null;
                }
                computerColorActual = computerData.Color;
                if (_state.CurrentPlayer != computerColorActual)
                {
                    _logger.LogComputerMoveError(_gameIdInternal, "N/A", 0, "Versuch, Computerzug zu erhalten, aber Computer ist nicht am Zug.");
                    return null;
                }

                currentHalfMoveClock = _state.NoCaptureOrPawnMoves;
                currentFullMoveNumber = (_moveCounter / 2) + 1;
                fen = new StateString(_state.CurrentPlayer, _state.Board, null, currentHalfMoveClock, currentFullMoveNumber, true).ToString();
            }

            int depth = _computerDifficultyString switch { "Easy" => 1, "Medium" => 10, "Hard" => 20, _ => 10 };
            var client = _httpClientFactory.CreateClient("ChessApi");
            var requestBody = new { fen, depth };

            try
            {
                _logger.LogComputerFetchingMove(_gameIdInternal, fen, depth);
                HttpResponseMessage response = await client.PostAsJsonAsync("https://chess-api.com/v1", requestBody);

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = await response.Content.ReadFromJsonAsync<ChessApiResponseDto>();
                    if (apiResponse != null && !string.IsNullOrEmpty(apiResponse.Move))
                    {
                        _logger.LogComputerReceivedMove(_gameIdInternal, apiResponse.Move, fen, depth);
                        return apiResponse.Move;
                    }

                    string rawApiResponseForLog = "apiResponse war null";
                    if (apiResponse != null)
                    {
                        try { rawApiResponseForLog = System.Text.Json.JsonSerializer.Serialize(apiResponse); }
                        catch { rawApiResponseForLog = "Konnte apiResponse nicht serialisieren."; }
                    }
                    var errorDetail = apiResponse == null ? "Response war null" : $"Move war null/leer. Rohe Antwort (falls nicht null): {rawApiResponseForLog}";
                    _logger.LogComputerMoveError(_gameIdInternal, fen, depth, $"API-Antwort erfolgreich, aber kein Zug gefunden oder Antwort fehlerhaft. Detail: {errorDetail}");
                    return null;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogComputerMoveError(_gameIdInternal, fen, depth, $"API-Anfrage fehlgeschlagen mit Status {response.StatusCode}: {errorContent}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogComputerMoveError(_gameIdInternal, fen, depth, $"Exception während API-Aufruf: {ex.Message}");
                return null;
            }
        }

        // Interne Klasse zur Deserialisierung der Antwort der externen Schach-API.
        sealed class ChessApiResponseDto
        {
            public string? Move { get; set; }
            public string? Evaluation { get; set; }
        }

        #endregion

        #region State & History Management

        // Gibt eine Liste aller legalen Züge für eine Figur an einer bestimmten Position zurück.
        public IEnumerable<string> GetLegalMoves(string fromAlg)
        {
            var pos = ParsePos(fromAlg);
            lock (_sessionLock)
            {
                return _state.LegalMovesForPiece(pos).Select(m => PieceHelper.ToAlgebraic(m.ToPos));
            }
        }

        // Gibt den aktuellen Spielstatus für einen bestimmten Spieler zurück.
        public GameStatusDto GetStatus(Guid playerId)
        {
            Player color = GetPlayerColor(playerId);
            return GetStatusForPlayer(color);
        }

        // Gibt den Spielstatus für den Gegner des angegebenen Spielers zurück.
        public GameStatusDto GetStatusForOpponentOf(Guid lastPlayerId)
        {
            Player lastPlayerColor = GetPlayerColor(lastPlayerId);
            Player opponentColor = lastPlayerColor.Opponent();
            return GetStatusForPlayer(opponentColor);
        }

        // Gibt die gesamte Spielhistorie als DTO zurück.
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

        // Gibt den aktuellen Brettzustand als DTO zurück.
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

        #endregion

        #region Timer Control

        // Startet den Timer für den Spieler, der am Zug ist.
        public void StartGameTimer() { _timerServiceInternal.StartPlayerTimer(GetCurrentTurnPlayerLogic(), _state.IsGameOver()); }

        // Wechselt den Timer zum nächsten Spieler.
        public void SwitchPlayerTimer() { _timerServiceInternal.StartPlayerTimer(GetCurrentTurnPlayerLogic(), _state.IsGameOver()); }

        // Benachrichtigt den Timer-Dienst, dass das Spiel beendet ist.
        public void NotifyTimerGameOver() { _timerServiceInternal.SetGameOver(); }

        #endregion

        #region Notifications & Event Handlers

        // Behandelt das "TimeUpdated"-Event vom Timer-Service und leitet die Info an die Clients weiter.
        private void HandleTimeUpdated(TimeUpdateDto timeUpdateDto)
        {
            _logger.LogSessionSendTimeUpdate(_gameIdInternal, timeUpdateDto.WhiteTime, timeUpdateDto.BlackTime, timeUpdateDto.PlayerWhoseTurnItIs);
            _hubContext.Clients.Group(_gameIdInternal.ToString()).SendAsync("OnTimeUpdate", timeUpdateDto);
        }

        // Behandelt das "TimeExpired"-Event vom Timer-Service, beendet das Spiel und benachrichtigt die Clients.
        private void HandleTimeExpired(Player expiredPlayer)
        {
            BoardDto currentBoardDtoForSignalR;
            Player winnerForSignalR;
            TimeUpdateDto finalTimeUpdateForSignalR;

            lock (_sessionLock)
            {
                _logger.LogGameEndedByTimeoutInSession(_gameIdInternal, expiredPlayer);
                if (!_state.IsGameOver())
                {
                    _state.SetResult(Result.Win(expiredPlayer.Opponent(), EndReason.TimeOut));
                    UpdateHistoryOnGameOver();
                    currentBoardDtoForSignalR = ToBoardDto();
                    winnerForSignalR = expiredPlayer.Opponent();
                    finalTimeUpdateForSignalR = _timerServiceInternal.GetCurrentTimeUpdateDto();
                }
                else
                {
                    return;
                }
            }

            Task.Run(async () =>
            {
                await _hubContext.Clients.Group(_gameIdInternal.ToString())
                    .SendAsync("OnTurnChanged", currentBoardDtoForSignalR, winnerForSignalR, GameStatusDto.TimeOut, null, null, null);
                await _hubContext.Clients.Group(_gameIdInternal.ToString())
                    .SendAsync("OnTimeUpdate", finalTimeUpdateForSignalR);
            });
        }

        // Sendet eine "OnTurnChanged"-Benachrichtigung an alle Clients in der Spielgruppe.
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

        // Sendet eine Benachrichtigung über eine neu gezogene Karte an einen spezifischen Client.
        private async Task SignalCardDrawnToPlayer(Guid playerIdToSignal, CardDto drawnCard, string sourceAction)
        {
            string? targetConnectionId = null;
            if (ChessHub.PlayerIdToConnectionMap.TryGetValue(playerIdToSignal, out string? connId))
            {
                targetConnectionId = connId;
            }

            if (!string.IsNullOrEmpty(targetConnectionId))
            {
                int drawPileCount = GetDrawPileCount(playerIdToSignal);
                await _hubContext.Clients.Client(targetConnectionId).SendAsync("CardAddedToHand", drawnCard, drawPileCount);

                if (sourceAction == "Move") _logger.LogControllerMoveSentCardToHand(drawnCard.Name, targetConnectionId, playerIdToSignal);
                else if (sourceAction == "ActivateCard") _logger.LogControllerActivateCardSentCardToHand(drawnCard.Name, targetConnectionId, playerIdToSignal);
                else if (sourceAction == "ComputerMove") _logger.LogControllerMoveSentCardToHand($"[CompMove] {drawnCard.Name}", targetConnectionId, playerIdToSignal);
            }
            else if (drawnCard.Name.Contains(CardConstants.NoMoreCardsName))
            {
                if (sourceAction == "Move") _logger.LogControllerConnectionIdNotFoundNoMoreCards("Move", playerIdToSignal);
                else if (sourceAction == "ActivateCard") _logger.LogControllerConnectionIdNotFoundNoMoreCards("ActivateCard", playerIdToSignal);
                else if (sourceAction == "ComputerMove") _logger.LogControllerConnectionIdNotFoundNoMoreCards("ComputerMove", playerIdToSignal);
            }
            else
            {
                await _hubContext.Clients.Group(_gameIdInternal.ToString()).SendAsync("OnPlayerEarnedCardDraw", playerIdToSignal);
                if (sourceAction == "Move") _logger.LogControllerConnectionIdNotFoundGeneric("Move", playerIdToSignal);
                else if (sourceAction == "ActivateCard") _logger.LogControllerConnectionIdNotFoundGeneric("ActivateCard", playerIdToSignal);
                else if (sourceAction == "ComputerMove") _logger.LogControllerConnectionIdNotFoundGeneric("ComputerMove", playerIdToSignal);
            }
        }

        #endregion

        #region Private Helpers & Utilities

        // Aktualisiert die Spielhistorie, wenn das Spiel endet.
        private void UpdateHistoryOnGameOver()
        {
            _gameHistory.Winner = _state.Result?.Winner;
            _gameHistory.ReasonForGameEnd = _state.Result?.Reason;
            if (!_gameHistory.DateTimeEndedUtc.HasValue)
            {
                _gameHistory.DateTimeEndedUtc = DateTime.UtcNow;
            }
        }

        // Gibt den Spieler zurück, der aktuell am Zug ist.
        public Player GetCurrentTurnPlayerLogic()
        {
            lock (_sessionLock)
            {
                return _state.CurrentPlayer;
            }
        }

        // Konvertiert eine algebraische Notation in ein Positionsobjekt.
        public static Position ParsePos(string alg)
        {
            if (string.IsNullOrWhiteSpace(alg) || alg.Length != 2) throw new ArgumentException("Ungültiges algebraisches Format für Position.", nameof(alg));
            int col = alg[0] - 'a';
            if (!int.TryParse(alg[1].ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int rankValue)) throw new ArgumentException("Ungültiger Rang in algebraischer Notation.", nameof(alg));
            int row = 8 - rankValue;
            if (col < 0 || col > 7 || row < 0 || row > 7) throw new ArgumentException("Position ausserhalb des Schachbretts.", nameof(alg));
            return new Position(row, col);
        }

        // Hilfsmethode, um den Spielstatus für einen Spieler zu bestimmen.
        private GameStatusDto GetStatusForPlayer(Player color)
        {
            lock (_sessionLock)
            {
                var result = _state.Result;
                if (_state.IsGameOver())
                {
                    if (result == null) return GameStatusDto.None;
                    if (result.Winner == Player.None) return MapEndReasonToGameStatusDto(result.Reason);
                    return result.Winner == color ? GameStatusDto.None : MapEndReasonToGameStatusDto(result.Reason, true);
                }
                return _state.Board.IsInCheck(color) ? GameStatusDto.Check : GameStatusDto.None;
            }
        }

        // Konvertiert einen Endgrund in ein GameStatusDto.
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

        // Prüft, ob nach einem Zug eine Bauernumwandlung ansteht.
        private Position? CheckForPawnPromotion(Player player)
        {
            Board currentBoard = _state.Board;
            int promotionRank = (player == Player.White) ? 0 : 7;
            for (int col = 0; col < 8; col++)
            {
                Position pos = new Position(promotionRank, col);
                Piece? piece = currentBoard[pos];
                if (piece != null && piece.Type == PieceType.Pawn && piece.Color == player) { return pos; }
            }
            return null;
        }

        // Prüft, ob ein Spieler am Zug ist.
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

        // Konvertiert das Zeichen einer Bauernumwandlung in den entsprechenden Figurentyp.
        private static PieceType? ParsePromotionChar(char promoChar)
        {
            return promoChar switch { 'q' => PieceType.Queen, 'r' => PieceType.Rook, 'b' => PieceType.Bishop, 'n' => PieceType.Knight, _ => null };
        }

        // Gibt den Namen eines Spielers anhand seiner ID zurück, ohne einen Fehler auszulösen, wenn er nicht gefunden wird.
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

        #endregion

        #region IDisposable

        // Gibt die von der Sitzung verwendeten Ressourcen frei.
        public void Dispose()
        {
            _timerServiceInternal.Dispose();
            _activateCardSemaphore.Dispose();
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}