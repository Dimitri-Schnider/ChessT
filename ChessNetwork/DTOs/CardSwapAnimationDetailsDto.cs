using System;

namespace ChessNetwork.DTOs
{
    // Überträgt die notwendigen Details an die Clients, um die Animation für den Kartentausch-Effekt darzustellen.
    public record CardSwapAnimationDetailsDto(

        Guid PlayerId,          // Die ID des Spielers, für den diese Animationsdetails bestimmt sind.
        CardDto CardGiven,      // Die Karte, die dieser Spieler abgegeben hat.
        CardDto CardReceived    // Die Karte, die dieser Spieler im Tausch neu erhalten hat.
    );
}