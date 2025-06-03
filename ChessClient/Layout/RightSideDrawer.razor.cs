using ChessClient.Configuration;
using ChessClient.Services;
using ChessClient.State;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace ChessClient.Layout
{
    // Stellt eine seitliche Schublade für Einstellungen, Spieloptionen und API-Logs dar.
    public partial class RightSideDrawer : ComponentBase
    {
        // Dienst für API-Protokollierung.
        [Inject]
        private LoggingService Logger { get; set; } = null!;
        // Dienst für Navigation.
        [Inject]
        private NavigationManager NavManager { get; set; } = null!; // Aktuell nicht direkt verwendet.
        // Dienst für JavaScript-Interaktionen.
        [Inject]
        private IJSRuntime JSRuntime { get; set; } = null!;


        // NEU: Injizierte State-Objekte für das Debug-Panel
        [Inject] private IModalState _modalState { get; set; } = default!;
        [Inject] private IGameCoreState _gameCoreState { get; set; } = default!; // Falls benötigt, z.B. für GameID
        [Inject] private IHighlightState _highlightState { get; set; } = default!;
        [Inject] private ICardState _cardState { get; set; } = default!;
        // ENDE NEU

        // True, wenn die Schublade geöffnet ist.
        private bool isOpen;
        // True, wenn API-Logging aktiviert ist.
        private bool apiLoggingEnabled = true;

        // ID des aktuellen Spiels.
        [Parameter] public Guid CurrentGameId { get; set; }
        // True, wenn der Spielverlauf heruntergeladen werden kann.
        [Parameter] public bool CanDownloadHistory { get; set; }

        // Schaltet die Sichtbarkeit der Schublade um.
        public void Toggle()
        {
            isOpen = !isOpen;
            StateHasChanged();
        }

        // Öffnet die Schublade.
        public void Open()
        {
            isOpen = true;
            StateHasChanged();
        }

        // Schliesst die Schublade.
        public void Close()
        {
            isOpen = false;
            StateHasChanged();
        }

        // Schaltet das API-Logging um.
        private void ToggleApiLogging(ChangeEventArgs e)
        {
            if (e.Value is bool val)
            {
                apiLoggingEnabled = val;
                if (apiLoggingEnabled && Logger.IsPaused)
                {
                    Logger.TogglePause(); // Logging fortsetzen.
                }
                else if (!apiLoggingEnabled && !Logger.IsPaused)
                {
                    Logger.TogglePause(); // Logging pausieren.
                }
            }
        }

        // Löst den Download des Spielverlaufs aus.
        private async Task DownloadGameHistory()
        {
            if (CurrentGameId != Guid.Empty)
            {
                const string serverBaseUrl = ClientConstants.DefaultServerBaseUrl;
                var downloadUrl = $"{serverBaseUrl}/api/games/{CurrentGameId}/downloadhistory";
                if (JSRuntime != null)
                {
                    await JSRuntime.InvokeVoidAsync("window.open", downloadUrl, "_blank");
                }
            }
        }
    }
}