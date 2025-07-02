using Chess.Logging;
using ChessLogic;
using ChessLogic.Moves;
using ChessLogic.Utilities;
using ChessNetwork.Configuration;
using ChessNetwork.DTOs;
using System;
using System.Linq;

namespace ChessServer.Services.Session;

public class MoveExecutionService : IMoveExecutionService
{
    private readonly IChessLogger _logger;

    public MoveExecutionService(IChessLogger logger)
    {
        _logger = logger;
    }

    public MoveResultDto ExecuteMove(MoveExecutionContext context)
    {
        var playerColor = context.PlayerManager.GetPlayerColor(context.PlayerId);

        // Validierung: Ist der Spieler am Zug?
        if (playerColor != context.State.CurrentPlayer)
        {
            return new MoveResultDto { IsValid = false, ErrorMessage = "Nicht dein Zug.", NewBoard = context.Session.ToBoardDto(), IsYourTurn = context.Session.IsPlayerTurn(context.PlayerId), Status = GameStatusDto.None };
        }

        // Prüft, ob ein "Extrazug" durch eine Karte aktiv ist.
        var extraTurn = context.CardManager.PeekPendingCardEffect(context.PlayerId) == CardConstants.ExtraZug;

        var fromPos = PositionParser.ParsePos(context.MoveDto.From);
        var toPos = PositionParser.ParsePos(context.MoveDto.To);
        Move? legalMove = FindLegalMove(context.State, context.MoveDto, fromPos, toPos);

        if (legalMove == null)
        {
            if (context.State.Board.IsInCheck(playerColor))
            {
                if (fromPos == toPos && context.MoveDto.PromotionTo.HasValue)
                {
                    _logger.LogPlayerTriedMoveThatDidNotResolveCheck(context.Session.GameId, context.PlayerId, playerColor, context.MoveDto.From, context.MoveDto.To);
                    return new MoveResultDto { IsValid = false, ErrorMessage = "Umwandlung nicht möglich, da der König danach im Schach stehen würde.", NewBoard = context.Session.ToBoardDto(), IsYourTurn = true, Status = GameStatusDto.Check };
                }
                _logger.LogPlayerInCheckTriedInvalidMove(context.Session.GameId, context.PlayerId, playerColor, context.MoveDto.From, context.MoveDto.To);
            }
            return new MoveResultDto { IsValid = false, ErrorMessage = "Ungültiger Zug.", NewBoard = context.Session.ToBoardDto(), IsYourTurn = context.Session.IsPlayerTurn(context.PlayerId), Status = GameStatusDto.None };
        }

        // Sonderregel für Extrazug: Der erste Zug darf den Gegner nicht ins Schach setzen.
        if (extraTurn)
        {
            Board boardCopy = context.State.Board.Copy();
            legalMove.Execute(boardCopy);
            if (boardCopy.IsInCheck(playerColor.Opponent()))
            {
                _logger.LogExtraTurnFirstMoveCausesCheck(context.Session.GameId, context.PlayerId, context.MoveDto.From, context.MoveDto.To);
                return new MoveResultDto { IsValid = false, ErrorMessage = "Erster Zug der Extrazug-Karte darf den gegnerischen König nicht ins Schach setzen.", NewBoard = context.Session.ToBoardDto(), IsYourTurn = true, Status = context.Session.GetStatus(context.PlayerId) };
            }
        }

        // Da FindLegalMove bereits einen Zug zurückgibt, der `IsLegal` bestanden hat,
        // ist der manuelle Check hier (den wir vorher besprochen haben) nicht mehr nötig.

        if (extraTurn)
        {
            context.CardManager.ClearPendingCardEffect(context.PlayerId);
        }

        // Führt den Zug endgültig aus und finalisiert den Zustand.
        return ExecuteAndFinalizeMove(context, legalMove, playerColor, extraTurn);
    }

    // Hilfsmethode, um den legalen Zug zu finden.
    private static Move? FindLegalMove(GameState state, MoveDto dto, Position from, Position to)
    {
        var candidateMoves = state.LegalMovesForPiece(from);
        if (dto.PromotionTo.HasValue)
            return candidateMoves.OfType<PawnPromotion>().FirstOrDefault(m => m.ToPos == to && m.PromotionTo == dto.PromotionTo.Value);
        return candidateMoves.FirstOrDefault(m => m.ToPos == to && m is not PawnPromotion);
    }

