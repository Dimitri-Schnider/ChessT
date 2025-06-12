using Chess.Logging;
using ChessLogic;
using ChessLogic.Utilities;
using ChessNetwork.Configuration;
using ChessNetwork.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessServer.Services.CardEffects
{
    public class RebirthEffect : ICardEffect
    {
        private readonly IChessLogger _logger;
        public RebirthEffect(IChessLogger logger) { _logger = logger; }

        private static Piece CreateNewPieceByType(PieceType type, Player color) => type switch
        {
            PieceType.Queen => new Queen(color),
            PieceType.Rook => new Rook(color),
            PieceType.Bishop => new Bishop(color),
            PieceType.Knight => new Knight(color),
            _ => throw new ArgumentException($"Ungültiger Typ für Wiederbelebung: {type}"),
        };

        public CardActivationResult Execute(GameSession session, Guid playerId, Player playerDataColor, IHistoryManager historyManager, string cardTypeId, string? fromSquareAlg, string? toSquareAlg)
        {
            if (cardTypeId != CardConstants.Wiedergeburt || !Enum.TryParse<PieceType>(fromSquareAlg, true, out var pieceType) || toSquareAlg == null)
                return new CardActivationResult(false, ErrorMessage: "Ungültige Anfrage für Wiedergeburt.");
            var targetPos = GameSession.ParsePos(toSquareAlg);

            List<Position> possibleOriginalSquares = PieceHelper.GetOriginalStartSquares(pieceType, playerDataColor);
            if (!possibleOriginalSquares.Any(s => s.Row == targetPos.Row && s.Column == targetPos.Column))
            {
                _logger.LogRebirthEffectFailedEnum($"{toSquareAlg} ist kein gültiges Ursprungsfeld für {pieceType}.", pieceType, toSquareAlg, session.GameId);
                // Wichtig: true zurückgeben, damit die Karte als "verbraucht" gilt, wie in der alten Logik.
                return new CardActivationResult(true, ErrorMessage: $"{toSquareAlg} ist kein gültiges Ursprungsfeld für {pieceType}. Karte verbraucht.", EndsPlayerTurn: true);
            }

            if (!session.CardManager.GetCapturedPieceTypesOfPlayer(playerDataColor).Any(p => p.Type == pieceType))
            {
                _logger.LogRebirthEffectFailedEnum($"Spieler hat keinen geschlagenen {pieceType}.", pieceType, toSquareAlg, session.GameId);
                return new CardActivationResult(true, ErrorMessage: $"Du hast keinen geschlagenen {pieceType} zum Wiederbeleben.", EndsPlayerTurn: true);
            }

            if (!session.CurrentGameState.Board.IsEmpty(targetPos))
                return new CardActivationResult(true, ErrorMessage: $"Feld {toSquareAlg} ist besetzt.", EndsPlayerTurn: true);
            Board boardCopy = session.CurrentGameState.Board.Copy();
            boardCopy[targetPos] = CreateNewPieceByType(pieceType, playerDataColor);
            if (boardCopy.IsInCheck(playerDataColor))
                return new CardActivationResult(false, ErrorMessage: "Wiederbelebung nicht möglich, da König im Schach stehen würde.");

            var newPiece = CreateNewPieceByType(pieceType, playerDataColor);
            session.CurrentGameState.Board[targetPos] = newPiece;
            session.CardManager.RemoveCapturedPieceOfType(playerDataColor, pieceType);

            // HINZUGEFÜGT: Protokollierung des Zugs im Spielverlauf
            historyManager.AddMove(new PlayedMoveDto
            {
                PlayerId = playerId,
                PlayerColor = playerDataColor,
                From = "graveyard", // Spezieller Wert für Wiederbelebung
                To = toSquareAlg,
                ActualMoveType = MoveType.Rebirth,
                PieceMoved = $"{newPiece.Color} {newPiece.Type}",
                TimestampUtc = DateTime.UtcNow,
                TimeTaken = TimeSpan.Zero,
                RemainingTimeWhite = session.TimerService.GetCurrentTimeForPlayer(Player.White),
                RemainingTimeBlack = session.TimerService.GetCurrentTimeForPlayer(Player.Black)
            });

            _logger.LogRebirthEffectExecuted(pieceType, toSquareAlg, playerDataColor, playerId, session.GameId);
            var affectedSquares = new List<AffectedSquareInfo> { new() { Square = toSquareAlg, Type = "card-rebirth" } };
            return new CardActivationResult(true, BoardUpdatedByCardEffect: true, AffectedSquaresByCard: affectedSquares);
        }
    }
}