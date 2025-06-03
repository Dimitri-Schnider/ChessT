using System;
using ChessLogic;

namespace ChessNetwork.DTOs
{
    // DTO für das Ergebnis der Spielerstellung.
    public class CreateGameResultDto
    {
        // Eindeutige ID des erstellten Spiels.
        public Guid GameId { get; set; }
        // Eindeutige ID des erstellenden Spielers.
        public Guid PlayerId { get; set; }
        // Tatsächlich zugewiesene Farbe des erstellenden Spielers.
        public Player Color { get; set; }
        // Anfänglicher Zustand des Schachbretts. Erforderlich.
        public required BoardDto Board { get; set; }
    }
}