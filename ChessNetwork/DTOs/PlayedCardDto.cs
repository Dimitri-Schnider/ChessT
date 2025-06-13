using ChessLogic;
using System;

namespace ChessNetwork.DTOs
{
    // DTO zur Speicherung von Informationen über eine gespielte Karte im Verlauf.
    public class PlayedCardDto
    {
        public int MoveNumberWhenActivated { get; set; }        // Zugnummer, bei oder vor der die Karte aktiviert wurde.
        public Guid PlayerId { get; set; }                      // ID des Spielers, der die Karte gespielt hat.
        public string PlayerName { get; set; } = string.Empty;  // Name des Spielers, der die Karte gespielt hat.
        public Player PlayerColor { get; set; }                 // Farbe des Spielers, der die Karte gespielt hat.
        public string CardId { get; set; } = string.Empty;      // Eindeutige ID der gespielten Karte.
        public string CardName { get; set; } = string.Empty;    // Name der gespielten Karte.
        public DateTime TimestampUtc { get; set; }              // UTC-Zeitstempel der Kartenaktivierung.
    }
}