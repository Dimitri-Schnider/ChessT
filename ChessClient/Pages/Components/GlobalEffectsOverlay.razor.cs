using ChessClient.State;
using Microsoft.AspNetCore.Components;
using System;
using System.Globalization;

namespace ChessClient.Pages.Components
{
    public partial class GlobalEffectsOverlay : ComponentBase, IDisposable
    {
        [Inject]
        private IUiState UiState { get; set; } = default!;

        private readonly Random _random = new();

        protected override void OnInitialized()
        {
            UiState.StateChanged += OnUiStateChanged;
        }

        private void OnUiStateChanged()
        {
            InvokeAsync(StateHasChanged);
        }

        protected string GetConfettiStyle(int index)
        {
            double startX = _random.NextDouble() * 100;
            double delay = _random.NextDouble() * 2;
            double duration = 1.5 + _random.NextDouble() * 2.0;
            double rotation = _random.Next(-360, 360);
            string color = $"hsl({_random.Next(0, 360)}, 90%, 65%)";

            return $"--start-x: {startX.ToString(CultureInfo.InvariantCulture)}vw; " +
                   $"--delay: {delay.ToString(CultureInfo.InvariantCulture)}s; " +
                   $"--duration: {duration.ToString(CultureInfo.InvariantCulture)}s; " +
                   $"--rotation: {rotation.ToString(CultureInfo.InvariantCulture)}deg; " +
                   $"--color: {color};";
        }

        public void Dispose()
        {
            UiState.StateChanged -= OnUiStateChanged;
            GC.SuppressFinalize(this);
        }
    }
}