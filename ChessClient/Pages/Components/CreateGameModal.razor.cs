using ChessClient.Models;
using ChessLogic;
using ChessNetwork.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;


namespace ChessClient.Pages.Components
{
    // Die Code-Behind-Klasse für das CreateGameModal.
    // Sie verwaltet den Zustand der Eingabefelder und löst die entsprechenden
    // Events aus, um mit der übergeordneten Komponente zu kommunizieren.
    public partial class CreateGameModal
    {
        // --- PARAMETER ---
        [Parameter] public bool IsVisible { get; set; }                                     // Steuert die Sichtbarkeit des Modals.
        [Parameter] public EventCallback OnClose { get; set; }                              // Event-Callback, der aufgerufen wird, wenn das Modal geschlossen wird.
        [Parameter] public EventCallback<CreateGameDto> OnCreateGame { get; set; }          // Event-Callback, der die gesammelten Spieldaten an den Aufrufer übergibt.

        // --- PRIVATE PROPERTIES (ZUSTAND) ---
        private string PlayerName { get; set; } = "";                                       // Gebundener Wert für das Spielernamen-Eingabefeld.
        private Player SelectedColor { get; set; } = Player.White;                          // Gebundener Wert für die Farbauswahl.
        private int InitialTimeMinutes { get; set; } = 15;                                  // Gebundener Wert für die Bedenkzeit-Auswahl.
        private string ModalErrorMessage { get; set; } = "";                                // Speichert eine Fehlermeldung, die im Modal angezeigt wird.

        private bool _isSubmitting;                                                         // Flag, um mehrfaches Absenden des Formulars zu verhindern.

        // Neue Properties für die Auswahl des Gegners.
        private OpponentType SelectedOpponentType { get; set; } = OpponentType.Human;                   // Gebundener Wert für die Auswahl des Gegnertyps.
        private ComputerDifficulty SelectedComputerDifficulty { get; set; } = ComputerDifficulty.Medium; // Gebundener Wert für die Auswahl der Computerstärke.

        // Lifecycle-Methode, die bei Parameter-Änderungen aufgerufen wird.
        protected override void OnParametersSet()
        {
            // Setzt den 'submitting'-Status zurück, wenn das Modal ausgeblendet wird.
            // Dies verhindert, dass der Button bei erneutem Öffnen fälschlicherweise deaktiviert ist.
            if (!IsVisible)
            {
                _isSubmitting = false;
            }
        }

        // Behandelt den Klick auf den "Spiel erstellen"-Button.
        private async Task HandleCreateGame()
        {
            _isSubmitting = true;
            
            // Einfache Validierung, ob ein Spielername eingegeben wurde.
            if (string.IsNullOrWhiteSpace(PlayerName))
            {
                ModalErrorMessage = "Bitte gib einen Spielernamen ein.";
                _isSubmitting = false;
                return;
            }

            ModalErrorMessage = ""; // Setzt die Fehlermeldung zurück, falls vorhanden.

            // Erstellt direkt das DTO und löst das Event aus.
            await OnCreateGame.InvokeAsync(new CreateGameDto
            {
                PlayerName = PlayerName,
                Color = SelectedColor,
                InitialMinutes = InitialTimeMinutes,
                OpponentType = SelectedOpponentType,
                ComputerDifficulty = SelectedComputerDifficulty
            });
        }

        // Schliesst das Modal und setzt alle Eingabefelder auf ihren Standardwert zurück.
        private async Task CloseModal()
        {
            PlayerName = "";
            SelectedColor = Player.White;
            InitialTimeMinutes = 15;
            ModalErrorMessage = "";
            SelectedOpponentType = OpponentType.Human;
            SelectedComputerDifficulty = ComputerDifficulty.Easy;

            _isSubmitting = false;

            // Löst das OnClose-Event aus, um die übergeordnete Komponente zu benachrichtigen.
            await OnClose.InvokeAsync();
        }
    }
}