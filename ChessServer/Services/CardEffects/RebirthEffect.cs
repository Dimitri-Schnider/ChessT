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
    // Implementiert den Karteneffekt "Wiedergeburt", der eine geschlagene Figur wiederbelebt.
    public class RebirthEffect : ICardEffect
    {
        private readonly IChessLogger _logger;
        public RebirthEffect(IChessLogger logger) { _logger = logger; }

        // Hilfsmethode, um ein neues Piece-Objekt basierend auf Typ und Farbe zu erstellen.
        private static Piece CreateNewPieceByType(PieceType type, Player color) => type switch
        {
            PieceType.Queen => new Queen(color),
            PieceType.Rook => new Rook(color),
            PieceType.Bishop => new Bishop(color),
            PieceType.Knight => new Knight(color),
            _ => throw new ArgumentException($"Ungültiger Typ für Wiederbelebung: {type}"),
        };

        // Führt den Wiedergeburts-Effekt aus.
        public CardActivationResult Execute(GameSession session, Guid playerId, Player playerDataColor, IHistoryManager historyManager, string cardTypeId, string? fromSquareAlg, string? toSquareAlg)
        {
            // `fromSquareAlg` wird hier für den Figurentyp, `toSquareAlg` für das Zielfeld missbraucht.
            if (cardTypeId != CardConstants.Wiedergeburt || !Enum.TryParse<PieceType>(fromSquareAlg, true, out var pieceType) || toSquareAlg == null)
                return new CardActivationResult(false, ErrorMessage: "Ungültige Anfrage für Wiedergeburt.");
            var targetPos = GameSession.ParsePos(toSquareAlg);

            // Holt die ursprünglichen Startfelder für den gewählten Figurentyp.
            List<Position> possibleOriginalSquares = PieceHelper.GetOriginalStartSquares(pieceType, playerDataColor);
            // Prüft, ob das gewählte Zielfeld ein gültiges Startfeld ist.
            if (!possibleOriginalSquares.Any(s => s.Row == targetPos.Row && s.Column == targetPos.Column))
            {
                _logger.LogRebirthEffectFailedEnum($"{toSquareAlg} ist kein gültiges Ursprungsfeld für {pieceType}.", pieceType, toSquareAlg, session.GameId);
                // Wichtig: Karte wird trotzdem als verbraucht markiert, um Missbrauch zu verhindern.
                return new CardActivationResult(true, ErrorMessage: $"{toSquareAlg} ist kein gültiges Ursprungsfeld für {pieceType}. Karte verbraucht.", EndsPlayerTurn: true);
            }

            // Prüft, ob der Spieler überhaupt eine geschlagene Figur dieses Typs besitzt.
            if (!session.CardManager.GetCapturedPieceTypesOfPlayer(playerDataColor).Any(p => p.Type == pieceType))
            {
                _logger.LogRebirthEffectFailedEnum($"Spieler hat keinen geschlagenen {pieceType}.", pieceType, toSquareAlg, session.GameId);
                return new CardActivationResult(true, ErrorMessage: $"Du hast keinen geschlagenen {pieceType} zum Wiederbeleben.", EndsPlayerTurn: true);
            }

            // Prüft, ob das Zielfeld besetzt ist.
            if (!session.CurrentGameState.Board.IsEmpty(targetPos))
                return new CardActivationResult(true, ErrorMessage: $"Feld {toSquareAlg} ist besetzt.", EndsPlayerTurn: true);

            // Führt eine hypothetische Wiederbelebung durch, um zu prüfen, ob der König dadurch ins Schach gerät.
            Board boardCopy = session.CurrentGameState.Board.Copy();
            boardCopy[targetPos] = CreateNewPieceByType(pieceType, playerDataColor);
            if (boardCopy.IsInCheck(playerDataColor))
                return new CardActivationResult(false, ErrorMessage: "Wiederbelebung nicht möglich, da König im Schach stehen würde.");

            // Führt die Wiederbelebung auf dem echten Brett durch.
            var newPiece = CreateNewPieceByType(pieceType, playerDataColor);
            session.CurrentGameState.Board[targetPos] = newPiece;
            // Entfernt die Figur aus der Liste der geschlagenen Figuren.
            session.CardManager.RemoveCapturedPieceOfType(playerDataColor, pieceType);

            // Protokolliert die Aktion im Spielverlauf.
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