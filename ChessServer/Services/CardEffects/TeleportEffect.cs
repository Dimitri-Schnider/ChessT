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
    // Implementiert den Karteneffekt, der eine Figur auf ein anderes leeres Feld teleportiert.
    public class TeleportEffect : ICardEffect
    {
        private readonly IChessLogger _logger;
        public TeleportEffect(IChessLogger logger)
        {
            _logger = logger;
        }

        // Führt den Teleport-Effekt aus.
        public CardActivationResult Execute(GameSession session, Guid playerId, Player playerDataColor,
                                            IHistoryManager historyManager,
                                            string cardTypeId,
                                            string? fromSquareAlg,
                                            string? toSquareAlg)
        {
            if (cardTypeId != CardConstants.Teleport)
            {
                return new CardActivationResult(false, ErrorMessage: $"TeleportEffect fälschlicherweise für Karte {cardTypeId} aufgerufen.");
            }

            if (string.IsNullOrEmpty(fromSquareAlg) || string.IsNullOrEmpty(toSquareAlg))
            {
                return new CardActivationResult(false, ErrorMessage: "FromSquare oder ToSquare nicht angegeben für Teleport.");
            }

            Position fromPos;
            Position toPos;
            try
            {
                fromPos = GameSession.ParsePos(fromSquareAlg);
                toPos = GameSession.ParsePos(toSquareAlg);
            }
            catch (ArgumentException ex)
            {
                return new CardActivationResult(false, ErrorMessage: $"Ungültige Koordinaten für Teleport: {ex.Message}");
            }

            // Validiert, dass eine eigene Figur auf dem Startfeld steht.
            Piece? pieceToMove = session.CurrentGameState.Board[fromPos];
            if (pieceToMove == null || pieceToMove.Color != playerDataColor)
            {
                return new CardActivationResult(false, ErrorMessage: "Ungültige Figur auf FromSquare für Teleport ausgewählt.");
            }

            // Validiert, dass das Zielfeld leer ist.
            if (!session.CurrentGameState.Board.IsEmpty(toPos))
            {
                return new CardActivationResult(false, ErrorMessage: "ToSquare für Teleport ist nicht leer.");
            }

            // Verwendet eine spezielle Move-Klasse, um die Legalität zu prüfen (insbesondere Selbst-Schach).
            var teleportMove = new TeleportMove(fromPos, toPos);
            if (!teleportMove.IsLegal(session.CurrentGameState.Board))
            {
                return new CardActivationResult(false, ErrorMessage: "Teleport würde eigenen König ins Schach stellen oder ist anderweitig ungültig.");
            }

            // Protokolliert die Aktion im Spielverlauf.
            historyManager.AddMove(new PlayedMoveDto
            {
                PlayerId = playerId,
                PlayerColor = playerDataColor,
                From = fromSquareAlg,
                To = toSquareAlg,
                ActualMoveType = MoveType.Teleport,
                PieceMoved = $"{pieceToMove.Color} {pieceToMove.Type}",
                TimestampUtc = DateTime.UtcNow,
                TimeTaken = TimeSpan.Zero,
                RemainingTimeWhite = session.TimerService.GetCurrentTimeForPlayer(Player.White),
                RemainingTimeBlack = session.TimerService.GetCurrentTimeForPlayer(Player.Black)
            });
            // Führt den Teleport auf dem Brett aus.
            teleportMove.Execute(session.CurrentGameState.Board);

            _logger.LogTeleportEffectExecuted(fromSquareAlg, toSquareAlg, playerId, session.GameId);
            // Definiert die betroffenen Felder für die visuelle Hervorhebung.
            var affectedSquares = new List<AffectedSquareInfo>
            {
                new AffectedSquareInfo { Square = fromSquareAlg, Type = "card-teleport-from" },
                new AffectedSquareInfo { Square = toSquareAlg, Type = "card-teleport-to" }
            };
            return new CardActivationResult(true, BoardUpdatedByCardEffect: true, AffectedSquaresByCard: affectedSquares, EndsPlayerTurn: true);
        }
    }
}