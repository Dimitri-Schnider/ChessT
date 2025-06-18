using Chess.Logging;
using ChessLogic;
using ChessLogic.Moves;
using ChessLogic.Utilities;
using ChessNetwork.Configuration;
using ChessNetwork.DTOs;
using System;
using System.Collections.Generic;

namespace ChessServer.Services.Cards.CardEffects
{
    // Implementiert den Karteneffekt, der eine Figur auf ein anderes leeres Feld teleportiert.
    public class TeleportEffect : ICardEffect
    {
        private readonly IChessLogger _logger;
        public TeleportEffect(IChessLogger logger)
        {
            _logger = logger;
        }

        // Führt den Teleport-Effekt aus.
        public CardActivationResult Execute(CardExecutionContext context)
        {
            var fromSquareAlg = context.RequestDto.FromSquare;
            var toSquareAlg = context.RequestDto.ToSquare;

            if (context.RequestDto.CardTypeId != CardConstants.Teleport)
            {
                return new CardActivationResult(false, ErrorMessage: $"TeleportEffect fälschlicherweise für Karte {context.RequestDto.CardTypeId} aufgerufen.");
            }

            if (string.IsNullOrEmpty(fromSquareAlg) || string.IsNullOrEmpty(toSquareAlg))
            {
                return new CardActivationResult(false, ErrorMessage: "FromSquare oder ToSquare nicht angegeben für Teleport.");
            }

            Position fromPos;
            Position toPos;
            try
            {
                fromPos = PositionParser.ParsePos(fromSquareAlg);
                toPos = PositionParser.ParsePos(toSquareAlg);
            }
            catch (ArgumentException ex)
            {
                return new CardActivationResult(false, ErrorMessage: $"Ungültige Koordinaten für Teleport: {ex.Message}");
            }

            Piece? pieceToMove = context.Session.CurrentGameState.Board[fromPos];
            if (pieceToMove == null || pieceToMove.Color != context.PlayerColor)
            {
                return new CardActivationResult(false, ErrorMessage: "Ungültige Figur auf FromSquare für Teleport ausgewählt.");
            }

            if (!context.Session.CurrentGameState.Board.IsEmpty(toPos))
            {
                return new CardActivationResult(false, ErrorMessage: "ToSquare für Teleport ist nicht leer.");
            }

            var teleportMove = new TeleportMove(fromPos, toPos);
            if (!teleportMove.IsLegal(context.Session.CurrentGameState.Board))
            {
                return new CardActivationResult(false, ErrorMessage: "Teleport würde eigenen König ins Schach stellen oder ist anderweitig ungültig.");
            }

            context.HistoryManager.AddMove(new PlayedMoveDto
            {
                PlayerId = context.PlayerId,
                PlayerColor = context.PlayerColor,
                From = fromSquareAlg,
                To = toSquareAlg,
                ActualMoveType = MoveType.Teleport,
                PieceMoved = $"{pieceToMove.Color} {pieceToMove.Type}",
                TimestampUtc = DateTime.UtcNow,
                TimeTaken = TimeSpan.Zero,
                RemainingTimeWhite = context.Session.TimerService.GetCurrentTimeForPlayer(Player.White),
                RemainingTimeBlack = context.Session.TimerService.GetCurrentTimeForPlayer(Player.Black)
            });

            teleportMove.Execute(context.Session.CurrentGameState.Board);

            _logger.LogTeleportEffectExecuted(fromSquareAlg, toSquareAlg, context.PlayerId, context.Session.GameId);

            var affectedSquares = new List<AffectedSquareInfo>
            {
                new AffectedSquareInfo { Square = fromSquareAlg, Type = "card-teleport-from" },
                new AffectedSquareInfo { Square = toSquareAlg, Type = "card-teleport-to" }
            };
            return new CardActivationResult(true, BoardUpdatedByCardEffect: true, AffectedSquaresByCard: affectedSquares, EndsPlayerTurn: true);
        }
    }
}