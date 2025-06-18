using Chess.Logging;
using ChessLogic;
using ChessLogic.Utilities;
using ChessNetwork.Configuration;
using ChessNetwork.DTOs;
using System;
using System.Collections.Generic;

namespace ChessServer.Services.CardEffects
{
    // Implementiert den Karteneffekt, bei dem ein Bauer geopfert wird, um eine neue Karte zu ziehen.
    public class SacrificeEffect : ICardEffect
    {
        private readonly IChessLogger _logger;
        public SacrificeEffect(IChessLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Führt den Opfer-Effekt aus.
        public CardActivationResult Execute(CardExecutionContext context)
        {
            var fromSquareAlg = context.RequestDto.FromSquare;

            if (context.RequestDto.CardTypeId != CardConstants.SacrificeEffect)
            {
                return new CardActivationResult(false, ErrorMessage: $"SacrificeEffect fälschlicherweise für Karte {context.RequestDto.CardTypeId} aufgerufen.");
            }

            if (string.IsNullOrEmpty(fromSquareAlg))
            {
                _logger.LogSacrificeEffectFailedPawnNotFound(context.Session.GameId, context.PlayerId, fromSquareAlg ?? "NULL");
                return new CardActivationResult(false, ErrorMessage: "Kein Bauer für Opfergabe ausgewählt.");
            }

            Position pawnPos;
            try
            {
                pawnPos = PositionParser.ParsePos(fromSquareAlg);
            }
            catch (ArgumentException ex)
            {
                _logger.LogSacrificeEffectFailedPawnNotFound(context.Session.GameId, context.PlayerId, fromSquareAlg);
                return new CardActivationResult(false, ErrorMessage: $"Ungültige Koordinate für Bauernopfer: {ex.Message}");
            }

            Piece? pieceToSacrifice = context.Session.CurrentGameState.Board[pawnPos];

            if (pieceToSacrifice == null)
            {
                _logger.LogSacrificeEffectFailedPawnNotFound(context.Session.GameId, context.PlayerId, fromSquareAlg);
                return new CardActivationResult(false, ErrorMessage: $"Keine Figur auf dem Feld {fromSquareAlg} für Opfergabe gefunden.");
            }

            if (pieceToSacrifice.Type != PieceType.Pawn)
            {
                _logger.LogSacrificeEffectFailedNotAPawn(context.Session.GameId, context.PlayerId, fromSquareAlg, pieceToSacrifice.Type);
                return new CardActivationResult(false, ErrorMessage: $"Die Figur auf {fromSquareAlg} ist kein Bauer und kann nicht geopfert werden.");
            }

            if (pieceToSacrifice.Color != context.PlayerColor)
            {
                _logger.LogSacrificeEffectFailedWrongColor(context.Session.GameId, context.PlayerId, fromSquareAlg, pieceToSacrifice.Color);
                return new CardActivationResult(false, ErrorMessage: $"Der Bauer auf {fromSquareAlg} gehört nicht dir.");
            }

            Board boardCopy = context.Session.CurrentGameState.Board.Copy();
            boardCopy[pawnPos] = null;
            if (boardCopy.IsInCheck(context.PlayerColor))
            {
                _logger.LogSacrificeEffectFailedWouldCauseCheck(context.Session.GameId, context.PlayerId, fromSquareAlg);
                return new CardActivationResult(false, ErrorMessage: "Opfergabe nicht möglich, da dies deinen König ins Schach stellen würde.");
            }

            context.HistoryManager.AddMove(new PlayedMoveDto
            {
                PlayerId = context.PlayerId,
                PlayerColor = context.PlayerColor,
                From = fromSquareAlg,
                To = "off-board",
                ActualMoveType = MoveType.Sacrifice,
                PieceMoved = $"{pieceToSacrifice.Color} {pieceToSacrifice.Type}",
                TimestampUtc = DateTime.UtcNow,
                TimeTaken = TimeSpan.Zero,
                RemainingTimeWhite = context.Session.TimerService.GetCurrentTimeForPlayer(Player.White),
                RemainingTimeBlack = context.Session.TimerService.GetCurrentTimeForPlayer(Player.Black)
            });

            context.Session.CurrentGameState.Board[pawnPos] = null;
            _logger.LogSacrificeEffectExecuted(context.Session.GameId, context.PlayerId, fromSquareAlg);

            return new CardActivationResult(
                Success: true,
                EndsPlayerTurn: true,
                PlayerIdToSignalDraw: context.PlayerId,
                BoardUpdatedByCardEffect: true,
                AffectedSquaresByCard: new List<AffectedSquareInfo> { new AffectedSquareInfo { Square = fromSquareAlg, Type = "card-sacrifice" } }
            );
        }
    }
}