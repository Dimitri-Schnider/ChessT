using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;
using ChessClient.Services;

namespace ChessClient.Layout
{
    public partial class NavMenu : ComponentBase, IDisposable
    {
        [Inject]
        private NavigationManager NavigationManager { get; set; } = null!;
        [Inject]
        private ModalService ModalService { get; set; } = null!;

        // Parameter vom MainLayout
        [Parameter] public bool IsPinnedModeRequest { get; set; } // Info, ob Menü als pinned (Desktop, kein Spiel) dargestellt werden soll
        [Parameter] public bool IsOpenAsOverlayRequest { get; set; } // Anforderung vom MainLayout, ob Menü als Overlay offen sein soll
        [Parameter] public EventCallback OnRequestCloseMenu { get; set; } // Callback, um Schliessanforderung an MainLayout zu senden

        private string NavMenuCssClasses
        {
            get
            {
                if (IsPinnedModeRequest)
                {
                    return "sidebar desktop pinned"; // Ist immer offen und fixiert
                }
                // Ist im Overlay-Modus (Mobile oder Desktop mit aktivem Spiel)
                return $"sidebar offcanvas-like {(IsOpenAsOverlayRequest ? "open" : "")}";
            }
        }

        private async Task HandleContentClickAndRequestClose()
        {
            // Nur schliessen, wenn es als Overlay offen ist und nicht pinned.
            if (!IsPinnedModeRequest && IsOpenAsOverlayRequest)
            {
                await OnRequestCloseMenu.InvokeAsync();
            }
        }

        private async void RequestCreateGame()
        {
            Console.WriteLine("NavMenu: RequestCreateGame. Navigating & requesting modal.");
            NavigationManager.NavigateTo("/chess");
            ModalService.RequestShowCreateGameModal();
            // Menü nur schliessen, wenn es als Overlay agiert
            if (!IsPinnedModeRequest)
            {
                await OnRequestCloseMenu.InvokeAsync();
            }
        }

        private async void RequestJoinGame()
        {
            Console.WriteLine("NavMenu: RequestJoinGame. Navigating & requesting modal.");
            NavigationManager.NavigateTo("/chess");
            ModalService.RequestShowJoinGameModal();
            // Menü nur schliessen, wenn es als Overlay agiert
            if (!IsPinnedModeRequest)
            {
                await OnRequestCloseMenu.InvokeAsync();
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}