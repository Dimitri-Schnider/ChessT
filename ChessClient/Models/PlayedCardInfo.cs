using System;
using ChessNetwork.DTOs;

namespace ChessClient.Models
{
    // Hält Informationen über eine bereits im Spiel gespielte Karte.
    public class PlayedCardInfo
    {
        public required CardDto CardDefinition { get; set; }    // Die vollständige Definition der Karte, die gespielt wurde.

        public Guid PlayerId { get; set; }                      // Die ID des Spielers, der die Karte aktiviert hat.

        public DateTime Timestamp { get; set; }                 // Der Zeitstempel, wann die Karte gespielt wurde.

    }
}