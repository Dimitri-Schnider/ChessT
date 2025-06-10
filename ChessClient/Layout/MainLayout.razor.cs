using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;
using ChessClient.Pages.Components;
using ChessClient.State;
namespace ChessClient.Layout
{
    public partial class MainLayout : LayoutComponentBase, IDisposable
    {
        [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
        [Inject] private IGameCoreState GameCoreState { get; set; } = null!;
        private const int DesktopBreakpoint = 768;
        private RightSideDrawer? rightSideDrawerInstance;
        public Guid ActiveGameIdForDrawer { get; private set; }
        public bool CanDownloadGameHistory { get; private set; }
        private bool isMobileView;
        private bool isNavMenuOverlayOpen;
        private DotNetObjectReference<MainLayout>? dotNetHelper;
        private string GameStatusCssClass => IsGameActive ? "game-active-in-layout" : "";
        private bool IsGameActive => ActiveGameIdForDrawer != Guid.Empty;
        protected override async Task OnInitializedAsync()
        {
            dotNetHelper = DotNetObjectReference.Create(this);
            if (JSRuntime != null)
            {
                await JSRuntime.InvokeVoidAsync("layoutInterop.initializeNavMenu", dotNetHelper, DesktopBreakpoint);
            }
            isNavMenuOverlayOpen = GetIsNavMenuPinned();
        }

        [JSInvokable]
        public void UpdateViewportState(bool isDesktop)
        {
            bool previousIsMobileView = isMobileView;
            isMobileView = !isDesktop;
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

        private void ToggleNavMenuOverlay()
        {
            if (GetIsNavMenuPinned()) return;

            isNavMenuOverlayOpen = !isNavMenuOverlayOpen;
            InvokeAsync(StateHasChanged);
        }

        private void HandleRequestCloseMenuFromNav()
        {
            if (!GetIsNavMenuPinned() && isNavMenuOverlayOpen)
            {
                isNavMenuOverlayOpen = false;
                InvokeAsync(StateHasChanged);
            }
        }

        public void UpdateActiveGameId(Guid gameId)
        {
            ActiveGameIdForDrawer = gameId;
            if (gameId == Guid.Empty)
            {
                CanDownloadGameHistory = false;
                isNavMenuOverlayOpen = GetIsNavMenuPinned();
            }
            else
            {
                CanDownloadGameHistory = true;
                isNavMenuOverlayOpen = false;
            }
            InvokeAsync(StateHasChanged);
        }

        public void SetCanDownloadGameHistory(bool value)
        {
            if (CanDownloadGameHistory != value)
            {
                CanDownloadGameHistory = value;
                InvokeAsync(StateHasChanged);
            }
        }

        private void ToggleRightDrawer()
        {
            rightSideDrawerInstance?.Toggle();
        }

        private bool GetIsNavMenuPinned()
        {
            return !isMobileView && !IsGameActive;
        }

        private bool ShouldShowGlobalBurgerButton()
        {
            if (GetIsNavMenuPinned())
            {
                return false;
            }
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