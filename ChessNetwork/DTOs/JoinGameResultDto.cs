using System;
using ChessLogic;

namespace ChessNetwork.DTOs
{
    // DTO für das Ergebnis des Beitritts zu einem Spiel.
    public class JoinGameResultDto
    {
        // Eindeutige ID des beigetretenen Spielers.
        public Guid PlayerId { get; set; }
        // Name des beigetretenen Spielers.
        public string Name { get; set; } = default!;
        // Zugewiesene Farbe des Spielers.
        public Player Color { get; set; }
        // Aktueller Brettzustand nach Beitritt. Erforderlich.
        public required BoardDto Board { get; set; }
    }
}