using System;

namespace ChessNetwork.DTOs
{
    // DTO zur Repräsentation eines Spielers.
    public record PlayerDto(
            Guid Id,     // Eindeutige ID des Spielers.
            string Name  // Anzeigename des Spielers.
        );
}