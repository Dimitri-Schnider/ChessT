using System;
using ChessLogic;

namespace ChessNetwork.DTOs
{
    // DTO zur Speicherung von Informationen über einen einzelnen Zug im Spielverlauf.
    public class PlayedMoveDto
    {
        // Fortlaufende Nummer des Zugs.
        public int MoveNumber { get; set; }
        // ID des Spielers, der den Zug ausgeführt hat.
        public Guid PlayerId { get; set; }
        // Farbe des Spielers, der den Zug ausgeführt hat.
        public Player PlayerColor { get; set; }
        // Startkoordinate des Zugs.
        public string From { get; set; } = string.Empty;
        // Zielkoordinate des Zugs.
        public string To { get; set; } = string.Empty;
        // Tatsächlicher Typ des Zugs.
        public MoveType ActualMoveType { get; set; }
        // Figurentyp bei Bauernumwandlung, sonst null.
        public PieceType? PromotionPiece { get; set; }
        // UTC-Zeitstempel der Zugausführung.
        public DateTime TimestampUtc { get; set; }
        // Für diesen Zug benötigte Zeit.
        public TimeSpan TimeTaken { get; set; }
        // Verbleibende Bedenkzeit für Weiss nach diesem Zug.
        public TimeSpan RemainingTimeWhite { get; set; }
        // Verbleibende Bedenkzeit für Schwarz nach diesem Zug.
        public TimeSpan RemainingTimeBlack { get; set; }
        // Beschreibung der bewegten Figur.
        public string? PieceMoved { get; set; }
        // Beschreibung der geschlagenen Figur; null bei keinem Schlagzug.
        public string? CapturedPiece { get; set; }
    }
}