using ChessClient.Models;
using ChessLogic;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;


namespace ChessClient.Pages.Components
{
    public partial class CreateGameModal
    {
        [Parameter] public bool IsVisible { get; set; } // Steuert die Sichtbarkeit des Modals.
        [Parameter] public EventCallback OnClose { get; set; } // Event-Callback, wenn das Modal geschlossen wird.
        [Parameter] public EventCallback<CreateGameParameters> OnCreateGame { get; set; } // Event-Callback, wenn ein Spiel erstellt wird.

        private string PlayerName { get; set; } = ""; // Gebundener Wert für den Spielernamen.
        private Player SelectedColor { get; set; } = Player.White; // Gebundener Wert für die ausgewählte Farbe.
        private int InitialTimeMinutes { get; set; } = 15; // Gebundener Wert für die ausgewählte Bedenkzeit.
        private string ModalErrorMessage { get; set; } = ""; // Fehlermeldung für das Modal.

        // NEUE Properties
        private OpponentType SelectedOpponentType { get; set; } = OpponentType.Human;
        private ComputerDifficulty SelectedComputerDifficulty { get; set; } = ComputerDifficulty.Medium;

        // Behandelt den Klick auf den "Spiel erstellen"-Button.
        private async Task HandleCreateGame()
        {
            if (string.IsNullOrWhiteSpace(PlayerName)) // Validierung des Spielernamens.
            {
                ModalErrorMessage = "Bitte gib einen Spielernamen ein.";
                return;
            }
            ModalErrorMessage = ""; // Setzt Fehlermeldung zurück.
            await OnCreateGame.InvokeAsync(new CreateGameParameters
            {
                Name = PlayerName,
                Color = SelectedColor,
                TimeMinutes = InitialTimeMinutes,
                OpponentType = SelectedOpponentType, // NEU
                ComputerDifficulty = SelectedComputerDifficulty // NEU
            });
        }

        // Schliesst das Modal und setzt die Eingabefelder zurück.
        private async Task CloseModal()
        {
            PlayerName = "";
            SelectedColor = Player.White;
            InitialTimeMinutes = 15;
            ModalErrorMessage = "";
            SelectedOpponentType = OpponentType.Human; // Reset
            SelectedComputerDifficulty = ComputerDifficulty.Easy; // Reset
            await OnClose.InvokeAsync(); // Löst das OnClose Event aus.
        }

        // Unverändert, falls noch für andere Zwecke benötigt
        /*
        public void FocusPlayerNameInput(IJSRuntime jsRuntime)
        {
        }
        */
    }
}