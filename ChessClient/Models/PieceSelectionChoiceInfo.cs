using ChessLogic;

namespace ChessClient.Models
{
    public class PieceSelectionChoiceInfo
    {
        public PieceType Type { get; }
        public bool CanBeRevivedOnBoard { get; }
        public string? TooltipMessage { get; }

        public PieceSelectionChoiceInfo(PieceType type, bool canBeRevivedOnBoard, string? tooltipMessage = null)
        {
            Type = type;
            CanBeRevivedOnBoard = canBeRevivedOnBoard;
            TooltipMessage = tooltipMessage;
        }
    }
}