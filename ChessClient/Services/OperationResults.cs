using ChessNetwork.DTOs;

namespace ChessClient.Services
{
    // Definiert die möglichen Ergebnisse einer Spielerzug-Verarbeitung.
    public enum PlayerMoveOutcome
    {
        Success,
        InvalidMove,
        PawnPromotionPending,
        Error
    }

    // Kapselt das Ergebnis einer Zugverarbeitung, inklusive eventuell benötigter Daten.
    public record PlayerMoveProcessingResult(
        PlayerMoveOutcome Outcome,
        MoveDto? PendingPromotionMove = null,
        string? ErrorMessage = null
    );

    // Definiert die möglichen Ergebnisse einer Kartenaktivierung.
    public enum CardActivationOutcome
    {
        Success,
        Error
    }

    // Kapselt das Endergebnis einer Kartenaktivierung, das an den Client zurückgegeben wird.
    public record CardActivationFinalizationResult(
        CardActivationOutcome Outcome,
        string? ErrorMessage = null,
        bool EndsPlayerTurn = true,
        PositionDto? PawnPromotionPendingAt = null
    );
}