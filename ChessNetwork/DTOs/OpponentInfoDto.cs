using ChessLogic;
using System;

namespace ChessNetwork.DTOs
{
    // DTO zur Übermittlung der Basisinformationen eines Gegners.
    public record OpponentInfoDto(
        Guid OpponentId,        // Eindeutige ID des Gegners.
        string OpponentName,    // Anzeigename des Gegners.
        Player OpponentColor    // Farbe des Gegners (Weiss oder Schwarz).
    );
}