    private MoveResultDto ExecuteAndFinalizeMove(MoveExecutionContext context, Move legalMove, Player playerColor, bool isExtraTurn)
    {
        Piece? pieceBeingMoved = context.State.Board[legalMove.FromPos];
        Piece? pieceAtDestination = context.State.Board[legalMove.ToPos];

        DateTime moveTimestamp = DateTime.UtcNow;
        TimeSpan timeTaken = context.TimerService.StopAndCalculateElapsedTime();

        bool captureOrPawn = legalMove.Execute(context.State.Board);

        if (isExtraTurn)
        {
            context.State.GrantExtraMove(playerColor);
            _logger.LogExtraTurnEffectApplied(context.Session.GameId, context.PlayerId, CardConstants.ExtraZug);
        }
        else
        {
            context.State.UpdateStateAfterMove(captureOrPawn, true, legalMove);
        }

        context.Session.SetLastMoveForHistory(legalMove);
        if (pieceAtDestination != null) context.CardManager.AddCapturedPiece(pieceAtDestination.Color, pieceAtDestination.Type);
        else if (legalMove is EnPassant) context.CardManager.AddCapturedPiece(playerColor.Opponent(), PieceType.Pawn);

        context.CardManager.IncrementPlayerMoveCount(context.PlayerId);
        var (shouldDraw, newlyDrawnCard) = context.CardManager.CheckAndProcessCardDraw(context.PlayerId);

        AddMoveToHistory(context, legalMove, playerColor, pieceBeingMoved, pieceAtDestination, moveTimestamp, timeTaken);

        context.State.CheckForGameOver(); // Spielende-Bedingungen prüfen

        if (context.State.IsGameOver())
        {
            context.HistoryManager.UpdateOnGameOver(context.State.Result!);
            context.Session.NotifyTimerGameOver();
        }
        else
        {
            if (!isExtraTurn) context.Session.SwitchPlayerTimer();
            else context.Session.StartGameTimer();
        }

        return CreateMoveResultDto(context, isExtraTurn, shouldDraw ? context.PlayerId : null, newlyDrawnCard);
    }

    private static void AddMoveToHistory(MoveExecutionContext context, Move legalMove, Player playerColor, Piece? moved, Piece? captured, DateTime timestamp, TimeSpan timeTaken)
    {
        context.HistoryManager.AddMove(new PlayedMoveDto
        {
            PlayerId = context.PlayerId,
            PlayerColor = playerColor,
            From = context.MoveDto.From,
            To = context.MoveDto.To,
            ActualMoveType = legalMove.Type,
            PromotionPiece = legalMove is PawnPromotion promoMove ? promoMove.PromotionTo : null,
            TimestampUtc = timestamp,
            TimeTaken = timeTaken,
            RemainingTimeWhite = context.TimerService.GetCurrentTimeForPlayer(Player.White),
            RemainingTimeBlack = context.TimerService.GetCurrentTimeForPlayer(Player.Black),
            PieceMoved = moved != null ? $"{moved.Color} {moved.Type}" : "Unknown",
            CapturedPiece = legalMove is EnPassant ? $"{playerColor.Opponent()} Pawn (EP)" : captured != null ? $"{captured.Color} {captured.Type}" : null
        });
    }

    private static MoveResultDto CreateMoveResultDto(MoveExecutionContext context, bool isExtraTurn, Guid? drawPlayerId, CardDto? drawnCard)
    {
        return new MoveResultDto
        {
            IsValid = true,
            ErrorMessage = null,
            NewBoard = context.Session.ToBoardDto(),
            IsYourTurn = isExtraTurn,
            Status = context.Session.GetStatus(context.PlayerId),
            PlayerIdToSignalCardDraw = drawPlayerId,
            NewlyDrawnCard = drawnCard,
            LastMoveFrom = context.MoveDto.From,
            LastMoveTo = context.MoveDto.To,
            CardEffectSquares = null
        };
    }
}