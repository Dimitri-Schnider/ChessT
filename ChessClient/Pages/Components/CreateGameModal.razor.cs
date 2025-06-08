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

        private bool _isSubmitting;

        // NEUE Properties
        private OpponentType SelectedOpponentType { get; set; } = OpponentType.Human;
        private ComputerDifficulty SelectedComputerDifficulty { get; set; } = ComputerDifficulty.Medium;

        // NEU: Methode zum Zurücksetzen des Zustands hinzugefügt
        protected override void OnParametersSet()
        {
            // Wenn das Modal nicht sichtbar ist, stellen wir sicher, dass der "submitting"-Status zurückgesetzt wird.
            if (!IsVisible)
            {
                _isSubmitting = false;
            }
        }

        // Behandelt den Klick auf den "Spiel erstellen"-Button.
        private async Task HandleCreateGame()
        {
            _isSubmitting = true;

            if (string.IsNullOrWhiteSpace(PlayerName)) // Validierung des Spielernamens.
            {
                ModalErrorMessage = "Bitte gib einen Spielernamen ein.";
                _isSubmitting = false;
                return;
            }
            ModalErrorMessage = ""; // Setzt Fehlermeldung zurück.
            await OnCreateGame.InvokeAsync(new CreateGameParameters
            {
                Name = PlayerName,
                Color = SelectedColor,
                TimeMinutes = InitialTimeMinutes,
                OpponentType = SelectedOpponentType,
                ComputerDifficulty = SelectedComputerDifficulty
            });
        }

        // Schliesst das Modal und setzt die Eingabefelder zurück.
        private async Task CloseModal()
        {
            PlayerName = "";
            SelectedColor = Player.White;
            InitialTimeMinutes = 15;
            ModalErrorMessage = "";
            SelectedOpponentType = OpponentType.Human;
            SelectedComputerDifficulty = ComputerDifficulty.Easy;

            _isSubmitting = false;

            await OnClose.InvokeAsync(); // Löst das OnClose Event aus.
        }
    }
}