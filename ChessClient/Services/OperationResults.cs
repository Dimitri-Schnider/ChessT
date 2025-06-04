using ChessNetwork.DTOs; 

namespace ChessClient.Services
{
    public enum PlayerMoveOutcome
    {
        Success,
        InvalidMove,
        PawnPromotionPending,
        Error
    }

    public record PlayerMoveProcessingResult(
        PlayerMoveOutcome Outcome,
        MoveDto? PendingPromotionMove = null,
        string? ErrorMessage = null
    );
    public enum CardActivationOutcome
    {
        Success,
        Error
    }

    public record CardActivationFinalizationResult(
        CardActivationOutcome Outcome,
        string? ErrorMessage = null,
        // NEUE FELDER:
        bool EndsPlayerTurn = true,
        PositionDto? PawnPromotionPendingAt = null
    );
}