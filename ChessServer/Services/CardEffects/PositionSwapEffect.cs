using Chess.Logging;
using ChessLogic;
using ChessLogic.Moves;
using ChessLogic.Utilities;
using ChessNetwork.Configuration;
using ChessNetwork.DTOs;
using System;
using System.Collections.Generic;

namespace ChessServer.Services.CardEffects
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
        public CardActivationResult Execute(GameSession session, Guid playerId, Player playerDataColor,
                                            IHistoryManager historyManager,
                                            string cardTypeId,
                                            string? fromSquareAlg,
                                            string? toSquareAlg)
        {
            if (cardTypeId != CardConstants.Positionstausch)
            {
                return new CardActivationResult(false, ErrorMessage: $"PositionSwapEffect fälschlicherweise für Karte {cardTypeId} aufgerufen.");
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
                piece1Pos = GameSession.ParsePos(fromSquareAlg);
                piece2Pos = GameSession.ParsePos(toSquareAlg);
            }
            catch (ArgumentException ex)
            {
                return new CardActivationResult(false, ErrorMessage: $"Ungültige Koordinaten für Positionstausch: {ex.Message}");
            }

            Piece? piece1 = session.CurrentGameState.Board[piece1Pos];
            Piece? piece2 = session.CurrentGameState.Board[piece2Pos];
            if (piece1 == null || piece2 == null)
            {
                return new CardActivationResult(false, ErrorMessage: "Eines oder beide Felder für Positionstausch sind leer.");
            }

            if (piece1.Color != playerDataColor || piece2.Color != playerDataColor)
            {
                return new CardActivationResult(false, ErrorMessage: "Nicht beide Figuren für Positionstausch gehören dem Spieler.");
            }

            var positionSwapMove = new PositionSwapMove(piece1Pos, piece2Pos);
            if (!positionSwapMove.IsLegal(session.CurrentGameState.Board))
            {
                return new CardActivationResult(false, ErrorMessage: "Positionstausch würde eigenen König ins Schach stellen.");
            }

            positionSwapMove.Execute(session.CurrentGameState.Board);

            // HINZUGEFÜGT: Protokollierung des Zugs im Spielverlauf
            historyManager.AddMove(new PlayedMoveDto
            {
                PlayerId = playerId,
                PlayerColor = playerDataColor,
                From = fromSquareAlg,
                To = toSquareAlg,
                ActualMoveType = MoveType.PositionSwap,
                PieceMoved = $"{piece1.Color} {piece1.Type} swapped with {piece2.Color} {piece2.Type}",
                TimestampUtc = DateTime.UtcNow,
                TimeTaken = TimeSpan.Zero,
                RemainingTimeWhite = session.TimerService.GetCurrentTimeForPlayer(Player.White),
                RemainingTimeBlack = session.TimerService.GetCurrentTimeForPlayer(Player.Black)
            });

            _logger.LogPositionSwapEffectExecuted(fromSquareAlg, toSquareAlg, playerId, session.GameId);
            var affectedSquares = new List<AffectedSquareInfo>
            {
                new AffectedSquareInfo { Square = fromSquareAlg, Type = "card-swap-1" },
                new AffectedSquareInfo { Square = toSquareAlg, Type = "card-swap-2" }
            };
            return new CardActivationResult(true, BoardUpdatedByCardEffect: true, AffectedSquaresByCard: affectedSquares, EndsPlayerTurn: true);
        }
    }
}