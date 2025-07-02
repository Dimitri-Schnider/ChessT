using Chess.Logging;
using ChessLogic;
using ChessLogic.Moves;
using ChessLogic.Utilities;
using ChessNetwork.Configuration;
using ChessNetwork.DTOs;
using ChessServer.Hubs;
using ChessServer.Services.Cards;
using ChessServer.Services.Cards.CardEffects;
using ChessServer.Services.ComputerPlayer;
using ChessServer.Services.Connectivity;
using ChessServer.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace ChessServer.Services.Session
{
    // Repräsentiert eine einzelne, laufende Schachpartie.
    // Diese Klasse ist das Herzstück der Spiellogik auf dem Server. Sie verwaltet den Spielzustand (`GameState`),
    // die Spieler, den Timer, die Karten und die Kommunikation mit den Clients über den SignalR Hub.
    public class GameSession : IDisposable
    {
        #region Felder
        private readonly Guid _gameIdInternal;                                  // Die eindeutige ID dieser Spielsitzung.
        private readonly GameState _state;                                      // Das Kernobjekt, das die Schachlogik und den Brettzustand verwaltet.
        private readonly GameTimerService _timerServiceInternal;                // Der Dienst, der die Bedenkzeiten der Spieler verwaltet.
        private readonly IHubContext<ChessHub> _hubContext;                     // Der SignalR-Hub-Kontext für die Echtzeitkommunikation.
        private readonly IChessLogger _logger;                                  // Dienst für das Logging.
        private readonly IComputerMoveProvider _computerMoveProvider;           // Dienst, der die Züge des Computergegners generiert.
        private readonly object _sessionLock = new();                           // Sperrobjekt, um Thread-Sicherheit bei Zugriffen auf den Spielzustand zu gewährleisten.
        private readonly IMoveExecutionService _moveExecutionService;           // Dienst, der die Logik für die Ausführung von Zügen kapselt.
        private Move? lastMoveForHistory;                                       // Speichert den letzten Zug für die FEN-Generierung in der Historie.

        // Manager-Komponenten, die spezifische Aspekte der Sitzung verwalten.
        private readonly IPlayerManager _playerManager;                         // Verwaltet die Spieler (Namen, Farben, IDs).
        private readonly HistoryManager _historyManager;                        // Verwaltet den detaillierten Spielverlauf.
        private readonly IConnectionMappingService _connectionMappingService;   // Verwaltet die Verbindungen der Spieler zu dieser Spielsitzung.
        internal virtual ICardManager CardManager { get; }                      // Verwaltet das gesamte Kartensystem. (Virtuelle Eigenschaft für Tests.)
        #endregion

        #region Öffentliche Eigenschaften
        public virtual Guid GameId => _gameIdInternal;
        internal virtual GameState CurrentGameState => _state;
        internal virtual GameTimerService TimerService => _timerServiceInternal;
        public bool HasOpponent => _playerManager.HasOpponent;
        public Guid FirstPlayerId => _playerManager.FirstPlayerId;
        public Player FirstPlayerColor => _playerManager.FirstPlayerColor;
        #endregion

        // Konstruktor: Initialisiert eine neue Spielsitzung mit allen notwendigen Diensten und Konfigurationen.
        public GameSession(Guid gameId, IPlayerManager playerManager, int initialMinutes,
                           IHubContext<ChessHub> hubContext, IChessLogger logger, ILoggerFactory loggerFactory,
                           IComputerMoveProvider computerMoveProvider,
                           IConnectionMappingService connectionMappingService,
                           IMoveExecutionService moveExecutionService)
        {
            _gameIdInternal = gameId;
            _playerManager = playerManager;
            _state = new GameState(Player.White, Board.Initial());
            _hubContext = hubContext;
            _logger = logger;
            _computerMoveProvider = computerMoveProvider;
            _connectionMappingService = connectionMappingService;
            _moveExecutionService = moveExecutionService;
            _historyManager = new HistoryManager(gameId, initialMinutes);
            // Initialisiert die spezialisierten Manager mit Referenzen auf diese Session.
            CardManager = new CardManager(this, _sessionLock, _historyManager, new ChessLogger<CardManager>(loggerFactory.CreateLogger<CardManager>()), loggerFactory);
            _timerServiceInternal = new GameTimerService(gameId, TimeSpan.FromMinutes(initialMinutes), new ChessLogger<GameTimerService>(loggerFactory.CreateLogger<GameTimerService>()));
            // Abonniert die Timer-Events, um auf Zeit-Updates und Zeitüberschreitungen zu reagieren.
            _timerServiceInternal.OnTimeUpdated += HandleTimeUpdated;
            _timerServiceInternal.OnTimeExpired += HandleTimeExpired;
        }

        #region Öffentliche Methoden
        // Ermöglicht einem Spieler, dieser Sitzung beizutreten.
        public (Guid PlayerId, Player Color) Join(string playerName, Player? preferredColor = null)
        {
            var (playerId, assignedColor) = _playerManager.Join(playerName, preferredColor);
            int initialTimeMinutes = _historyManager.GetGameHistory(_playerManager).InitialTimeMinutes;
            CardManager.InitializeDecksForPlayer(playerId, initialTimeMinutes);
            // Bei Spielen gegen den Computer wird auch dessen Deck initialisiert.
            if (_playerManager.ComputerPlayerId.HasValue && CardManager.GetPlayerHand(_playerManager.ComputerPlayerId.Value).Count == 0)
            {
                CardManager.InitializeDecksForPlayer(_playerManager.ComputerPlayerId.Value, initialTimeMinutes);
            }
            return (playerId, assignedColor);
        }

        // Hilfsmethode, damit der Service den letzten Zug setzen kann
        public void SetLastMoveForHistory(Move move)
        {
            lastMoveForHistory = move;
        }

        // Dieser parameterlose Konstruktor wird für Moq in den Tests benötigt.
        public GameSession()
        {
            // Den Feldern einen "null-forgiving" Wert zuweisen, um den Compiler zufrieden zu stellen.
            // Durch das Mocking-Framework (Moq) werden diese Felder später initialisiert.
            _gameIdInternal = Guid.Empty;
            _state = null!;
            _timerServiceInternal = null!;
            _hubContext = null!;
            _logger = null!;
            _computerMoveProvider = null!;
            _moveExecutionService = null!;
            _playerManager = null!;
            _historyManager = null!;
            _connectionMappingService = null!;
            CardManager = null!;
        }
        public bool IsGameReallyOver() { lock (_sessionLock) { return _state.IsGameOver(); } }
        public virtual Player GetPlayerColor(Guid playerId) => _playerManager.GetPlayerColor(playerId);
        public virtual Guid? GetPlayerIdByColor(Player color) => _playerManager.GetPlayerIdByColor(color);
        public string? GetPlayerName(Guid playerId) => _playerManager.GetPlayerName(playerId);
        public OpponentInfoDto? GetOpponentDetails(Guid currentPlayerId) => _playerManager.GetOpponentDetails(currentPlayerId);
        public GameHistoryDto GetGameHistory() => _historyManager.GetGameHistory(_playerManager);
        public IEnumerable<string> GetLegalMoves(string fromAlg) => _state.LegalMovesForPiece(PositionParser.ParsePos(fromAlg)).Select(m => PieceHelper.ToAlgebraic(m.ToPos));
        public GameStatusDto GetStatus(Guid playerId) => GetStatusForPlayer(GetPlayerColor(playerId));
        public GameStatusDto GetStatusForOpponentOf(Guid lastPlayerId) => GetStatusForPlayer(GetPlayerColor(lastPlayerId).Opponent());
        public Task<ServerCardActivationResultDto> ActivateCard(Guid playerId, ActivateCardRequestDto dto) => CardManager.ActivateCard(playerId, dto);

        // Startet das Spiel und den Timer. Löst bei Bedarf den ersten Zug des Computers aus.
        public void StartTheGameAndTimer()
        {
            lock (_sessionLock)
            {
                if (_playerManager.OpponentType == OpponentType.Computer &&
                    _playerManager.ComputerPlayerId.HasValue &&
                    _playerManager.GetPlayerColor(_playerManager.ComputerPlayerId.Value) == Player.White &&
                    _state.CurrentPlayer == Player.White)
                {
                    var computerColor = _playerManager.GetPlayerColor(_playerManager.ComputerPlayerId.Value);
                    _logger.LogComputerStartingInitialMove(GameId, computerColor, _state.CurrentPlayer);
                    // Der Computerzug wird in einem Hintergrundthread ausgeführt, um die Hauptanwendung nicht zu blockieren.
                    Task.Run(() => ProcessComputerTurnIfNeeded());
                }
            }
        }

        // Verarbeitet einen vom Client gesendeten Zug.
        public MoveResultDto MakeMove(MoveDto dto, Guid playerIdCalling)
        {
            MoveResultDto moveResult;
            lock (_sessionLock)
            {
                var context = new MoveExecutionContext(
                    this,
                    _state,
                    _playerManager,
                    CardManager,
                    _historyManager,
                    _timerServiceInternal,
                    dto,
                    playerIdCalling
                );

                moveResult = _moveExecutionService.ExecuteMove(context);
            }

            // Die Orchestrierung der "Nach-dem-Zug"-Aktionen bleibt hier
            if (moveResult.IsValid)
            {
                Player nextPlayerTurn = GetCurrentTurnPlayerLogic();
                Guid? nextPlayerId = GetPlayerIdByColor(nextPlayerTurn);
                var statusForNextPlayer = nextPlayerId.HasValue ? GetStatus(nextPlayerId.Value) : GameStatusDto.None;

                Task.Run(async () =>
                {
                    await SendOnTurnChangedNotification(moveResult.NewBoard, nextPlayerTurn, statusForNextPlayer, moveResult.LastMoveFrom, moveResult.LastMoveTo, moveResult.CardEffectSquares);
                    await _hubContext.Clients.Group(GameId.ToString()).SendAsync("OnTimeUpdate", TimerService.GetCurrentTimeUpdateDto());
                    if (moveResult.PlayerIdToSignalCardDraw.HasValue && moveResult.NewlyDrawnCard != null)
                    {
                        await SignalCardDrawnToPlayer(moveResult.PlayerIdToSignalCardDraw.Value, moveResult.NewlyDrawnCard, "PlayerMove");
                    }
                });

                bool isExtraTurn = moveResult.IsYourTurn;
                if (_playerManager.OpponentType == OpponentType.Computer &&
                    _playerManager.ComputerPlayerId != playerIdCalling && !isExtraTurn &&
                    !_state.IsGameOver())
                {
                    Task.Run(() => ProcessComputerTurnIfNeeded());
                }
            }

            return moveResult;
        }

        // Verarbeitet das Ergebnis einer Kartenaktivierung und koordiniert die Folgeaktionen.
        public async Task<ServerCardActivationResultDto> HandleCardActivationResult(CardActivationResult effectResult, Guid playerId, CardDto playedCard, string cardTypeId, bool timerWasPaused)
        {
            var activatingPlayerColor = GetPlayerColor(playerId);
            var activatingPlayerName = GetPlayerName(playerId) ?? "Unbekannt";

            if (!effectResult.Success)
            {
                _logger.LogSessionCardActivationFailed(GameId, playerId, cardTypeId, effectResult.ErrorMessage ?? "Kartenaktivierung durch Effektimplementierung fehlgeschlagen.");
                if (timerWasPaused) lock (_sessionLock) TimerService.ResumeTimer();

                // Sonderfall: Bestimmte fehlgeschlagene Effekte (z.B. Wiedergeburt auf besetztes Feld) verbrauchen die Karte trotzdem.
                bool consumeCardOnFailure = cardTypeId == CardConstants.Wiedergeburt && (effectResult.ErrorMessage?.Contains("besetzt") == true || effectResult.ErrorMessage?.Contains("Ursprungsfeld") == true || effectResult.ErrorMessage?.Contains("Keine wiederbelebungsfähigen") == true);
                if (consumeCardOnFailure)
                {
                    CardManager.RemoveCardFromPlayerHand(playerId, playedCard.InstanceId);
                    CardManager.MarkCardAsUsedGlobal(playerId, cardTypeId);
                    _historyManager.AddPlayedCard(new PlayedCardDto { PlayerId = playerId, PlayerName = activatingPlayerName, PlayerColor = activatingPlayerColor, CardId = cardTypeId, CardName = playedCard.Name, TimestampUtc = DateTime.UtcNow }, true);
                    await _hubContext.Clients.Group(GameId.ToString()).SendAsync("OnCardPlayed", playerId, playedCard);
                    return new ServerCardActivationResultDto { Success = true, ErrorMessage = effectResult.ErrorMessage, CardId = cardTypeId, EndsPlayerTurn = true, BoardUpdatedByCardEffect = false };
                }

                return new ServerCardActivationResultDto
                {
                    Success = false,
                    ErrorMessage = effectResult.ErrorMessage ?? "Fehler",
                    CardId = cardTypeId
                };
            }

            // Bei erfolgreicher Aktivierung
            TimeSpan timeTakenForCard = TimerService.StopAndCalculateElapsedTime();
            CardManager.RemoveCardFromPlayerHand(playerId, playedCard.InstanceId);
            CardManager.MarkCardAsUsedGlobal(playerId, cardTypeId);

            // Sendet spezielle Animationsdetails für den Kartentausch.
            if (cardTypeId == CardConstants.CardSwap && effectResult.CardGivenByPlayerForSwapEffect != null && effectResult.CardReceivedByPlayerForSwapEffect != null)
            {
                await SignalCardSwapAnimationDetails(playerId, effectResult.CardGivenByPlayerForSwapEffect, effectResult.CardReceivedByPlayerForSwapEffect);
            }

            // Löst die generische Kartenaktivierungs-Animation auf den Clients aus.
            await _hubContext.Clients.Group(GameId.ToString()).SendAsync("PlayCardActivationAnimation", playedCard, playerId, activatingPlayerColor);
            await _hubContext.Clients.Group(GameId.ToString()).SendAsync("OnCardPlayed", playerId, playedCard);

            // Wartet eine definierte Zeit, um die Animation auf dem Client abzuspielen.
            int delayMs = playedCard.AnimationDelayMs;
            if (delayMs > 0)
            {
                if (cardTypeId == CardConstants.CardSwap) { }
                else
                    _logger.LogComputerTurnDelayAfterCard(GameId, cardTypeId, delayMs / 1000.0);
                await Task.Delay(delayMs);
            }

            // Sendet die aktualisierten Handkarten nach einem Tausch.
            if (cardTypeId == CardConstants.CardSwap)
            {
                await SignalHandUpdatesAfterCardSwap(playerId, GetOpponentDetails(playerId));
            }

            _historyManager.AddPlayedCard(new PlayedCardDto { PlayerId = playerId, PlayerName = activatingPlayerName, PlayerColor = activatingPlayerColor, CardId = cardTypeId, CardName = playedCard.Name, TimestampUtc = DateTime.UtcNow }, effectResult.BoardUpdatedByCardEffect);

            // Fügt einen "abstrakten" Zug zur Historie hinzu, wenn eine Karte gespielt wurde, die das Brett nicht verändert (z.B. Zeitkarte).
            if (effectResult.Success && !effectResult.BoardUpdatedByCardEffect && effectResult.EndsPlayerTurn)
            {
                _historyManager.AddMove(new PlayedMoveDto
                {
                    PlayerId = playerId,
                    PlayerColor = activatingPlayerColor,
                    From = "card",
                    To = "play",
                    ActualMoveType = MoveType.AbstractMove,
                    PromotionPiece = null,
                    TimestampUtc = DateTime.UtcNow,
                    TimeTaken = timeTakenForCard,
                    RemainingTimeWhite = TimerService.GetCurrentTimeForPlayer(Player.White),
                    RemainingTimeBlack = TimerService.GetCurrentTimeForPlayer(Player.Black),
                    PieceMoved = $"Card: {playedCard.Name}",
                    CapturedPiece = null
                });
            }

            Position? promotionSquareAfterCard = null;
            if (effectResult.BoardUpdatedByCardEffect)
            {
                _state.CheckForGameOver();
                if (_state.IsGameOver())
                {
                    _historyManager.UpdateOnGameOver(_state.Result!);
                    NotifyTimerGameOver();
                }
                else
                {
                    // Prüft, ob der Karteneffekt zu einer Bauernumwandlung geführt hat.
                    promotionSquareAfterCard = CheckForPawnPromotion(activatingPlayerColor);
                    if (promotionSquareAfterCard != null)
                    {
                        effectResult = effectResult with { EndsPlayerTurn = false };
                        _logger.LogPawnPromotionPendingAfterCard(GameId, activatingPlayerColor, PieceHelper.ToAlgebraic(promotionSquareAfterCard), cardTypeId);
                    }
                }
            }

            // Prüft, ob der Spieler nach der Kartenaktion immer noch im Schach steht.
            if (CurrentGameState.Board.IsInCheck(activatingPlayerColor) && !_state.IsGameOver())
            {
                _logger.LogPlayerStillInCheckAfterCardTurnNotEnded(GameId, playerId, cardTypeId);
            }

            // Aktualisiert den Spielzustand basierend auf dem Ergebnis des Karteneffekts.
            lock (_sessionLock)
            {
                if (effectResult.EndsPlayerTurn) _state.UpdateStateAfterMove(false, false, null);
                else if (effectResult.BoardUpdatedByCardEffect) _state.RecordCurrentStateForRepetition(null);

                if (!_state.IsGameOver())
                {
                    _state.CheckForGameOver();
                }

                if (_state.IsGameOver())
                {
                    if (!_historyManager.GetGameHistory(_playerManager).DateTimeEndedUtc.HasValue)
                    {
                        _historyManager.UpdateOnGameOver(_state.Result!);
                    }
                    NotifyTimerGameOver();
                }
                else
                {
                    if (effectResult.EndsPlayerTurn) SwitchPlayerTimer();
                    else if (timerWasPaused) TimerService.ResumeTimer();
                    else if (!TimerService.IsPaused) TimerService.StartPlayerTimer(activatingPlayerColor, false);
                }
            }

            // Signalisiert einem Spieler, eine neue Karte zu ziehen, falls erforderlich.
            CardDto? newlyDrawnCard = null;
            if (effectResult.PlayerIdToSignalDraw.HasValue)
            {
                newlyDrawnCard = CardManager.DrawCardForPlayer(effectResult.PlayerIdToSignalDraw.Value);
                if (newlyDrawnCard != null) await SignalCardDrawnToPlayer(effectResult.PlayerIdToSignalDraw.Value, newlyDrawnCard, "ActivateCard");
            }

            // Sendet die finale Zustandsänderung an alle Clients.
            await SendOnTurnChangedNotification(ToBoardDto(), _state.CurrentPlayer, GetStatusForPlayer(_state.CurrentPlayer), null, null, effectResult.AffectedSquaresByCard);
            // Löst bei Bedarf den Zug des Computers aus.
            if (effectResult.EndsPlayerTurn && _playerManager.OpponentType == OpponentType.Computer && !_state.IsGameOver())
            {
                await Task.Run(() => ProcessComputerTurnIfNeeded(null, Random.Shared.Next(1000, 2001)));
            }

            // Gibt das Endergebnis der Kartenaktivierung an den Controller zurück.
            return new ServerCardActivationResultDto
            {
                Success = true,
                CardId = cardTypeId,
                AffectedSquaresByCard = effectResult.AffectedSquaresByCard,
                EndsPlayerTurn = effectResult.EndsPlayerTurn,
                BoardUpdatedByCardEffect = effectResult.BoardUpdatedByCardEffect,
                CardGivenByPlayerForSwap = effectResult.CardGivenByPlayerForSwapEffect,
                CardReceivedByPlayerForSwap = effectResult.CardReceivedByPlayerForSwapEffect,
                PawnPromotionPendingAt = promotionSquareAfterCard != null ? new PositionDto(promotionSquareAfterCard.Row, promotionSquareAfterCard.Column) : null
            };
        }

        // Pausiert den Timer für eine Aktion, die Zeit benötigt (z.B. Kartenaktivierung).
        public bool PauseTimerForAction()
        {
            lock (_sessionLock)
            {
                if (!_state.IsGameOver() && !TimerService.IsPaused)
                {
                    TimerService.PauseTimer();
                    return true;
                }
                return false;
            }
        }

        // Konvertiert den aktuellen Brettzustand in ein DTO für die Netzwerkübertragung.
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

        // Gibt den Spieler zurück, der laut Spiellogik am Zug ist.
        public Player GetCurrentTurnPlayerLogic()
        {
            lock (_sessionLock)
            {
                return _state.CurrentPlayer;
            }
        }
        #endregion

        #region Private Hilfsmethoden
        // Ermittelt den Spielstatus aus der Perspektive eines bestimmten Spielers.
        private GameStatusDto GetStatusForPlayer(Player color)
        {
            lock (_sessionLock)
            {
                if (_state.IsGameOver())
                {
                    var result = _state.Result!;
                    if (result.Winner == Player.None) return MapEndReasonToGameStatusDto(result.Reason);
                    return result.Winner == color ? GameStatusDto.None : MapEndReasonToGameStatusDto(result.Reason, true);
                }
                return _state.Board.IsInCheck(color) ? GameStatusDto.Check : GameStatusDto.None;
            }
        }

        // Mappt den logischen Endgrund auf das DTO für den Client.
        private static GameStatusDto MapEndReasonToGameStatusDto(EndReason? reason, bool forLoserOrOpponentOfWinner = false)
        {
            if (reason == null) return GameStatusDto.None;
            return reason switch
            {
                EndReason.Checkmate => forLoserOrOpponentOfWinner ? GameStatusDto.Checkmate : GameStatusDto.None,
                EndReason.TimeOut => forLoserOrOpponentOfWinner ? GameStatusDto.TimeOut : GameStatusDto.None,
                _ => GameStatusDto.Stalemate,
            };
        }

        // Prüft, ob nach einem Zug eine Bauernumwandlung möglich ist.
        private Position? CheckForPawnPromotion(Player player)
        {
            int promotionRank = player == Player.White ? 0 : 7;
            for (int col = 0; col < 8; col++)
            {
                Position pos = new(promotionRank, col);
                Piece? piece = _state.Board[pos];
                if (piece is { Type: PieceType.Pawn, Color: var pColor } && pColor == player) return pos;
            }
            return null;
        }

        // Prüft, ob ein bestimmter Spieler gerade am Zug ist.
        internal bool IsPlayerTurn(Guid playerId)
        {
            try
            {
                return GetPlayerColor(playerId) == _state.CurrentPlayer;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogSessionErrorIsPlayerTurn(_gameIdInternal, ex);
                return false;
            }
        }

        // Timer-Steuerung und Event-Handler
        internal void SwitchPlayerTimer() => TimerService.StartPlayerTimer(_state.CurrentPlayer, _state.IsGameOver());
        internal void StartGameTimer() => TimerService.StartPlayerTimer(_state.CurrentPlayer, _state.IsGameOver());
        internal void NotifyTimerGameOver() => TimerService.SetGameOver();
        private void HandleTimeUpdated(TimeUpdateDto dto) => _hubContext.Clients.Group(GameId.ToString()).SendAsync("OnTimeUpdate", dto);
        private void HandleTimeExpired(Player p)
        {
            lock (_sessionLock)
            {
                if (_state.IsGameOver()) return;
                _state.Timeout(p);
                _historyManager.UpdateOnGameOver(_state.Result!);
                var currentBoardDto = ToBoardDto();
                var winner = p.Opponent();
                var finalTimeUpdate = TimerService.GetCurrentTimeUpdateDto();
                Task.Run(async () =>
                {
                    await SendOnTurnChangedNotification(currentBoardDto, p, GameStatusDto.TimeOut, null, null, null);
                    await _hubContext.Clients.Group(GameId.ToString()).SendAsync("OnTimeUpdate", finalTimeUpdate);
                });
            }
        }

        // SignalR-Benachrichtigungen
        private async Task SendOnTurnChangedNotification(BoardDto board, Player nextPlayer, GameStatusDto status, string? from, string? to, List<AffectedSquareInfo>? effects)
        {
            await _hubContext.Clients.Group(GameId.ToString()).SendAsync("OnTurnChanged", board, nextPlayer, status, from, to, effects);
        }

        private async Task SignalCardDrawnToPlayer(Guid playerId, CardDto? card, string source)
        {
            if (card == null) return;

            if (_connectionMappingService.GetConnectionId(playerId) is string connId)
            {
                await _hubContext.Clients.Client(connId).SendAsync("CardAddedToHand", card, CardManager.GetDrawPileCount(playerId));
            }
        }

        private async Task SignalCardSwapAnimationDetails(Guid activatingPlayerId, CardDto cardGiven, CardDto cardReceived)
        {
            string? playerConnectionId = _connectionMappingService.GetConnectionId(activatingPlayerId);
            if (!string.IsNullOrEmpty(playerConnectionId))
            {
                await _hubContext.Clients.Client(playerConnectionId)
                    .SendAsync("ReceiveCardSwapAnimationDetails", new CardSwapAnimationDetailsDto(activatingPlayerId, cardGiven, cardReceived));
            }

            var opponentInfo = GetOpponentDetails(activatingPlayerId);
            if (opponentInfo != null)
            {
                string? opponentConnectionId = _connectionMappingService.GetConnectionId(opponentInfo.OpponentId);
                if (!string.IsNullOrEmpty(opponentConnectionId))
                {
                    await _hubContext.Clients.Client(opponentConnectionId)
                        .SendAsync("ReceiveCardSwapAnimationDetails", new CardSwapAnimationDetailsDto(opponentInfo.OpponentId, cardReceived, cardGiven));
                }
            }
        }

        private async Task SignalHandUpdatesAfterCardSwap(Guid playerId, OpponentInfoDto? opponentInfo)
        {
            string? playerConnectionId = _connectionMappingService.GetConnectionId(playerId);
            if (!string.IsNullOrEmpty(playerConnectionId))
            {
                var playerHand = CardManager.GetPlayerHand(playerId);
                int playerDrawPile = CardManager.GetDrawPileCount(playerId);
                await _hubContext.Clients.Client(playerConnectionId).SendAsync("UpdateHandContents", new InitialHandDto(playerHand, playerDrawPile));
            }

            if (opponentInfo != null)
            {
                string? opponentConnectionId = _connectionMappingService.GetConnectionId(opponentInfo.OpponentId);
                if (!string.IsNullOrEmpty(opponentConnectionId))
                {
                    var opponentHand = CardManager.GetPlayerHand(opponentInfo.OpponentId);
                    int opponentDrawPile = CardManager.GetDrawPileCount(opponentInfo.OpponentId);
                    await _hubContext.Clients.Client(opponentConnectionId).SendAsync("UpdateHandContents", new InitialHandDto(opponentHand, opponentDrawPile));
                }
            }
        }

        private static PieceType? ParsePromotionChar(char promoChar) => promoChar switch
        {
            'q' => PieceType.Queen,
            'r' => PieceType.Rook,
            'b' => PieceType.Bishop,
            'n' => PieceType.Knight,
            _ => null
        };

        private async Task ProcessComputerTurnIfNeeded(string? cardId = null, int animationDelayMs = -1)
        {
            // Künstliche Verzögerung für Animationen, falls erforderlich.
            if (animationDelayMs == -1)
            {
                var cardDef = cardId != null ? CardManager.GetCardDefinitionForAnimation(cardId) : null;
                animationDelayMs = cardDef?.AnimationDelayMs ?? Random.Shared.Next(1000, 2001);
            }

            if (animationDelayMs > 0)
            {
                Player computerColorToPause;
                lock (_sessionLock) { computerColorToPause = _state.CurrentPlayer; }
                _logger.LogComputerTimerPausedForAnimation(GameId, computerColorToPause);
                TimerService.PauseTimer();
                await Task.Delay(animationDelayMs);
                TimerService.ResumeTimer();
                _logger.LogComputerTimerResumedAfterAnimation(GameId, computerColorToPause);
            }

            Guid? computerId;
            Player computerColor;
            string fen;
            int depth;

            // Sicherstellen, dass der Computer am Zug ist 
            lock (_sessionLock)
            {
                if (_playerManager.OpponentType != OpponentType.Computer ||
                    !_playerManager.ComputerPlayerId.HasValue ||
                    _playerManager.GetPlayerColor(_playerManager.ComputerPlayerId.Value) != _state.CurrentPlayer ||
                    _state.IsGameOver())
                {
                    if (animationDelayMs > 0) _logger.LogComputerSkippingTurnAfterAnimationDelay(GameId, cardId ?? "N/A");
                    return;
                }
                computerId = _playerManager.ComputerPlayerId;
                computerColor = _playerManager.GetPlayerColor(computerId.Value);

                var moveCount = _historyManager.GetMoveCount();
                fen = new StateString(_state.CurrentPlayer, _state.Board, null, _state.NoCaptureOrPawnMoves, moveCount / 2 + 1, true).ToString();

                depth = _playerManager.ComputerDifficulty switch
                {
                    ComputerDifficulty.Easy => 1,
                    ComputerDifficulty.Hard => 20,
                    _ => 10 // Medium als Standard
                };
            }

            // Den besten Zug vom neuen Service holen
            string? apiMoveString = await _computerMoveProvider.GetNextMoveAsync(_gameIdInternal, fen, depth);

            if (string.IsNullOrWhiteSpace(apiMoveString) || apiMoveString.Length < 4)
            {
                _logger.LogComputerMoveError(GameId, "N/A", 0, $"API lieferte keinen gültigen Zug: '{apiMoveString}'.");
                return;
            }

            // Der restliche Teil der Methode (Parsen und Ausführen) 
            string fromAlg = apiMoveString.Substring(0, 2);
            string toAlg = apiMoveString.Substring(2, 2);
            PieceType? promotion = apiMoveString.Length == 5 ? ParsePromotionChar(apiMoveString[4]) : null;
            var moveDto = new MoveDto(fromAlg, toAlg, computerId.Value, promotion);
            _logger.LogComputerMakingMove(GameId, fromAlg, toAlg);

            var moveResult = MakeMove(moveDto, computerId.Value);

            if (moveResult.IsValid)
            {
                Player nextPlayerTurn = GetCurrentTurnPlayerLogic();
                Guid? nextPlayerId = GetPlayerIdByColor(nextPlayerTurn);
                var statusForNextPlayer = nextPlayerId.HasValue ? GetStatus(nextPlayerId.Value) : GameStatusDto.None;

                await SendOnTurnChangedNotification(moveResult.NewBoard, nextPlayerTurn, statusForNextPlayer, moveResult.LastMoveFrom, moveResult.LastMoveTo, moveResult.CardEffectSquares);
                await _hubContext.Clients.Group(GameId.ToString()).SendAsync("OnTimeUpdate", TimerService.GetCurrentTimeUpdateDto());

                if (moveResult.PlayerIdToSignalCardDraw.HasValue && moveResult.NewlyDrawnCard != null)
                {
                    await SignalCardDrawnToPlayer(moveResult.PlayerIdToSignalCardDraw.Value, moveResult.NewlyDrawnCard, "ComputerMove");
                }
            }
            else
            {
                _logger.LogComputerMoveError(GameId, apiMoveString, 0, $"Computer API schlug ungültigen Zug vor: {apiMoveString}. Fehler: {moveResult.ErrorMessage}");
            }
        }

        #endregion

        // Gibt die von der Session verwendeten Ressourcen (Timer, CardManager) frei.
        public void Dispose()
        {
            _timerServiceInternal.Dispose();
            (CardManager as IDisposable)?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}