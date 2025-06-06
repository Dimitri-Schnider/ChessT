using ChessLogic;
using System;

namespace ChessNetwork.DTOs
{
    // DTO, das das Ergebnis des Beitritts zu einem Spiel zurückgibt.
    public class JoinGameResultDto
    {
        public Guid PlayerId { get; set; }              // Die eindeutige ID des beigetretenen Spielers.
        public string Name { get; set; } = default!;    // Der Name des beigetretenen Spielers.
        public Player Color { get; set; }               // Die dem Spieler zugewiesene Farbe.
        public required BoardDto Board { get; set; }    // Der aktuelle Zustand des Schachbretts.
    }
}