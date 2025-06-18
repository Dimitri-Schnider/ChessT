using ChessClient.Configuration;
using ChessClient.State;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ChessClient.Services.Logging;

namespace ChessClient.Layout
{
    // Stellt eine seitliche Schublade (Drawer) für Einstellungen, Spieloptionen und API-Logs dar.
    public partial class RightSideDrawer : ComponentBase
    {
        // In-memory Dienst zur Anzeige von API-Protokollen.
        [Inject]
        private LoggingService Logger { get; set; } = null!;
        // Dienst zur Interaktion mit dem Browser via JavaScript.
        [Inject]
        private IJSRuntime JSRuntime { get; set; } = null!;
        // Zugriff auf die Konfigurationsdateien (z.B. appsettings.json).
        [Inject]
        private IConfiguration Configuration { get; set; } = null!;

        // Injizierte State-Container, um auf Debug-Infos zugreifen zu können.
        [Inject] private IModalState _modalState { get; set; } = default!;
        [Inject] private IGameCoreState _gameCoreState { get; set; } = default!;
        [Inject] private IHighlightState _highlightState { get; set; } = default!;
        [Inject] private ICardState _cardState { get; set; } = default!;

        // Zustand, ob die Schublade geöffnet ist.
        private bool isOpen;
        // Zustand, ob das API-Logging aktiv ist.
        private bool apiLoggingEnabled = true;

        // ID des aktuellen Spiels, wird vom MainLayout übergeben.
        [Parameter] public Guid CurrentGameId { get; set; }
        // Gibt an, ob der Spielverlauf heruntergeladen werden kann.
        [Parameter] public bool CanDownloadHistory { get; set; }

        // Öffnet oder schliesst die Schublade.
        public void Toggle()
        {
            isOpen = !isOpen;
            StateHasChanged(); // UI-Update anstossen.
        }

        // Öffnet die Schublade explizit.
        public void Open()
        {
            isOpen = true;
            StateHasChanged();
        }

        // Schliesst die Schublade explizit.
        public void Close()
        {
            isOpen = false;
            StateHasChanged();
        }

        // Schaltet das API-Logging basierend auf der Checkbox-Auswahl um.
        private void ToggleApiLogging(ChangeEventArgs e)
        {
            if (e.Value is bool val)
            {
                apiLoggingEnabled = val;
                // Pausiert oder reaktiviert den Logging-Dienst.
                if (apiLoggingEnabled && Logger.IsPaused)
                {
                    Logger.TogglePause();
                }
                else if (!apiLoggingEnabled && !Logger.IsPaused)
                {
                    Logger.TogglePause();
                }
            }
        }

        // Baut die Download-URL zusammen und startet den Download über JavaScript.
        private async Task DownloadGameHistory()
        {
            if (CurrentGameId != Guid.Empty)
            {
                // Liest die Server-URL aus der Konfiguration, mit einem Fallback auf die lokale Adresse.
                string? serverBaseUrlFromConfig = Configuration.GetValue<string>("ServerBaseUrl");
                string serverBaseUrl = ClientConstants.DefaultServerBaseUrl;

                if (!string.IsNullOrEmpty(serverBaseUrlFromConfig))
                {
                    serverBaseUrl = serverBaseUrlFromConfig;
                }

                // Baut die vollständige URL zum API-Endpunkt.
                var downloadUrl = $"{serverBaseUrl.TrimEnd('/')}/api/games/{CurrentGameId}/downloadhistory";
                if (JSRuntime != null)
                {
                    // Öffnet die URL in einem neuen Tab, was den Download auslöst.
                    await JSRuntime.InvokeVoidAsync("window.open", downloadUrl, "_blank");
                }
            }
        }
    }
}