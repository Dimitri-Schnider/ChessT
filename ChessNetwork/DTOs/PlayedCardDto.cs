using ChessLogic;
using System;

namespace ChessNetwork.DTOs
{
    // DTO zur Speicherung von Informationen über eine gespielte Karte im Verlauf.
    public class PlayedCardDto
    {
        public int MoveNumberWhenActivated { get; set; }    // Zugnummer, bei oder vor der die Karte aktiviert wurde.
        public Guid PlayerId { get; set; }                  // ID des Spielers, der die Karte gespielt hat.
        public required string PlayerName { get; set; }     // Name des Spielers, der die Karte gespielt hat.
        public Player PlayerColor { get; set; }             // Farbe des Spielers, der die Karte gespielt hat.
        public required string CardId { get; set; }         // Eindeutige ID der gespielten Karte.
        public required string CardName { get; set; }       // Name der gespielten Karte.
        public DateTime TimestampUtc { get; set; }          // UTC-Zeitstempel der Kartenaktivierung.
    }
}