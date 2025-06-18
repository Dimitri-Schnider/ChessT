using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace ChessClient.Pages.Components.Info
{
    // Verwaltet die Sichtbarkeit, das automatische Ausblenden per Timer und die Logik des optionalen Aktions-Buttons.
    public partial class InfoBox : ComponentBase
    {
        [Parameter] public string Message { get; set; } = "";                   // Die anzuzeigende Nachricht.
        [Parameter] public int DurationMs { get; set; } = 3000;                 // Die Dauer in Millisekunden, nach der die Box automatisch verschwindet.
        [Parameter] public bool AutoHide { get; set; } = true;                  // Steuert, ob die Box automatisch verschwinden soll.

        // Parameter für den optionalen Aktions-Button
        [Parameter] public bool ShowActionButton { get; set; } = false;         // Bestimmt, ob der Button angezeigt wird.
        [Parameter] public string ActionButtonText { get; set; } = "Aktion";    // Der Text des Buttons.
        [Parameter] public EventCallback OnActionButtonClicked { get; set; }    // Der Callback, der bei einem Klick auf den Button ausgeführt wird.

        private bool Visible;                                                   // Interner Zustand für die Sichtbarkeit.
        private System.Timers.Timer? _timer;                                    // Timer-Objekt für das automatische Ausblenden.

        // Lifecycle-Methode: Wird aufgerufen, wenn sich die Parameter ändern.
        protected override void OnParametersSet()
        {
            if (!string.IsNullOrEmpty(Message))
            {
                Visible = true;
                // Der Timer wird nur gestartet, wenn AutoHide aktiv ist UND kein Aktions-Button angezeigt wird.
                if (AutoHide && !ShowActionButton)
                {
                    SetupTimer();
                }
                // Wenn die Box nicht automatisch ausgeblendet werden soll oder ein Button da ist, wird ein eventuell laufender Timer gestoppt.
                else if (!AutoHide || ShowActionButton)
                {
                    StopTimer();
                }
            }
            else
            {
                // Wenn keine Nachricht vorhanden ist, wird die Box ausgeblendet und der Timer gestoppt.
                Visible = false;
                StopTimer();
            }
        }

        // Initialisiert und startet den Timer.
        private void SetupTimer()
        {
            StopTimer(); // Stoppt einen eventuell bereits laufenden Timer, um ihn zurückzusetzen.
            _timer = new System.Timers.Timer(DurationMs);
            _timer.Elapsed += async (sender, e) => await HandleTimerElapsed(); // Abonniert das Elapsed-Event.
            _timer.AutoReset = false; // Der Timer soll nur einmal auslösen.
            _timer.Enabled = true;
        }

        // Stoppt und zerstört den Timer.
        private void StopTimer()
        {
            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;
        }

        // Wird aufgerufen, wenn der Timer abläuft.
        private async Task HandleTimerElapsed()
        {
            // Blendet die Box nur aus, wenn die Bedingungen (noch) erfüllt sind.
            if (AutoHide && Visible && !ShowActionButton)
            {
                Visible = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        // Behandelt den Klick auf den Aktions-Button.
        private async Task HandleActionButtonClick()
        {
            // Prüft, ob ein Callback registriert ist und führt ihn aus.
            if (OnActionButtonClicked.HasDelegate)
            {
                await OnActionButtonClicked.InvokeAsync();
            }
        }

        // Gibt die Timer-Ressource frei, wenn die Komponente zerstört wird.
        public void Dispose()
        {
            StopTimer();
            GC.SuppressFinalize(this);
        }
    }
}