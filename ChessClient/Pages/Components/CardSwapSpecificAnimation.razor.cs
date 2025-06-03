using ChessNetwork.DTOs;
using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace ChessClient.Pages.Components
{
    public partial class CardSwapSpecificAnimation : ComponentBase, IDisposable
    {
        [Parameter] public bool IsVisible { get; set; }
        [Parameter] public CardDto? CardGiven { get; set; }
        [Parameter] public CardDto? CardReceived { get; set; }
        [Parameter] public EventCallback OnSwapAnimationFinished { get; set; }

        private string AnimationStepClass { get; set; } = "";
        private string GivenCardAnimationClass { get; set; } = "";
        private string ReceivedCardAnimationClass { get; set; } = "";

        private bool IsGivenCardFlipped { get; set; }
        private bool IsReceivedCardFlipped { get; set; }

        private CancellationTokenSource? _animationCts;
        private bool _processingAnimation;

        protected override async Task OnParametersSetAsync()
        {
            if (IsVisible && CardGiven != null && CardReceived != null)
            {
                if (!_processingAnimation)
                {
                    _processingAnimation = true;

                    // Phase 0: Setze explizit die Startzustände für das Auf-/Zudecken
                    IsGivenCardFlipped = false; // Eigene Karte (gegeben) startet AUFGEDECKT
                    IsReceivedCardFlipped = true;  // Gegnerkarte (erhalten) startet ZUGEDECKT (Rückseite sichtbar)

                    AnimationStepClass = "";
                    GivenCardAnimationClass = "";
                    ReceivedCardAnimationClass = "";

                    // Wichtig: StateHasChanged hier, damit die initialen Flip-Zustände im DOM sind,
                    // bevor die CSS-Animationen für das Einfliegen starten.
                    StateHasChanged();

                    await StartAnimationSequenceAsync();
                }
            }
            else if (!IsVisible && _processingAnimation)
            {
                CleanUpAnimation();
            }
        }

        private async Task StartAnimationSequenceAsync()
        {
            _animationCts = new CancellationTokenSource();
            var token = _animationCts.Token;

            try
            {
                // Verzögerung, um sicherzustellen, dass initiale Zustände gerendert wurden
                await Task.Delay(50, token); // Kurze Verzögerung
                if (token.IsCancellationRequested) return;

                // Phase 1: Karten fliegen ein (Eigene von unten aufgedeckt, Gegner von oben zugedeckt)
                AnimationStepClass = "step-fly-in";
                GivenCardAnimationClass = "fly-in-from-bottom-to-left-center";
                ReceivedCardAnimationClass = "fly-in-from-top-to-right-center";
                StateHasChanged();
                await Task.Delay(1000, token); // Dauer für das Einfliegen
                if (token.IsCancellationRequested) return;

                // Phase 1.5: Kurze Pause in der Mitte (Karten sind jetzt leicht versetzt in der Mitte)
                AnimationStepClass = "step-pause-center";
                // Die Klassen von oben halten die Position, oder wir setzen spezifische "in-center-offset" Klassen
                GivenCardAnimationClass = "in-center-left";
                ReceivedCardAnimationClass = "in-center-right";
                StateHasChanged();
                await Task.Delay(500, token); // Pause 500ms
                if (token.IsCancellationRequested) return;

                // Phase 2: Karten drehen sich gleichzeitig
                AnimationStepClass = "step-flip";
                IsGivenCardFlipped = true;   // Eigene Karte wird zugedeckt
                IsReceivedCardFlipped = false; // Gegnerkarte wird aufgedeckt
                StateHasChanged();
                await Task.Delay(700, token); // Dauer für die Flip-Animation
                if (token.IsCancellationRequested) return;

                // Phase 3: Karten bewegen sich zu den neuen Besitzern
                AnimationStepClass = "step-fly-out";
                GivenCardAnimationClass = "fly-out-to-opponent-from-left"; // Eigene (zugedeckt) zum Gegner
                ReceivedCardAnimationClass = "fly-out-to-player-from-right"; // Gegner (aufgedeckt) zum Spieler
                StateHasChanged();
                await Task.Delay(1000, token); // Dauer für das Ausfliegen
                if (token.IsCancellationRequested) return;

                await FinishAnimation();
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("CardSwapSpecificAnimation abgebrochen.");
            }
            finally
            {
                if (_processingAnimation)
                {
                    await FinishAnimation();
                }
            }
        }
        private async Task FinishAnimation()
        {
            if (_processingAnimation)
            {
                _processingAnimation = false;
                if (OnSwapAnimationFinished.HasDelegate)
                {
                    await OnSwapAnimationFinished.InvokeAsync();
                }
                CleanUpAnimation(false);
                StateHasChanged();
            }
        }

        private void CleanUpAnimation(bool invokeCallback = true)
        {
            _animationCts?.Cancel();
            _animationCts?.Dispose();
            _animationCts = null;

            if (_processingAnimation)
            {
                _processingAnimation = false;
                AnimationStepClass = ""; GivenCardAnimationClass = ""; ReceivedCardAnimationClass = "";
                if (invokeCallback && OnSwapAnimationFinished.HasDelegate)
                {
                    InvokeAsync(async () => await OnSwapAnimationFinished.InvokeAsync());
                }
            }
        }

        private static void TryCloseAnimation() { /* Overlay Klick, aktuell keine Aktion */ }

        public void Dispose()
        {
            CleanUpAnimation(false);
            GC.SuppressFinalize(this);
        }
    }
}