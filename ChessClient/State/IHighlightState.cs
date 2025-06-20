using System;
using System.Collections.Generic;

namespace ChessClient.State
{
    // Definiert den Vertrag für den State-Container, der alle visuellen Hervorhebungen auf dem Schachbrett verwaltet.
    public interface IHighlightState
    {
        event Action? StateChanged; // Event, das bei Änderungen ausgelöst wird.

        // Properties für die Hervorhebung des letzten Zugs
        string? MostRecentMoveFrom { get; }     // Das Startfeld des letzten Zuges.
        string? MostRecentMoveTo { get; }       // Das Zielfeld des letzten Zuges.
        string? PenultimateMoveFrom { get; }    // Vorletzter Zug, wichtig für "Extrazug"-Sequenzen.
        string? PenultimateMoveTo { get; }      // Vorletzter Zug, wichtig für "Extrazug"-Sequenzen.
        bool IsThirdMoveOfSequence { get; }     // Spezial-Flag für den 2. Zug des Extrazugs.

        // Properties für Karteneffekte
        List<(string Square, string Type)> HighlightCardEffectSquares { get; }  // Felder, die durch einen direkten Karteneffekt hervorgehoben werden.
        List<string> CardTargetSquaresForSelection { get; }                     // Felder, die für eine Kartenaktion ausgewählt werden können (z.B. Wiedergeburts-Ziele).

        // Methoden zur Steuerung der Highlights

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