using System;
using System.Collections.Generic;

namespace ChessClient.State
{
    // Definiert den Vertrag für den State-Container, der alle visuellen Hervorhebungen auf dem Schachbrett verwaltet.
    public interface IHighlightState
    {
        // Event, das bei Änderungen ausgelöst wird.
        event Action? StateChanged;

        // --- Properties für die Hervorhebung des letzten Zugs ---
        string? MostRecentMoveFrom { get; }
        string? MostRecentMoveTo { get; }
        string? PenultimateMoveFrom { get; } // Vorletzter Zug, wichtig für "Extrazug"-Sequenzen
        string? PenultimateMoveTo { get; }
        bool IsThirdMoveOfSequence { get; } // Spezial-Flag für den 2. Zug des Extrazugs

        // --- Properties für Karteneffekte ---
        List<(string Square, string Type)> HighlightCardEffectSquares { get; }
        List<string> CardTargetSquaresForSelection { get; } // Felder, die für eine Kartenaktion ausgewählt werden können

        // --- Methoden zur Steuerung der Highlights ---

        // Setzt die Highlights für den letzten Zug.
        void SetHighlights(string? currentFrom, string? currentTo, bool isPartOfSequenceContinuing, bool isCurrentMoveTheThirdInSequence = false);
        // Setzt die Highlights für einen Karteneffekt (z.B. Teleport, Tausch).
        void SetHighlightForCardEffect(List<(string Square, string Type)> cardSquares);
        // Entfernt alle Aktions-Highlights vom Brett.
        void ClearAllActionHighlights();
        // Setzt die Felder, die als gültige Ziele für eine Kartenaktion markiert werden sollen.
        void SetCardTargetSquaresForSelection(List<string> squares);
        // Entfernt die Hervorhebungen für die Kartenziel-Auswahl.
        void ClearCardTargetSquaresForSelection();
    }
}