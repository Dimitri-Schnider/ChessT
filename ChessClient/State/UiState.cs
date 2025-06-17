using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace ChessClient.State
{
    // Verwaltet den Zustand für temporäre oder globale UI-Elemente, die nicht direkt zum Spielbrett gehören,
    // wie Info-Boxen, Ladeanzeigen und Animationen.
    public class UiState : IUiState
    {
        public event Action? StateChanged;                                          // Event, das UI-Komponenten über Zustandsänderungen informiert.
        protected virtual void OnStateChanged() => StateChanged?.Invoke();          // Löst das StateChanged-Event sicher aus.
        public string CurrentInfoMessageForBox { get; private set; } = "";          // Die aktuell in der Info-Box angezeigte Nachricht.
        public bool IsConnecting { get; private set; }                              // Gibt an, ob gerade eine Verbindung zum Hub aufgebaut wird.
        public bool IsCountdownVisible { get; private set; }                        // Gibt an, ob der Spielstart-Countdown sichtbar ist.
        public string CountdownMessage { get; private set; } = "";                  // Die im Countdown angezeigte Nachricht (z.B. "3", "2", "1"...).
        public bool IsCreatingGame { get; private set; }                            // Gibt an, ob gerade der "Spiel erstellen"-Prozess läuft (für die Ladeanimation).
        public bool InfoBoxShowActionButton { get; private set; }                   // Gibt an, ob die Info-Box einen Aktions-Button anzeigt.
        public string InfoBoxActionButtonText { get; private set; } = "Abbrechen";  // Der Text für den Aktions-Button in der Info-Box.
        public EventCallback InfoBoxOnActionButtonClicked { get; private set; }     // Der Callback, der ausgeführt wird, wenn der Aktions-Button geklickt wird.
        public bool ShowWinAnimation { get; private set; }                          // Gibt an, ob die Gewinn-Animation (Konfetti) angezeigt werden soll.
        public bool ShowLossAnimation { get; private set; }                         // Gibt an, ob die Verlust-Animation angezeigt werden soll.
        public UiState() { }

        // Löst die Gewinn-Animation aus.
        public void TriggerWinAnimation()
        {
            ShowWinAnimation = true;
            OnStateChanged();
        }

        // Löst die Verlust-Animation aus.
        public void TriggerLossAnimation()
        {
            ShowLossAnimation = true;
            OnStateChanged();
        }

        // Versteckt alle Spielende-Animationen.
        public void HideEndGameAnimations()
        {
            ShowWinAnimation = false;
            ShowLossAnimation = false;
            OnStateChanged();
        }

        // Setzt eine Nachricht in der Info-Box, die optional nach einer Dauer automatisch verschwindet.
        public Task SetCurrentInfoMessageForBoxAsync(string message, bool autoClear = false, int durationMs = 5000, bool showActionButton = false, string actionButtonText = "Abbrechen", EventCallback? onActionButtonClicked = null)
        {
            CurrentInfoMessageForBox = message;
            InfoBoxShowActionButton = showActionButton;
            InfoBoxActionButtonText = actionButtonText;
            InfoBoxOnActionButtonClicked = onActionButtonClicked ?? new EventCallback();
            OnStateChanged();

            // Wenn die Nachricht leer ist und kein Button angezeigt wird, wird die Box sofort geleert.
            if (string.IsNullOrEmpty(message) && !showActionButton)
            {
                ClearCurrentInfoMessageForBox();
            }
            return Task.CompletedTask;
        }

        // Leert die Info-Box und setzt die zugehörigen Aktions-Button-Zustände zurück.
        public void ClearCurrentInfoMessageForBox()
        {
            CurrentInfoMessageForBox = "";
            InfoBoxShowActionButton = false;
            InfoBoxActionButtonText = "Abbrechen";
            InfoBoxOnActionButtonClicked = new EventCallback();
            OnStateChanged();
        }

        // Setzt den Zustand, ob gerade eine Verbindung aufgebaut wird.
        public void SetIsConnecting(bool isConnecting)
        {
            IsConnecting = isConnecting;
            OnStateChanged();
        }

        // Zeigt den Countdown mit einer bestimmten Nachricht an.
        public void ShowCountdown(string message)
        {
            CountdownMessage = message;
            if (!IsCountdownVisible)
            {
                IsCountdownVisible = true;
            }
            OnStateChanged();
        }

        // Versteckt den Countdown.
        public void HideCountdown()
        {
            if (IsCountdownVisible)
            {
                IsCountdownVisible = false;
                CountdownMessage = "";
                OnStateChanged();
            }
        }

        // Setzt den Zustand, ob gerade ein Spiel erstellt wird (für die Ladeanzeige).
        public void SetIsCreatingGame(bool isCreating)
        {
            if (IsCreatingGame == isCreating) return;
            IsCreatingGame = isCreating;
            OnStateChanged();
        }
    }
}