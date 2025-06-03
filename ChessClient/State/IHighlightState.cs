using System;
using System.Collections.Generic;

namespace ChessClient.State
{
    public interface IHighlightState
    {
        event Action? StateChanged;
        string? MostRecentMoveFrom { get; }
        string? MostRecentMoveTo { get; }
        string? PenultimateMoveFrom { get; }
        string? PenultimateMoveTo { get; }
        bool IsThirdMoveOfSequence { get; }
        List<(string Square, string Type)> HighlightCardEffectSquares { get; }

        // Neu für die Auswahl von Zielfeldern auf dem Brett für Karteneffekte
        List<string> CardTargetSquaresForSelection { get; }

        void SetHighlights(string? currentFrom, string? currentTo, bool isPartOfSequenceContinuing, bool isCurrentMoveTheThirdInSequence = false);
        void SetHighlightForCardEffect(List<(string Square, string Type)> cardSquares);
        void ClearAllActionHighlights();

        // Neu
        void SetCardTargetSquaresForSelection(List<string> squares);
        void ClearCardTargetSquaresForSelection();
    }
}