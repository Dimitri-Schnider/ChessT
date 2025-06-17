using ChessLogic;

namespace ChessClient.Models
{
    // Enthält Informationen für eine einzelne Auswahloption im Figurenauswahl-Modal.
    public class PieceSelectionChoiceInfo
    {
        public PieceType Type { get; }              // Der Typ der Figur (z.B. Dame, Turm).

        public bool CanBeRevivedOnBoard { get; }    // Gibt an, ob diese Figur auf dem Brett wiederbelebt werden kann (z.B. ob Startfelder frei sind).

        public string? TooltipMessage { get; }      // Eine optionale Tooltip-Nachricht, z.B. um zu erklären, warum eine Option deaktiviert ist.


        // Konstruktor zur Initialisierung der Auswahloption.
        public PieceSelectionChoiceInfo(PieceType type, bool canBeRevivedOnBoard, string? tooltipMessage = null)
        {
            Type = type;
            CanBeRevivedOnBoard = canBeRevivedOnBoard;
            TooltipMessage = tooltipMessage;
        }
    }
}