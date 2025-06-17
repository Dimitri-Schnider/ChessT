using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;
using ChessClient.Pages.Components;
using ChessClient.State;

namespace ChessClient.Layout
{
    // Code-Behind für das Hauptlayout der Anwendung. Verwaltet globale UI-Zustände wie das Navigationsmenü und den Seiten-Drawer.
    public partial class MainLayout : LayoutComponentBase, IDisposable
    {
        [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
        [Inject] private IGameCoreState GameCoreState { get; set; } = null!;

        // Definiert die Bildschirmbreite, an der die Ansicht von mobil zu desktop wechselt.
        private const int DesktopBreakpoint = 768;

        // Referenz auf die Komponente der rechten Seitenleiste (Drawer).
        private RightSideDrawer? rightSideDrawerInstance;

        // ID des aktiven Spiels, wird an den Drawer weitergegeben.
        public Guid ActiveGameIdForDrawer { get; private set; }

        // Gibt an, ob der Spielverlauf für das aktuelle Spiel heruntergeladen werden kann.
        public bool CanDownloadGameHistory { get; private set; }

        // Zustandvariablen für die Navigation und Ansicht.
        private bool isMobileView;
        private bool isNavMenuOverlayOpen;

        // .NET-Referenz für JavaScript-Interop-Aufrufe.
        private DotNetObjectReference<MainLayout>? dotNetHelper;

        // Berechnet eine CSS-Klasse basierend darauf, ob ein Spiel aktiv ist.
        private string GameStatusCssClass => IsGameActive ? "game-active-in-layout" : "";
        private bool IsGameActive => ActiveGameIdForDrawer != Guid.Empty;

        // Initialisiert die Komponente und registriert JavaScript-Interop für die Viewport-Größenüberwachung.
        protected override async Task OnInitializedAsync()
        {
            dotNetHelper = DotNetObjectReference.Create(this);
            if (JSRuntime != null)
            {
                // Ruft JS auf, um einen Resize-Listener für das Fenster hinzuzufügen.
                await JSRuntime.InvokeVoidAsync("layoutInterop.initializeNavMenu", dotNetHelper, DesktopBreakpoint);
            }
            // Setzt den initialen Zustand des Navigationsmenüs.
            isNavMenuOverlayOpen = GetIsNavMenuPinned();
        }

        // Wird von JavaScript aufgerufen, wenn sich die Grösse des Viewports ändert (mobil vs. desktop).
        [JSInvokable]
        public void UpdateViewportState(bool isDesktop)
        {
            bool previousIsMobileView = isMobileView;
            isMobileView = !isDesktop;

            // Logik, um das Navigationsmenü bei Wechsel der Ansicht korrekt ein- oder auszublenden.
            if (isMobileView && !previousIsMobileView)
            {
                if (isNavMenuOverlayOpen && !IsGameActive)
                {
                    isNavMenuOverlayOpen = false;
                }
            }
            else if (!isMobileView && previousIsMobileView)
            {
                if (GetIsNavMenuPinned())
                {
                    isNavMenuOverlayOpen = true;
                }
            }

            InvokeAsync(StateHasChanged);
        }

        // Schaltet das Navigationsmenü-Overlay um (nur im mobilen Modus oder wenn nicht gepinnt).
        private void ToggleNavMenuOverlay()
        {
            if (GetIsNavMenuPinned()) return;
            isNavMenuOverlayOpen = !isNavMenuOverlayOpen;
            InvokeAsync(StateHasChanged);
        }

        // Schliesst das Navigationsmenü, wenn es als Overlay angezeigt wird.
        private void HandleRequestCloseMenuFromNav()
        {
            if (!GetIsNavMenuPinned() && isNavMenuOverlayOpen)
            {
                isNavMenuOverlayOpen = false;
                InvokeAsync(StateHasChanged);
            }
        }

        // Wird von der `Chess.razor`-Seite aufgerufen, um das Layout über das aktive Spiel zu informieren.
        public void UpdateActiveGameId(Guid gameId)
        {
            ActiveGameIdForDrawer = gameId;
            if (gameId == Guid.Empty)
            {
                // Kein Spiel aktiv: Verlauf nicht herunterladbar.
                CanDownloadGameHistory = false;
                // Menü-Zustand zurücksetzen.
                isNavMenuOverlayOpen = GetIsNavMenuPinned();
            }
            else
            {
                // Spiel aktiv: Verlauf herunterladbar.
                CanDownloadGameHistory = true;
                // Menü schliessen, um Platz für das Spiel zu machen.
                isNavMenuOverlayOpen = false;
            }
            InvokeAsync(StateHasChanged);
        }

        // Setzt die Berechtigung zum Herunterladen des Spielverlaufs.
        public void SetCanDownloadGameHistory(bool value)
        {
            if (CanDownloadGameHistory != value)
            {
                CanDownloadGameHistory = value;
                InvokeAsync(StateHasChanged);
            }
        }

        // Öffnet oder schliesst die rechte Seitenleiste.
        private void ToggleRightDrawer()
        {
            rightSideDrawerInstance?.Toggle();
        }

        // Bestimmt, ob das Navigationsmenü fixiert (pinned) sein soll.
        private bool GetIsNavMenuPinned()
        {
            // Pinned, wenn Desktop-Ansicht UND kein Spiel aktiv ist.
            return !isMobileView && !IsGameActive;
        }

        // Bestimmt, ob der globale Burger-Button zum Öffnen des Menüs angezeigt werden soll.
        private bool ShouldShowGlobalBurgerButton()
        {
            // Nicht anzeigen, wenn das Menü fixiert ist.
            if (GetIsNavMenuPinned())
            {
                return false;
            }
            // Nur anzeigen, wenn das Overlay geschlossen ist.
            return !isNavMenuOverlayOpen;
        }

        // Gibt Ressourcen frei und entfernt den JS-Listener.
        public void Dispose()
        {
            if (dotNetHelper != null)
            {
                if (JSRuntime != null)
                {
                    // Asynchroner Aufruf zum Aufräumen im JavaScript.
                    var JSTask = JSRuntime.InvokeVoidAsync("layoutInterop.disposeNavMenu").AsTask();
                    _ = JSTask;
                }
                dotNetHelper.Dispose();
            }
            GC.SuppressFinalize(this);
        }
    }
}