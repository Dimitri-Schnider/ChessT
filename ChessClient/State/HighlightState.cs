using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessClient.State
{
    // Implementiert die IHighlightState-Schnittstelle und verwaltet alle visuellen Hervorhebungen.
    public class HighlightState : IHighlightState
    {
        public event Action? StateChanged; // Wird ausgelöst, wenn sich ein Highlight-Zustand ändert.
        protected virtual void OnStateChanged() => StateChanged?.Invoke();  // Löst das Event sicher aus.

        public string? MostRecentMoveFrom { get; private set; }             // Startfeld des letzten Zuges.
        public string? MostRecentMoveTo { get; private set; }               // Zielfeld des letzten Zuges.
        public string? PenultimateMoveFrom { get; private set; }            // Startfeld des vorletzten Zuges (für Sequenzen).
        public string? PenultimateMoveTo { get; private set; }              // Zielfeld des vorletzten Zuges (für Sequenzen).
        public bool IsThirdMoveOfSequence { get; private set; }             // Spezial-Flag für den zweiten Zug eines "Extrazugs".
        public List<(string Square, string Type)> HighlightCardEffectSquares { get; private set; } = new(); // Highlights durch direkte Karteneffekte.
        public List<string> CardTargetSquaresForSelection { get; private set; } = new();    // Felder, die als Ziel für eine Kartenaktion wählbar sind.

        public HighlightState() { }

        // Setzt die Highlights für einen normalen oder einen Extrazug.
        public void SetHighlights(string? currentFrom, string? currentTo, bool isPartOfSequenceContinuing, bool isCurrentMoveTheThirdInSequence = false)
        {
            // Wenn der Zug Teil einer Sequenz ist, wird der vorherige Zug als "vorletzter" gespeichert.
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

            // Löscht andere Highlight-Typen, wenn ein Zug-Highlight gesetzt wird.
            if (currentFrom != null || currentTo != null)
            {
                if (HighlightCardEffectSquares.Count > 0) HighlightCardEffectSquares.Clear();
                if (CardTargetSquaresForSelection.Count > 0) CardTargetSquaresForSelection.Clear();
            }
            OnStateChanged();
        }

        // Setzt die Highlights, die durch einen Karteneffekt verursacht werden.
        public void SetHighlightForCardEffect(List<(string Square, string Type)> cardSquares)
        {
            // Löscht alle anderen Highlights, um Konflikte zu vermeiden.
            ClearAllActionHighlights();
            HighlightCardEffectSquares = new List<(string Square, string Type)>(cardSquares);
            OnStateChanged();
        }

        // Entfernt alle Aktions-Highlights vom Brett.
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

        // Setzt die Felder, die als gültige Ziele für eine Kartenaktion zur Auswahl stehen.
        public void SetCardTargetSquaresForSelection(List<string> squares)
        {
            ClearAllActionHighlights();
            CardTargetSquaresForSelection = new List<string>(squares);
            OnStateChanged();
        }

        // Entfernt die speziellen Highlights für die Kartenziel-Auswahl.
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