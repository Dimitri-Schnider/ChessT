using ChessLogic;
using System;

namespace ChessNetwork.DTOs
{
    // DTO, das das Ergebnis der Spielerstellung an den Client zurückgibt.
    public class CreateGameResultDto
    {
        public Guid GameId { get; set; }                // Die eindeutige ID des neu erstellten Spiels.
        public Guid PlayerId { get; set; }              // Die eindeutige ID des erstellenden Spielers.
        public Player Color { get; set; }               // Die tatsächlich zugewiesene Farbe des erstellenden Spielers.
        public BoardDto Board { get; set; } = default!; // Der anfängliche Zustand des Schachbretts.
    }
}
