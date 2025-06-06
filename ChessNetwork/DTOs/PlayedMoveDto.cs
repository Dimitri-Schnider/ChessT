using ChessLogic;
using System;

namespace ChessNetwork.DTOs
{
    // DTO zur Speicherung von Informationen über einen einzelnen Zug im Spielverlauf.
    public class PlayedMoveDto
    {
        public int MoveNumber { get; set; }                 // Fortlaufende Nummer des Zugs.
        public Guid PlayerId { get; set; }                  // ID des Spielers, der den Zug ausgeführt hat.
        public Player PlayerColor { get; set; }             // Farbe des Spielers, der den Zug ausgeführt hat.
        public string From { get; set; } = string.Empty;    // Startkoordinate des Zugs.
        public string To { get; set; } = string.Empty;      // Zielkoordinate des Zugs.
        public MoveType ActualMoveType { get; set; }        // Tatsächlicher Typ des Zugs.
        public PieceType? PromotionPiece { get; set; }      // Figurentyp bei Bauernumwandlung, sonst null.
        public DateTime TimestampUtc { get; set; }          // UTC-Zeitstempel der Zugausführung.
        public TimeSpan TimeTaken { get; set; }             // Für diesen Zug benötigte Zeit.
        public TimeSpan RemainingTimeWhite { get; set; }    // Verbleibende Bedenkzeit für Weiss nach diesem Zug.
        public TimeSpan RemainingTimeBlack { get; set; }    // Verbleibende Bedenkzeit für Schwarz nach diesem Zug.
        public string? PieceMoved { get; set; }             // Beschreibung der bewegten Figur.
        public string? CapturedPiece { get; set; }          // Beschreibung der geschlagenen Figur; null bei keinem Schlagzug.
    }
}