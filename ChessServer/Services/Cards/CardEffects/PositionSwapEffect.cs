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
    // Implementiert den Karteneffekt, der die Positionen zweier eigener Figuren tauscht.
    public class PositionSwapEffect : ICardEffect
    {
        private readonly IChessLogger _logger;
        public PositionSwapEffect(IChessLogger logger)
        {
            _logger = logger;
        }

        // Führt den Tauscheffekt aus.
        public CardActivationResult Execute(CardExecutionContext context)
        {
            var fromSquareAlg = context.RequestDto.FromSquare;
            var toSquareAlg = context.RequestDto.ToSquare;

            if (context.RequestDto.CardTypeId != CardConstants.Positionstausch)
            {
                return new CardActivationResult(false, ErrorMessage: $"PositionSwapEffect fälschlicherweise für Karte {context.RequestDto.CardTypeId} aufgerufen.");
            }

            if (string.IsNullOrEmpty(fromSquareAlg) || string.IsNullOrEmpty(toSquareAlg))
            {
                return new CardActivationResult(false, ErrorMessage: "FromSquare oder ToSquare nicht angegeben für Positionstausch.");
            }

            if (fromSquareAlg == toSquareAlg)
            {
                return new CardActivationResult(false, ErrorMessage: "FromSquare und ToSquare dürfen für Positionstausch nicht identisch sein.");
            }

            Position piece1Pos;
            Position piece2Pos;
            try
            {
                piece1Pos = PositionParser.ParsePos(fromSquareAlg);
                piece2Pos = PositionParser.ParsePos(toSquareAlg);
            }
            catch (ArgumentException ex)
            {
                return new CardActivationResult(false, ErrorMessage: $"Ungültige Koordinaten für Positionstausch: {ex.Message}");
            }

            Piece? piece1 = context.Session.CurrentGameState.Board[piece1Pos];
            Piece? piece2 = context.Session.CurrentGameState.Board[piece2Pos];
            if (piece1 == null || piece2 == null)
            {
                return new CardActivationResult(false, ErrorMessage: "Eines oder beide Felder für Positionstausch sind leer.");
            }

            if (piece1.Color != context.PlayerColor || piece2.Color != context.PlayerColor)
            {
                return new CardActivationResult(false, ErrorMessage: "Nicht beide Figuren für Positionstausch gehören dem Spieler.");
            }

            var positionSwapMove = new PositionSwapMove(piece1Pos, piece2Pos);
            if (!positionSwapMove.IsLegal(context.Session.CurrentGameState.Board))
            {
                return new CardActivationResult(false, ErrorMessage: "Positionstausch würde eigenen König ins Schach stellen.");
            }

            positionSwapMove.Execute(context.Session.CurrentGameState.Board);
            context.HistoryManager.AddMove(new PlayedMoveDto
            {
                PlayerId = context.PlayerId,
                PlayerColor = context.PlayerColor,
                From = fromSquareAlg,
                To = toSquareAlg,
                ActualMoveType = MoveType.PositionSwap,
                PieceMoved = $"{piece1.Color} {piece1.Type} swapped with {piece2.Color} {piece2.Type}",
                TimestampUtc = DateTime.UtcNow,
                TimeTaken = TimeSpan.Zero,
                RemainingTimeWhite = context.Session.TimerService.GetCurrentTimeForPlayer(Player.White),
                RemainingTimeBlack = context.Session.TimerService.GetCurrentTimeForPlayer(Player.Black)
            });
            _logger.LogPositionSwapEffectExecuted(fromSquareAlg, toSquareAlg, context.PlayerId, context.Session.GameId);
            var affectedSquares = new List<AffectedSquareInfo>
        {
            new AffectedSquareInfo { Square = fromSquareAlg, Type = "card-swap-1" },
            new AffectedSquareInfo { Square = toSquareAlg, Type = "card-swap-2" }
        };
            return new CardActivationResult(true, BoardUpdatedByCardEffect: true, AffectedSquaresByCard: affectedSquares, EndsPlayerTurn: true);
        }
    }
}