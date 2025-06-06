using System;
using ChessLogic;

namespace ChessNetwork.DTOs
{
    // DTO zur Übermittlung aktueller Bedenkzeiten.
    public record TimeUpdateDto(
        TimeSpan WhiteTime,             // Verbleibende Zeit für Weiss.
        TimeSpan BlackTime,             // Verbleibende Zeit für Schwarz.
        Player? PlayerWhoseTurnItIs     // Spieler, dessen Uhr läuft; null wenn keine Uhr aktiv.
    );
}