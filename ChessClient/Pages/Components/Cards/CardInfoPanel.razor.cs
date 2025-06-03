// File: [SolutionDir]/ChessClient/Pages/Components/Cards/CardInfoPanel.razor.cs
using ChessNetwork.DTOs;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace ChessClient.Pages.Components.Cards
{
    public partial class CardInfoPanel : ComponentBase
    {
        [Parameter] public bool IsVisible { get; set; } // Wird vom ModalState gesteuert
        [Parameter] public CardDto? CardToDisplay { get; set; } // Die anzuzeigende Karte
        [Parameter] public bool IsActivatable { get; set; } // Ist die Karte aktivierbar?
        [Parameter] public bool IsPreviewOnly { get; set; } // Ist es nur eine Vorschau?

        [Parameter] public EventCallback<CardDto> OnActivateCard { get; set; }
        [Parameter] public EventCallback OnCancelCardSelectionOrCloseModal { get; set; } // Umbenannt für Klarheit

        private async Task HandleActivateClick()
        {
            if (CardToDisplay != null && IsActivatable && !IsPreviewOnly)
            {
                await OnActivateCard.InvokeAsync(CardToDisplay);
                // Das Modal wird typischerweise vom Aufrufer (Chess.razor) geschlossen,
                // nachdem die Aktivierungslogik durchgelaufen ist.
            }
        }

        private async Task HandleCancelClick()
        {
            await OnCancelCardSelectionOrCloseModal.InvokeAsync();
        }
    }
}