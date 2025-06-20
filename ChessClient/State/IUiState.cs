using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace ChessClient.State
{
    // Definiert den Vertrag für den State-Container, der allgemeine UI-Zustände wie Info-Boxen und Animationen verwaltet.
    public interface IUiState
    {
        event Action? StateChanged;                         // Wird bei jeder Zustandsänderung ausgelöst.

        // Properties für die Info-Box
        string CurrentInfoMessageForBox { get; }            // Die aktuell in der Info-Box angezeigte Nachricht.
        bool InfoBoxShowActionButton { get; }               // Gibt an, ob die Info-Box einen Aktions-Button anzeigt.
        string InfoBoxActionButtonText { get; }             // Der Text für den Aktions-Button.
        EventCallback InfoBoxOnActionButtonClicked { get; } // Der Callback für den Klick auf den Aktions-Button.

        // Properties für Verbindungs- und Ladezustände
        bool IsConnecting { get; }                          // Gibt an, ob gerade eine Verbindung zum Server aufgebaut wird.
        bool IsCountdownVisible { get; }                    // Gibt an, ob der Spielstart-Countdown sichtbar ist.
        string CountdownMessage { get; }                    // Die im Countdown angezeigte Nachricht (z.B. "3", "2", "1").
        bool IsCreatingGame { get; }                        // Gibt an, ob das "Spiel erstellen"-Lade-Overlay angezeigt wird.

        // Properties für globale Animationen
        bool ShowWinAnimation { get; }                      // Gibt an, ob die globale Gewinn-Animation (Konfetti) läuft.
        bool ShowLossAnimation { get; }                     // Gibt an, ob die globale Verlust-Animation läuft.

        // Methoden zur Steuerung des UI-Zustands
        void TriggerWinAnimation();                         // Löst die Gewinn-Animation aus.
        void TriggerLossAnimation();                        // Löst die Verlust-Animation aus.
        void HideEndGameAnimations();                       // Versteckt alle Spielende-Animationen.
        Task SetCurrentInfoMessageForBoxAsync(              // Setzt eine Nachricht in der Info-Box.
            string message,
            bool autoClear = false,
            int durationMs = 5000,
            bool showActionButton = false,
            string actionButtonText = "Abbrechen",
            EventCallback? onActionButtonClicked = null
        );
        void ClearCurrentInfoMessageForBox();               // Leert die Info-Box manuell.
        void SetIsConnecting(bool isConnecting);            // Setzt den Verbindungsstatus.
        void ShowCountdown(string message);                 // Zeigt den Countdown an.
        void HideCountdown();                               // Versteckt den Countdown.
        void SetIsCreatingGame(bool isCreating);            // Setzt den Status für das "Spiel erstellen"-Overlay.
    }
}