using System;

namespace ChessNetwork.DTOs
{
    public record CardSwapAnimationDetailsDto(
        Guid PlayerId,          // ID des Spielers, für den diese Details sind
        CardDto CardGiven,      // Die Karte, die dieser Spieler abgegeben hat (enthält InstanceId)
        CardDto CardReceived    // Die Karte, die dieser Spieler neu erhalten hat (enthält InstanceId)
    );
}