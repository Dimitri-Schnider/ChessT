using System;
using ChessLogic;

namespace ChessNetwork.DTOs
{
    // DTO zur Speicherung von Informationen über eine gespielte Karte im Verlauf.
    public class PlayedCardDto
    {
        // Zugnummer, bei oder vor der die Karte aktiviert wurde.
        public int MoveNumberWhenActivated { get; set; }
        // ID des Spielers, der die Karte gespielt hat.
        public Guid PlayerId { get; set; }
        // Name des Spielers, der die Karte gespielt hat.
        public required string PlayerName { get; set; }
        // Farbe des Spielers, der die Karte gespielt hat.
        public Player PlayerColor { get; set; }
        // Eindeutige ID der gespielten Karte.
        public required string CardId { get; set; }
        // Name der gespielten Karte.
        public required string CardName { get; set; }
        // UTC-Zeitstempel der Kartenaktivierung.
        public DateTime TimestampUtc { get; set; }
    }
}