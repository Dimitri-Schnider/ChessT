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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ChessServer.Services
{
    public sealed class GameSession : IDisposable
    {
        #region Fields
        private readonly Guid _gameIdInternal;
        private readonly GameState _state;
        private readonly GameTimerService _timerServiceInternal;
        private readonly IHubContext<ChessHub> _hubContext;
        private readonly IChessLogger _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly object _sessionLock = new();
        private Move? lastMoveForHistory;

        private readonly IPlayerManager _playerManager;
        public ICardManager CardManager { get; }
        private readonly HistoryManager _historyManager;
        #endregion

        #region Public Properties
        public Guid GameId => _gameIdInternal;
        public GameState CurrentGameState => _state;
        public GameTimerService TimerService => _timerServiceInternal;
        public bool HasOpponent => _playerManager.HasOpponent;
        public Guid FirstPlayerId => _playerManager.FirstPlayerId;
        public Player FirstPlayerColor => _playerManager.FirstPlayerColor;
        #endregion

        public GameSession(Guid gameId, IPlayerManager playerManager, int initialMinutes,
                           IHubContext<ChessHub> hubContext, IChessLogger logger, ILoggerFactory loggerFactory,
                           IHttpClientFactory httpClientFactory)
        {
            _gameIdInternal = gameId;
            _playerManager = playerManager;
            _state = new GameState(Player.White, Board.Initial());
            _hubContext = hubContext;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _historyManager = new HistoryManager(gameId, initialMinutes);
            CardManager = new CardManager(this, _sessionLock, _historyManager, new ChessLogger<CardManager>(loggerFactory.CreateLogger<CardManager>()), loggerFactory);
            _timerServiceInternal = new GameTimerService(gameId, TimeSpan.FromMinutes(initialMinutes), loggerFactory.CreateLogger<GameTimerService>());
            _timerServiceInternal.OnTimeUpdated += HandleTimeUpdated;
            _timerServiceInternal.OnTimeExpired += HandleTimeExpired;
        }

        #region Public Methods
        public (Guid PlayerId, Player Color) Join(string playerName, Player? preferredColor = null)
        {
            var (playerId, assignedColor) = _playerManager.Join(playerName, preferredColor);
            CardManager.InitializeDecksForPlayer(playerId);
            if (_playerManager.ComputerPlayerId.HasValue && CardManager.GetPlayerHand(_playerManager.ComputerPlayerId.Value).Count == 0)
            {
                CardManager.InitializeDecksForPlayer(_playerManager.ComputerPlayerId.Value);
            }
            return (playerId, assignedColor);
        }

        public bool IsGameReallyOver() { lock (_sessionLock) { return _state.IsGameOver(); } }
        public Player GetPlayerColor(Guid playerId) => _playerManager.GetPlayerColor(playerId);
        public Guid? GetPlayerIdByColor(Player color) => _playerManager.GetPlayerIdByColor(color);
        public string? GetPlayerName(Guid playerId) => _playerManager.GetPlayerName(playerId);
        public OpponentInfoDto? GetOpponentDetails(Guid currentPlayerId) => _playerManager.GetOpponentDetails(currentPlayerId);
        public GameHistoryDto GetGameHistory() => _historyManager.GetGameHistory(_playerManager);
        public IEnumerable<string> GetLegalMoves(string fromAlg) => _state.LegalMovesForPiece(ParsePos(fromAlg)).Select(m => PieceHelper.ToAlgebraic(m.ToPos));
        public GameStatusDto GetStatus(Guid playerId) => GetStatusForPlayer(GetPlayerColor(playerId));
        public GameStatusDto GetStatusForOpponentOf(Guid lastPlayerId) => GetStatusForPlayer(GetPlayerColor(lastPlayerId).Opponent());

        public Task<ServerCardActivationResultDto> ActivateCard(Guid playerId, ActivateCardRequestDto dto) => CardManager.ActivateCard(playerId, dto);

        public void StartTheGameAndTimer()
        {
            TimerService.StartPlayerTimer(_state.CurrentPlayer, _state.IsGameOver());

            lock (_sessionLock)
            {
                if (_playerManager.OpponentType == "Computer" &&
                    _playerManager.ComputerPlayerId.HasValue &&
                    _playerManager.GetPlayerColor(_playerManager.ComputerPlayerId.Value) == Player.White &&
                    _state.CurrentPlayer == Player.White)
                {
                    var computerColor = _playerManager.GetPlayerColor(_playerManager.ComputerPlayerId.Value);
                    _logger.LogComputerStartingInitialMove(GameId, computerColor, _state.CurrentPlayer);
                    Task.Run(() => ProcessComputerTurnIfNeeded());
                }
            }
        }

        public MoveResultDto MakeMove(MoveDto dto, Guid playerIdCalling)
        {
            lock (_sessionLock) // Lock beginnt hier
            {
                var playerColor = GetPlayerColor(playerIdCalling);
                if (playerColor != _state.CurrentPlayer)
                    return new MoveResultDto { IsValid = false, ErrorMessage = "Nicht dein Zug.", NewBoard = ToBoardDto(), IsYourTurn = IsPlayerTurn(playerIdCalling), Status = GameStatusDto.None };
                var fromPos = ParsePos(dto.From);
                var toPos = ParsePos(dto.To);
                Move? legalMove = FindLegalMove(dto, fromPos, toPos);
                if (legalMove == null)
                {
                    if (_state.Board.IsInCheck(playerColor)) _logger.LogPlayerInCheckTriedInvalidMove(GameId, playerIdCalling, playerColor, dto.From, dto.To);
                    return new MoveResultDto { IsValid = false, ErrorMessage = "Ungültiger Zug.", NewBoard = ToBoardDto(), IsYourTurn = IsPlayerTurn(playerIdCalling), Status = GameStatusDto.None };
                }

                var extraTurn = CardManager.GetAndClearPendingCardEffect(playerIdCalling) == CardConstants.ExtraZug;
                if (extraTurn)
                {
                    Board boardCopy = _state.Board.Copy();
                    legalMove.Execute(boardCopy);
                    if (boardCopy.IsInCheck(playerColor.Opponent()))
                    {
                        _logger.LogExtraTurnFirstMoveCausesCheck(GameId, playerIdCalling, dto.From, dto.To);
                        return new MoveResultDto { IsValid = false, ErrorMessage = "Erster Zug der Extrazug-Karte darf den gegnerischen König nicht ins Schach setzen.", NewBoard = ToBoardDto(), IsYourTurn = true, Status = GetStatus(playerIdCalling) };
                    }
                }

                Board boardCopyForCheck = _state.Board.Copy();
                legalMove.Execute(boardCopyForCheck);
                if (boardCopyForCheck.IsInCheck(playerColor))
                {
                    _logger.LogPlayerTriedMoveThatDidNotResolveCheck(GameId, playerIdCalling, playerColor, dto.From, dto.To);
                    return new MoveResultDto { IsValid = false, ErrorMessage = "Dieser Zug ist ungültig, da du danach immer noch im Schach stehst.", NewBoard = ToBoardDto(), IsYourTurn = true, Status = GameStatusDto.Check };
                }

                // Der Rest der Methode bleibt innerhalb des Locks
                return ExecuteAndFinalizeMove(legalMove, dto, playerIdCalling, playerColor, extraTurn);
            } // Lock endet hier
        }


        public async Task<ServerCardActivationResultDto> HandleCardActivationResult(CardActivationResult effectResult, Guid playerId, CardDto playedCard, string cardTypeId, bool timerWasPaused)
        {
            var activatingPlayerColor = GetPlayerColor(playerId);
            var activatingPlayerName = GetPlayerName(playerId) ?? "Unbekannt";

            if (!effectResult.Success)
            {
                _logger.LogSessionCardActivationFailed(GameId, playerId, cardTypeId, effectResult.ErrorMessage ?? "Kartenaktivierung durch Effektimplementierung fehlgeschlagen.");
                if (timerWasPaused) lock (_sessionLock) TimerService.ResumeTimer();

                bool consumeCardOnFailure = cardTypeId == CardConstants.Wiedergeburt && (effectResult.ErrorMessage?.Contains("besetzt") == true || effectResult.ErrorMessage?.Contains("Ursprungsfeld") == true || effectResult.ErrorMessage?.Contains("Keine wiederbelebungsfähigen") == true);
                if (consumeCardOnFailure)
                {
                    CardManager.RemoveCardFromPlayerHand(playerId, playedCard.InstanceId);
                    CardManager.MarkCardAsUsedGlobal(playerId, cardTypeId);
                    _historyManager.AddPlayedCard(new PlayedCardDto { PlayerId = playerId, PlayerName = activatingPlayerName, PlayerColor = activatingPlayerColor, CardId = cardTypeId, CardName = playedCard.Name, TimestampUtc = DateTime.UtcNow }, true);
                    await _hubContext.Clients.Group(GameId.ToString()).SendAsync("OnCardPlayed", playerId, playedCard);
                    return new ServerCardActivationResultDto { Success = true, ErrorMessage = effectResult.ErrorMessage, CardId = cardTypeId, EndsPlayerTurn = true, BoardUpdatedByCardEffect = false };
                }

                return new ServerCardActivationResultDto { Success = false, ErrorMessage = effectResult.ErrorMessage ?? "Fehler", CardId = cardTypeId };
            }

            // Stoppe die Uhr des Spielers, um die Bedenkzeit für die Kartenaktion zu ermitteln
            TimeSpan timeTakenForCard = TimerService.StopAndCalculateElapsedTime();

            CardManager.RemoveCardFromPlayerHand(playerId, playedCard.InstanceId);
            CardManager.MarkCardAsUsedGlobal(playerId, cardTypeId);

            if (cardTypeId == CardConstants.CardSwap && effectResult.CardGivenByPlayerForSwapEffect != null && effectResult.CardReceivedByPlayerForSwapEffect != null)
            {
                await SignalCardSwapAnimationDetails(playerId, effectResult.CardGivenByPlayerForSwapEffect, effectResult.CardReceivedByPlayerForSwapEffect);
            }

            await _hubContext.Clients.Group(GameId.ToString()).SendAsync("PlayCardActivationAnimation", playedCard, playerId, activatingPlayerColor);
            await _hubContext.Clients.Group(GameId.ToString()).SendAsync("OnCardPlayed", playerId, playedCard);

            int delayMs = playedCard.AnimationDelayMs;
            if (delayMs > 0)
            {
                if (cardTypeId == CardConstants.CardSwap) { }
                // TODO  _logger.LogComputerTurnDelayCardSwap(GameId, cardTypeId, delayMs / 1000.0);
                else
                    _logger.LogComputerTurnDelayAfterCard(GameId, cardTypeId, delayMs / 1000.0);
                await Task.Delay(delayMs);
            }

            if (cardTypeId == CardConstants.CardSwap)
            {
                await SignalHandUpdatesAfterCardSwap(playerId, GetOpponentDetails(playerId));
            }

            _historyManager.AddPlayedCard(new PlayedCardDto { PlayerId = playerId, PlayerName = activatingPlayerName, PlayerColor = activatingPlayerColor, CardId = cardTypeId, CardName = playedCard.Name, TimestampUtc = DateTime.UtcNow }, effectResult.BoardUpdatedByCardEffect);

            // Wenn die Karte das Brett NICHT verändert hat UND den Zug des Spielers NICHT beendet, fügen wir einen "AbstractMove" hinzu.
            if (effectResult.Success && !effectResult.BoardUpdatedByCardEffect && effectResult.EndsPlayerTurn)
            {
                _historyManager.AddMove(new PlayedMoveDto
                {
                    PlayerId = playerId,
                    PlayerColor = activatingPlayerColor,
                    From = "card", // Deskriptiver Platzhalter
                    To = "play",   // Deskriptiver Platzhalter
                    ActualMoveType = MoveType.AbstractMove,
                    PromotionPiece = null,
                    TimestampUtc = DateTime.UtcNow,
                    TimeTaken = timeTakenForCard, // KORREKTUR: Die tatsächlich vergangene Zeit verwenden
                    RemainingTimeWhite = TimerService.GetCurrentTimeForPlayer(Player.White),
                    RemainingTimeBlack = TimerService.GetCurrentTimeForPlayer(Player.Black),
                    PieceMoved = $"Card: {playedCard.Name}", // Eindeutige Beschreibung der Aktion
                    CapturedPiece = null
                });
            }

            Position? promotionSquareAfterCard = null;
            if (effectResult.BoardUpdatedByCardEffect)
            {
                promotionSquareAfterCard = CheckForPawnPromotion(activatingPlayerColor);
                if (promotionSquareAfterCard != null)
                {
                    effectResult = effectResult with { EndsPlayerTurn = false };
                    _logger.LogPawnPromotionPendingAfterCard(GameId, activatingPlayerColor, PieceHelper.ToAlgebraic(promotionSquareAfterCard), cardTypeId);
                }
            }

            if (CurrentGameState.Board.IsInCheck(activatingPlayerColor) && !_state.IsGameOver())
            {
                _logger.LogPlayerStillInCheckAfterCardTurnNotEnded(GameId, playerId, cardTypeId);
            }

            lock (_sessionLock)
            {
                if (effectResult.EndsPlayerTurn) _state.UpdateStateAfterMove(false, false, null);
                else if (effectResult.BoardUpdatedByCardEffect) _state.RecordCurrentStateForRepetition(null);

                _state.CheckForGameOver();
                if (_state.IsGameOver())
                {
                    _historyManager.UpdateOnGameOver(_state.Result!); NotifyTimerGameOver();
                }
                else
                {
                    if (effectResult.EndsPlayerTurn) SwitchPlayerTimer();
                    else if (timerWasPaused) TimerService.ResumeTimer();
                    else if (!TimerService.IsPaused) TimerService.StartPlayerTimer(activatingPlayerColor, false); // Timer wieder starten, falls er nicht pausiert war
                }
            }

            CardDto? newlyDrawnCard = null;
            if (effectResult.PlayerIdToSignalDraw.HasValue)
            {
                newlyDrawnCard = CardManager.DrawCardForPlayer(effectResult.PlayerIdToSignalDraw.Value);
                if (newlyDrawnCard != null) await SignalCardDrawnToPlayer(effectResult.PlayerIdToSignalDraw.Value, newlyDrawnCard, "ActivateCard");
            }

            await SendOnTurnChangedNotification(ToBoardDto(), _state.CurrentPlayer, GetStatusForPlayer(_state.CurrentPlayer), null, null, effectResult.AffectedSquaresByCard);
            if (effectResult.EndsPlayerTurn && _playerManager.OpponentType == "Computer" && !_state.IsGameOver())
            {
                await Task.Run(() => ProcessComputerTurnIfNeeded(null, Random.Shared.Next(1000, 2001)));
            }

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

        public static Position ParsePos(string alg)
        {
            if (string.IsNullOrWhiteSpace(alg) || alg.Length != 2) throw new ArgumentException("Ungültiges algebraisches Format für Position.", nameof(alg));
            int col = alg[0] - 'a';
            if (!int.TryParse(alg[1].ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int rankValue)) throw new ArgumentException("Ungültiger Rang in algebraischer Notation.", nameof(alg));
            int row = 8 - rankValue;
            if (col < 0 || col > 7 || row < 0 || row > 7) throw new ArgumentException("Position ausserhalb des Schachbretts.", nameof(alg));
            return new Position(row, col);
        }

        public Player GetCurrentTurnPlayerLogic()
        {
            lock (_sessionLock)
            {
                return _state.CurrentPlayer;
            }
        }
        #endregion

        #region Private Helpers
        private MoveResultDto ExecuteAndFinalizeMove(Move legalMove, MoveDto dto, Guid playerId, Player playerColor, bool isExtraTurn)
        {
            Piece? pieceBeingMoved;
            Piece? pieceAtDestination;
            bool captureOrPawn;
            DateTime moveTimestamp = DateTime.UtcNow;
            TimeSpan timeTaken = TimerService.StopAndCalculateElapsedTime();

            pieceBeingMoved = _state.Board[legalMove.FromPos];
            pieceAtDestination = _state.Board[legalMove.ToPos];
            captureOrPawn = legalMove.Execute(_state.Board);
            if (isExtraTurn)
            {
                _state.SetCurrentPlayerOverride(playerColor);
                _logger.LogExtraTurnEffectApplied(GameId, playerId, CardConstants.ExtraZug);
            }
            else
            {
                _state.UpdateStateAfterMove(captureOrPawn, true, legalMove);
            }
            lastMoveForHistory = legalMove;
            if (pieceAtDestination != null) CardManager.AddCapturedPiece(pieceAtDestination.Color, pieceAtDestination.Type);
            else if (legalMove is EnPassant) CardManager.AddCapturedPiece(playerColor.Opponent(), PieceType.Pawn);

            CardManager.IncrementPlayerMoveCount(playerId);
            var (shouldDraw, newlyDrawnCard) = CardManager.CheckAndProcessCardDraw(playerId);
            var moveResultDto = CreateMoveResultDto(dto, isExtraTurn, shouldDraw ? playerId : null, newlyDrawnCard);
            AddMoveToHistory(dto, legalMove, playerId, playerColor, pieceBeingMoved, pieceAtDestination, moveTimestamp, timeTaken);
            if (_state.IsGameOver())
            {
                _historyManager.UpdateOnGameOver(_state.Result!);
                NotifyTimerGameOver();
            }
            else
            {
                if (!isExtraTurn) SwitchPlayerTimer();
                else StartGameTimer();
            }

            if (_playerManager.OpponentType == "Computer" && !isExtraTurn && !_state.IsGameOver())
            {
                Task.Run(() => ProcessComputerTurnIfNeeded());
            }

            return moveResultDto;
        }

        private Move? FindLegalMove(MoveDto dto, Position from, Position to)
        {
            var candidateMoves = _state.LegalMovesForPiece(from);
            if (dto.PromotionTo.HasValue)
                return candidateMoves.OfType<PawnPromotion>().FirstOrDefault(m => m.ToPos == to && m.PromotionTo == dto.PromotionTo.Value);
            return candidateMoves.FirstOrDefault(m => m.ToPos == to && m is not PawnPromotion);
        }

        private void AddMoveToHistory(MoveDto dto, Move legalMove, Guid playerId, Player playerColor, Piece? moved, Piece? captured, DateTime timestamp, TimeSpan timeTaken)
        {
            _historyManager.AddMove(new PlayedMoveDto
            {
                PlayerId = playerId,
                PlayerColor = playerColor,
                From = dto.From,
                To = dto.To,
                ActualMoveType = legalMove.Type,
                PromotionPiece = (legalMove is PawnPromotion promoMove) ? promoMove.PromotionTo : null,
                TimestampUtc = timestamp,
                TimeTaken = timeTaken,
                RemainingTimeWhite = TimerService.GetCurrentTimeForPlayer(Player.White),
                RemainingTimeBlack = TimerService.GetCurrentTimeForPlayer(Player.Black),
                PieceMoved = moved != null ? $"{moved.Color} {moved.Type}" : "Unknown",
                CapturedPiece = (legalMove is EnPassant) ? $"{playerColor.Opponent()} Pawn (EP)" : captured != null ? $"{captured.Color} {captured.Type}" : null
            });
        }

        private MoveResultDto CreateMoveResultDto(MoveDto dto, bool isExtraTurn, Guid? drawPlayerId, CardDto? drawnCard)
        {
            return new MoveResultDto
            {
                IsValid = true,
                ErrorMessage = null,
                NewBoard = ToBoardDto(),
                IsYourTurn = isExtraTurn,
                Status = GetStatusForPlayer(GetPlayerColor(dto.PlayerId)),
                PlayerIdToSignalCardDraw = drawPlayerId,
                NewlyDrawnCard = drawnCard,
                LastMoveFrom = dto.From,
                LastMoveTo = dto.To,
                CardEffectSquares = null
            };
        }

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

        private Position? CheckForPawnPromotion(Player player)
        {
            int promotionRank = (player == Player.White) ? 0 : 7;
            for (int col = 0; col < 8; col++)
            {
                Position pos = new(promotionRank, col);
                Piece? piece = _state.Board[pos];
                if (piece is { Type: PieceType.Pawn, Color: var pColor } && pColor == player) return pos;
            }
            return null;
        }

        private bool IsPlayerTurn(Guid playerId)
        {
            try
            {
                return GetPlayerColor(playerId) == _state.CurrentPlayer;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogSessionErrorIsPlayerTurn(_gameIdInternal, ex); return false;
            }
        }

        private void SwitchPlayerTimer() => TimerService.StartPlayerTimer(_state.CurrentPlayer, _state.IsGameOver());
        private void StartGameTimer() => TimerService.StartPlayerTimer(_state.CurrentPlayer, _state.IsGameOver());
        private void NotifyTimerGameOver() => TimerService.SetGameOver();
        private void HandleTimeUpdated(TimeUpdateDto dto) => _hubContext.Clients.Group(GameId.ToString()).SendAsync("OnTimeUpdate", dto);
        private void HandleTimeExpired(Player p)
        {
            lock (_sessionLock)
            {
                if (_state.IsGameOver()) return;
                _state.SetResult(Result.Win(p.Opponent(), EndReason.TimeOut));
                _historyManager.UpdateOnGameOver(_state.Result);
                var currentBoardDto = ToBoardDto();
                var winner = p.Opponent();
                var finalTimeUpdate = TimerService.GetCurrentTimeUpdateDto();
                Task.Run(async () =>
                {
                    await SendOnTurnChangedNotification(currentBoardDto, winner, GameStatusDto.TimeOut, null, null, null);
                    await _hubContext.Clients.Group(GameId.ToString()).SendAsync("OnTimeUpdate", finalTimeUpdate);
                });
            }
        }

        private async Task SendOnTurnChangedNotification(BoardDto board, Player nextPlayer, GameStatusDto status, string? from, string? to, List<AffectedSquareInfo>? effects)
        {
            await _hubContext.Clients.Group(GameId.ToString()).SendAsync("OnTurnChanged", board, nextPlayer, status, from, to, effects);
        }

        private async Task SignalCardDrawnToPlayer(Guid playerId, CardDto? card, string source)
        {
            if (card == null) return;
            if (ChessHub.PlayerIdToConnectionMap.TryGetValue(playerId, out string? connId) && connId != null)
            {
                await _hubContext.Clients.Client(connId).SendAsync("CardAddedToHand", card, CardManager.GetDrawPileCount(playerId));
            }
        }

        private async Task SignalCardSwapAnimationDetails(Guid activatingPlayerId, CardDto cardGiven, CardDto cardReceived)
        {
            string? playerConnectionId = ChessHub.PlayerIdToConnectionMap.GetValueOrDefault(activatingPlayerId);
            if (!string.IsNullOrEmpty(playerConnectionId))
            {
                await _hubContext.Clients.Client(playerConnectionId)
                    .SendAsync("ReceiveCardSwapAnimationDetails", new CardSwapAnimationDetailsDto(activatingPlayerId, cardGiven, cardReceived));
            }

            var opponentInfo = GetOpponentDetails(activatingPlayerId);
            if (opponentInfo != null)
            {
                string? opponentConnectionId = ChessHub.PlayerIdToConnectionMap.GetValueOrDefault(opponentInfo.OpponentId);
                if (!string.IsNullOrEmpty(opponentConnectionId))
                {
                    await _hubContext.Clients.Client(opponentConnectionId)
                        .SendAsync("ReceiveCardSwapAnimationDetails", new CardSwapAnimationDetailsDto(opponentInfo.OpponentId, cardReceived, cardGiven));
                }
            }
        }

        private async Task SignalHandUpdatesAfterCardSwap(Guid playerId, OpponentInfoDto? opponentInfo)
        {
            string? playerConnectionId = ChessHub.PlayerIdToConnectionMap.GetValueOrDefault(playerId);
            if (!string.IsNullOrEmpty(playerConnectionId))
            {
                var playerHand = CardManager.GetPlayerHand(playerId);
                int playerDrawPile = CardManager.GetDrawPileCount(playerId);
                await _hubContext.Clients.Client(playerConnectionId).SendAsync("UpdateHandContents", new InitialHandDto(playerHand, playerDrawPile));
            }

            if (opponentInfo != null)
            {
                string? opponentConnectionId = ChessHub.PlayerIdToConnectionMap.GetValueOrDefault(opponentInfo.OpponentId);
                if (!string.IsNullOrEmpty(opponentConnectionId))
                {
                    var opponentHand = CardManager.GetPlayerHand(opponentInfo.OpponentId);
                    int opponentDrawPile = CardManager.GetDrawPileCount(opponentInfo.OpponentId);
                    await _hubContext.Clients.Client(opponentConnectionId).SendAsync("UpdateHandContents", new InitialHandDto(opponentHand, opponentDrawPile));
                }
            }
        }


        private sealed class ChessApiResponseDto { public string? Move { get; set; } }
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
            lock (_sessionLock)
            {
                if (_playerManager.OpponentType != "Computer" ||
                    !_playerManager.ComputerPlayerId.HasValue ||
                    _playerManager.GetPlayerColor(_playerManager.ComputerPlayerId.Value) != _state.CurrentPlayer ||
                    _state.IsGameOver())
                {
                    if (animationDelayMs > 0) _logger.LogComputerSkippingTurnAfterAnimationDelay(GameId, cardId ?? "N/A");
                    return;
                }
                computerId = _playerManager.ComputerPlayerId;
                computerColor = _playerManager.GetPlayerColor(computerId.Value);
            }

            string? apiMoveString = await GetComputerApiMoveAsync();
            if (string.IsNullOrWhiteSpace(apiMoveString) || apiMoveString.Length < 4)
            {
                _logger.LogComputerMoveError(GameId, "N/A", 0, $"API lieferte keinen gültigen Zug: '{apiMoveString}'.");
                return;
            }

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
        private async Task<string?> GetComputerApiMoveAsync()
        {
            string fen;
            int depth;
            lock (_sessionLock)
            {
                if (_playerManager.ComputerPlayerId == null) return null;
                var moveCount = _historyManager.GetMoveCount();
                fen = new StateString(_state.CurrentPlayer, _state.Board, null, _state.NoCaptureOrPawnMoves, (moveCount / 2) + 1, true).ToString();
                depth = _playerManager.GetPlayerName(_playerManager.ComputerPlayerId.Value)?.Contains("Easy") == true ? 1 :
                        _playerManager.GetPlayerName(_playerManager.ComputerPlayerId.Value)?.Contains("Hard") == true ? 20 : 10;
            }

            var client = _httpClientFactory.CreateClient("ChessApi");
            var requestBody = new { fen, depth };

            try
            {
                _logger.LogComputerFetchingMove(GameId, fen, depth);
                HttpResponseMessage response = await client.PostAsJsonAsync("https://chess-api.com/v1", requestBody);

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = await response.Content.ReadFromJsonAsync<ChessApiResponseDto>();
                    if (apiResponse != null && !string.IsNullOrEmpty(apiResponse.Move))
                    {
                        _logger.LogComputerReceivedMove(GameId, apiResponse.Move, fen, depth);
                        return apiResponse.Move;
                    }
                    _logger.LogComputerMoveError(GameId, fen, depth, "API-Antwort erfolgreich, aber kein Zug gefunden.");
                    return null;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogComputerMoveError(GameId, fen, depth, $"API-Anfrage fehlgeschlagen mit Status {response.StatusCode}: {errorContent}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogComputerMoveError(GameId, fen, depth, $"Exception während API-Aufruf: {ex.Message}");
                return null;
            }
        }
        #endregion

        public void Dispose()
        {
            _timerServiceInternal.Dispose();
            (CardManager as IDisposable)?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}