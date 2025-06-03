using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace ChessClient.Pages.Components
{
    public partial class InfoBox : ComponentBase
    {
        [Parameter] public string Message { get; set; } = "";
        [Parameter] public int DurationMs { get; set; } = 3000;
        [Parameter] public bool AutoHide { get; set; } = true;

        // NEUE PARAMETER
        [Parameter] public bool ShowActionButton { get; set; } = false;
        [Parameter] public string ActionButtonText { get; set; } = "Aktion";
        [Parameter] public EventCallback OnActionButtonClicked { get; set; }

        private bool Visible;
        private System.Timers.Timer? _timer;

        protected override void OnParametersSet()
        {
            if (!string.IsNullOrEmpty(Message))
            {
                Visible = true;
                if (AutoHide && !ShowActionButton) // Button verhindert AutoHide nicht mehr per se
                {
                    SetupTimer();
                }
                else if (!AutoHide || ShowActionButton) // Wenn nicht AutoHide oder Button da ist, Timer stoppen
                {
                    StopTimer();
                }
            }
            else
            {
                Visible = false;
                StopTimer();
            }
        }

        private void SetupTimer()
        {
            StopTimer(); // Bestehenden Timer stoppen, falls vorhanden
            _timer = new System.Timers.Timer(DurationMs);
            _timer.Elapsed += async (sender, e) => await HandleTimerElapsed();
            _timer.AutoReset = false;
            _timer.Enabled = true;
        }

        private void StopTimer()
        {
            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;
        }

        private async Task HandleTimerElapsed()
        {
            if (AutoHide && Visible && !ShowActionButton) // Nur ausblenden, wenn AutoHide und kein ActionButton
            {
                Visible = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task HandleActionButtonClick()
        {
            if (OnActionButtonClicked.HasDelegate)
            {
                await OnActionButtonClicked.InvokeAsync();
            }
            // Optional: InfoBox nach Klick ausblenden, wenn gewünscht
            // Visible = false;
            // await InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            StopTimer();
            GC.SuppressFinalize(this);
        }
    }
}