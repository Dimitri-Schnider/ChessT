using ChessNetwork.DTOs;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace ChessClient.Pages.Components.Cards
{
    // Sie empfängt die Kartendaten und leitet Benutzeraktionen (Aktivieren, Abbrechen)
    // an die aufrufende Komponente (Chess.razor) weiter.
    public partial class CardInfoPanel : ComponentBase
    {
        [Parameter] public bool IsVisible { get; set; }                                     // Steuert die Sichtbarkeit des Modals.
        [Parameter] public CardDto? CardToDisplay { get; set; }                             // Das Datenobjekt der anzuzeigenden Karte.
        [Parameter] public bool IsActivatable { get; set; }                                 // Gibt an, ob die Karte momentan aktiviert werden kann.
        [Parameter] public bool IsPreviewOnly { get; set; }                                 // Schaltet das Modal in einen reinen Vorschau-Modus.
        [Parameter] public EventCallback<CardDto> OnActivateCard { get; set; }              // Callback, der ausgelöst wird, wenn der "Aktivieren"-Button geklickt wird.
        [Parameter] public EventCallback OnCancelCardSelectionOrCloseModal { get; set; }    // Callback für "Abbrechen" oder "Schliessen".

        // Behandelt den Klick auf "Aktivieren".
        private async Task HandleActivateClick()
        {
            // Stellt sicher, dass die Aktion nur unter den korrekten Bedingungen ausgeführt wird.
            if (CardToDisplay != null && IsActivatable && !IsPreviewOnly)
            {
                // Löst das Event aus und übergibt die zu aktivierende Karte.
                // Die eigentliche Logik der Kartenaktivierung findet in der übergeordneten Komponente statt.
                await OnActivateCard.InvokeAsync(CardToDisplay);
            }
        }

        // Behandelt den Klick auf "Abbrechen" oder "Schliessen".
        private async Task HandleCancelClick()
        {
            // Löst das Event aus, um das Modal zu schliessen und ggf. den Auswahl-Zustand zurückzusetzen.
            await OnCancelCardSelectionOrCloseModal.InvokeAsync();
        }
    }
}