using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;
using ChessClient.Services;

namespace ChessClient.Layout
{
    // Code-Behind für die Navigationsleiste der Anwendung.
    public partial class NavMenu : ComponentBase, IDisposable
    {
        [Inject] private NavigationManager NavigationManager { get; set; } = null!;
        [Inject] private ModalService ModalService { get; set; } = null!;
        [Inject] private TourService TourService { get; set; } = null!;

        // Parameter, die vom MainLayout übergeben werden, um den Zustand zu steuern.
        [Parameter] public bool IsPinnedModeRequest { get; set; }
        [Parameter] public bool IsOpenAsOverlayRequest { get; set; }
        [Parameter] public EventCallback OnRequestCloseMenu { get; set; }

        // Berechnet die CSS-Klassen für das Menü basierend auf seinem Zustand.
        private string NavMenuCssClasses
        {
            get
            {
                if (IsPinnedModeRequest)
                {
                    // Fixiertes Menü für Desktop ohne aktives Spiel.
                    return "sidebar desktop pinned";
                }
                // Overlay-Menü für Mobilgeräte oder wenn ein Spiel aktiv ist.
                return $"sidebar offcanvas-like {(IsOpenAsOverlayRequest ? "open" : "")}";
            }
        }

        // Behandelt Klicks auf den Inhalt, um das Menü zu schliessen, wenn es ein Overlay ist.
        private async Task HandleContentClickAndRequestClose()
        {
            if (!IsPinnedModeRequest && IsOpenAsOverlayRequest)
            {
                await OnRequestCloseMenu.InvokeAsync();
            }
        }

        // Leitet zur Schachseite und fordert das "Spiel erstellen"-Modal an.
        private async void RequestCreateGame()
        {
            NavigationManager.NavigateTo("/chess");
            ModalService.RequestShowCreateGameModal();
            if (!IsPinnedModeRequest)
            {
                await OnRequestCloseMenu.InvokeAsync();
            }
        }

        // Leitet zur Schachseite und fordert das "Spiel beitreten"-Modal an.
        private async void RequestJoinGame()
        {
            NavigationManager.NavigateTo("/chess");
            ModalService.RequestShowJoinGameModal();
            if (!IsPinnedModeRequest)
            {
                await OnRequestCloseMenu.InvokeAsync();
            }
        }

        // Startet die interaktive Tour durch die Anwendung.
        private async Task StartTour()
        {
            NavigationManager.NavigateTo("/chess");
            await TourService.RequestTourAsync();
            if (!IsPinnedModeRequest)
            {
                await OnRequestCloseMenu.InvokeAsync();
            }
        }

        // Gibt Ressourcen frei.
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}