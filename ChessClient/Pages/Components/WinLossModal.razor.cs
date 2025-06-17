using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace ChessClient.Pages.Components
{
    // Die Code-Behind-Klasse für das WinLossModal.
    // Sie bestimmt den Titel basierend auf der Nachricht und leitet die Klick-Events an die aufrufende Komponente weiter.
    public partial class WinLossModal : ComponentBase
    {
        [Parameter] public bool IsVisible { get; set; }                             // Steuert die Sichtbarkeit des Modals.
        [Parameter] public string Message { get; set; } = "";                       // Die Endspiel-Nachricht, die vom Server kommt.
        [Parameter] public EventCallback OnNewGameClicked { get; set; }             // Callback für den "Neues Spiel"-Button.
        [Parameter] public EventCallback OnDownloadHistoryClicked { get; set; }     // Callback für den "Spielverlauf herunterladen"-Button.
        [Parameter] public EventCallback OnClose { get; set; }                      // Callback für den "Schliessen"-Button.

        // Eine berechnete Eigenschaft, die prüft, ob die Endspiel-Nachricht das Wort "gewonnen" enthält.
        protected bool IsWin => !string.IsNullOrEmpty(Message) && Message.Contains("gewonnen", StringComparison.OrdinalIgnoreCase);

        // Gibt den passenden Titel ("Sieg!" oder "Niederlage") basierend auf dem Ergebnis von 'IsWin' zurück.
        protected string Title => IsWin ? "Sieg!" : "Niederlage";

        // Leitet den Klick auf "Neues Spiel" an die übergeordnete Komponente weiter.
        private async Task HandleNewGameClicked()
        {
            if (OnNewGameClicked.HasDelegate)
            {
                await OnNewGameClicked.InvokeAsync();
            }
        }

        // Leitet den Klick auf "Spielverlauf herunterladen" weiter.
        private async Task HandleDownloadHistoryClicked()
        {
            if (OnDownloadHistoryClicked.HasDelegate)
            {
                await OnDownloadHistoryClicked.InvokeAsync();
            }
        }

        // Leitet den Klick auf "Schliessen" weiter.
        private async Task HandleCloseClicked()
        {
            if (OnClose.HasDelegate)
            {
                await OnClose.InvokeAsync();
            }
        }
    }
}