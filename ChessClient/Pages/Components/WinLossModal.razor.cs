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

        protected bool IsWin => Message.Contains("gewonnen", StringComparison.OrdinalIgnoreCase);
        protected string Title => IsWin ? "Sieg!" : "Niederlage";

        private readonly Random _random = new();

        protected string GetConfettiStyle(int index)
        {
            double startX = _random.NextDouble() * 100;
            double delay = _random.NextDouble() * 4;
            double duration = 2.5 + _random.NextDouble() * 2.5;
            double rotation = _random.Next(-360, 360);
            string color = $"hsl({_random.Next(0, 360)}, 90%, 65%)";

            return $"--start-x: {startX}vw; " +
                   $"--delay: {delay}s; " +
                   $"--duration: {duration}s; " +
                   $"--rotation: {rotation}deg; " +
                   $"--color: {color};";
        }

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