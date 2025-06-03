using System;
using ChessNetwork.DTOs; 

namespace ChessClient.Models
{
    // Informationen über eine gespielte Karte, inklusive Spieler und Zeitpunkt.
    public class PlayedCardInfo
    {
        public required CardDto CardDefinition { get; set; } // Definition der gespielten Karte.
        public Guid PlayerId { get; set; } // ID des Spielers, der die Karte gespielt hat.
        public DateTime Timestamp { get; set; } // Zeitpunkt, zu dem die Karte gespielt wurde.
    }
}