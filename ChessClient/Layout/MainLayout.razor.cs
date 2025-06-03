// File: [SolutionDir]/ChessClient/Layout/MainLayout.razor.cs
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;
using ChessClient.Pages.Components;

namespace ChessClient.Layout
{
    public partial class MainLayout : LayoutComponentBase, IDisposable
    {
        [Inject]
        private IJSRuntime JSRuntime { get; set; } = null!;
        private const int DesktopBreakpoint = 768;
        private RightSideDrawer? rightSideDrawerInstance;
        public Guid ActiveGameIdForDrawer { get; private set; }
        public bool CanDownloadGameHistory { get; private set; }
        private bool isMobileView;
        private bool isNavMenuOverlayOpen; // Steuert, ob das NavMenu als Overlay geöffnet ist
        private DotNetObjectReference<MainLayout>? dotNetHelper;

        private string GameStatusCssClass => IsGameActive ? "game-active-in-layout" : ""; // Nutzt die IsGameActive Property
        private bool IsGameActive => ActiveGameIdForDrawer != Guid.Empty;

        protected override async Task OnInitializedAsync()
        {
            dotNetHelper = DotNetObjectReference.Create(this);
            if (JSRuntime != null)
            {
                await JSRuntime.InvokeVoidAsync("layoutInterop.initializeNavMenu", dotNetHelper, DesktopBreakpoint);
            }
            // Initial ist das Overlay geschlossen, es sei denn, es ist Desktop ohne Spiel
            isNavMenuOverlayOpen = GetIsNavMenuPinned(); // Wenn gepinnt, dann ist es quasi "offen"
        }

        [JSInvokable]
        public void UpdateViewportState(bool isDesktop)
        {
            bool previousIsMobileView = isMobileView; // Korrekte Speicherung des vorherigen Zustands
            isMobileView = !isDesktop;

            if (isMobileView && !previousIsMobileView) // Wechsel von Desktop zu Mobile
            {
                // Wenn das Menü auf Desktop gepinnt war (also als offen galt),
                // und jetzt zu Mobile gewechselt wird, soll das Overlay geschlossen sein.
                if (isNavMenuOverlayOpen && !IsGameActive) // War Desktop, kein Spiel (also Pinned und somit 'offen')
                {
                    isNavMenuOverlayOpen = false;
                }
                // Falls es als Overlay offen war, bleibt es offen, es sei denn eine andere Logik schliesst es.
            }
            else if (!isMobileView && previousIsMobileView) // Wechsel von Mobile zu Desktop
            {
                // Wenn auf Desktop ohne aktives Spiel gewechselt wird, soll das Menü "pinned" sein (also "offen")
                if (GetIsNavMenuPinned())
                {
                    isNavMenuOverlayOpen = true;
                }
                // Falls ein Spiel aktiv ist, bleibt das Overlay geschlossen (oder wie es war)
            }

            InvokeAsync(StateHasChanged);
        }

        private void ToggleNavMenuOverlay()
        {
            // Das "pinned" Menü (Desktop, kein Spiel) kann nicht über den Burger geschlossen werden.
            // Der Burger ist in diesem Zustand nicht sichtbar.
            if (GetIsNavMenuPinned()) return;

            isNavMenuOverlayOpen = !isNavMenuOverlayOpen;
            InvokeAsync(StateHasChanged);
        }

        private void HandleRequestCloseMenuFromNav()
        {
            // Schliesst das Overlay-Menü, wenn es nicht gepinnt ist.
            if (!GetIsNavMenuPinned() && isNavMenuOverlayOpen)
            {
                isNavMenuOverlayOpen = false;
                InvokeAsync(StateHasChanged);
            }
        }

        public void UpdateActiveGameId(Guid gameId)
        {
            ActiveGameIdForDrawer = gameId;
            if (gameId == Guid.Empty) // Kein Spiel mehr aktiv
            {
                CanDownloadGameHistory = false;
                // Wenn Desktop und kein Spiel: Menü ist gepinnt (also "offen")
                // Wenn Mobile und kein Spiel: Menü ist Overlay und geschlossen
                isNavMenuOverlayOpen = GetIsNavMenuPinned();
            }
            else // Spiel ist aktiv
            {
                isNavMenuOverlayOpen = false; // Menü-Overlay bei Spielstart/aktivem Spiel immer schliessen
            }
            InvokeAsync(StateHasChanged);
        }

        public void SetCanDownloadGameHistory(bool value)
        {
            if (ActiveGameIdForDrawer != Guid.Empty && CanDownloadGameHistory != value)
            {
                CanDownloadGameHistory = value;
            }
            else if (ActiveGameIdForDrawer == Guid.Empty && CanDownloadGameHistory)
            {
                CanDownloadGameHistory = false;
            }
            InvokeAsync(StateHasChanged);
        }

        private void ToggleRightDrawer()
        {
            rightSideDrawerInstance?.Toggle();
        }

        // Hilfsmethode, um zu bestimmen, ob das NavMenu im "pinned" Modus ist
        private bool GetIsNavMenuPinned()
        {
            return !isMobileView && !IsGameActive;
        }

        // Steuert die Sichtbarkeit des globalen Burger-Buttons
        private bool ShouldShowGlobalBurgerButton()
        {
            if (GetIsNavMenuPinned()) // Wenn Menü pinned ist (Desktop, kein Spiel)
            {
                return false; // Burger nicht zeigen, Menü ist voll da
            }
            // Sonst (Mobile oder Desktop mit aktivem Spiel): Burger nur zeigen, wenn Menü-Overlay ZU ist
            return !isNavMenuOverlayOpen;
        }

        public void Dispose()
        {
            if (dotNetHelper != null)
            {
                if (JSRuntime != null)
                {
                    var JSTask = JSRuntime.InvokeVoidAsync("layoutInterop.disposeNavMenu").AsTask();
                    _ = JSTask;
                }
                dotNetHelper.Dispose();
            }
            GC.SuppressFinalize(this);
        }
    }
}