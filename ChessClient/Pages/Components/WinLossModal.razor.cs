using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace ChessClient.Pages.Components
{
    public partial class WinLossModal : ComponentBase
    {
        [Parameter] public bool IsVisible { get; set; }
        [Parameter] public string Message { get; set; } = "";
        [Parameter] public EventCallback OnNewGameClicked { get; set; }
        [Parameter] public EventCallback OnDownloadHistoryClicked { get; set; }

        protected bool IsWin => !string.IsNullOrEmpty(Message) && Message.Contains("gewonnen", StringComparison.OrdinalIgnoreCase);
        protected string Title => IsWin ? "Sieg!" : "Niederlage";


        private async Task HandleNewGameClicked()
        {
            if (OnNewGameClicked.HasDelegate)
            {
                await OnNewGameClicked.InvokeAsync();
            }
        }

        private async Task HandleDownloadHistoryClicked()
        {
            if (OnDownloadHistoryClicked.HasDelegate)
            {
                await OnDownloadHistoryClicked.InvokeAsync();
            }
        }
    }
}