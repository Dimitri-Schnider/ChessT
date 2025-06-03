// File: [SolutionDir]/ChessClient/State/HighlightState.cs
using System;
using System.Collections.Generic;
using System.Linq; // Nötig für .Any() falls es doch verwendet wird, oder .Count > 0

namespace ChessClient.State
{
    public class HighlightState : IHighlightState
    {
        public event Action? StateChanged;
        protected virtual void OnStateChanged() => StateChanged?.Invoke();

        public string? MostRecentMoveFrom { get; private set; }
        public string? MostRecentMoveTo { get; private set; }
        public string? PenultimateMoveFrom { get; private set; }
        public string? PenultimateMoveTo { get; private set; }
        public bool IsThirdMoveOfSequence { get; private set; }
        public List<(string Square, string Type)> HighlightCardEffectSquares { get; private set; } = new();

        public List<string> CardTargetSquaresForSelection { get; private set; } = new();

        public HighlightState()
        {
        }

        public void SetHighlights(string? currentFrom, string? currentTo, bool isPartOfSequenceContinuing, bool isCurrentMoveTheThirdInSequence = false)
        {
            if (isPartOfSequenceContinuing)
            {
                PenultimateMoveFrom = MostRecentMoveFrom;
                PenultimateMoveTo = MostRecentMoveTo;
            }
            else
            {
                PenultimateMoveFrom = null;
                PenultimateMoveTo = null;
            }

            MostRecentMoveFrom = currentFrom;
            MostRecentMoveTo = currentTo;
            IsThirdMoveOfSequence = isPartOfSequenceContinuing && isCurrentMoveTheThirdInSequence;

            if (currentFrom != null || currentTo != null)
            {
                if (HighlightCardEffectSquares.Count > 0) HighlightCardEffectSquares.Clear();
                if (CardTargetSquaresForSelection.Count > 0) CardTargetSquaresForSelection.Clear();
            }
            OnStateChanged();
        }

        public void SetHighlightForCardEffect(List<(string Square, string Type)> cardSquares)
        {
            MostRecentMoveFrom = null;
            MostRecentMoveTo = null;
            PenultimateMoveFrom = null;
            PenultimateMoveTo = null;
            IsThirdMoveOfSequence = false;
            if (CardTargetSquaresForSelection.Count > 0) CardTargetSquaresForSelection.Clear();
            HighlightCardEffectSquares = new List<(string Square, string Type)>(cardSquares);
            OnStateChanged();
        }

        public void ClearAllActionHighlights()
        {
            MostRecentMoveFrom = null;
            MostRecentMoveTo = null;
            PenultimateMoveFrom = null;
            PenultimateMoveTo = null;
            IsThirdMoveOfSequence = false;
            if (HighlightCardEffectSquares.Count > 0) HighlightCardEffectSquares.Clear(); 
            if (CardTargetSquaresForSelection.Count > 0) CardTargetSquaresForSelection.Clear(); 
            OnStateChanged();
        }

        public void SetCardTargetSquaresForSelection(List<string> squares)
        {
            ClearAllActionHighlights();
            CardTargetSquaresForSelection = new List<string>(squares);
            OnStateChanged();
        }

        public void ClearCardTargetSquaresForSelection()
        {
            if (CardTargetSquaresForSelection.Count > 0)
            {
                CardTargetSquaresForSelection.Clear();
                OnStateChanged();
            }
        }
    }
}