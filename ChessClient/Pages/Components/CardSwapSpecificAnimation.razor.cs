using ChessNetwork.DTOs;
using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace ChessClient.Pages.Components
{
    // Steuert die Logik für die mehrstufige Kartentausch-Animation.
    public partial class CardSwapSpecificAnimation : ComponentBase, IDisposable
    {
        [Parameter] public bool IsVisible { get; set; }                         // Parameter zur Steuerung der Sichtbarkeit der Animation.
        [Parameter] public CardDto? CardGiven { get; set; }                     // Die Karte, die der eigene Spieler abgibt.
        [Parameter] public CardDto? CardReceived { get; set; }                  // Die Karte, die der eigene Spieler vom Gegner erhält.
        [Parameter] public EventCallback OnSwapAnimationFinished { get; set; }  // Callback, der nach Abschluss der Animation aufgerufen wird.

        // CSS-Klassen, die dynamisch gesetzt werden, um die Animationsphasen zu steuern.
        private string AnimationStepClass { get; set; } = "";
        private string GivenCardAnimationClass { get; set; } = "";
        private string ReceivedCardAnimationClass { get; set; } = "";

        // Zustände, die steuern, ob die Karten ihre Vorder- oder Rückseite zeigen.
        private bool IsGivenCardFlipped { get; set; }
        private bool IsReceivedCardFlipped { get; set; }

        // Hilfsmittel zum Abbrechen der Animation.
        private CancellationTokenSource? _animationCts;
        private bool _processingAnimation;

        // Wird aufgerufen, wenn die Parameter der Komponente gesetzt oder aktualisiert werden.
        protected override async Task OnParametersSetAsync()
        {
            // Startet die Animation nur, wenn sie sichtbar sein soll und alle Daten vorhanden sind.
            if (IsVisible && CardGiven != null && CardReceived != null)
            {
                if (!_processingAnimation)
                {
                    _processingAnimation = true;

                    // Setzt die Startzustände für die Karten (eigene aufgedeckt, gegnerische verdeckt).
                    IsGivenCardFlipped = false;
                    IsReceivedCardFlipped = true;

                    // Setzt alle CSS-Klassen zurück.
                    AnimationStepClass = "";
                    GivenCardAnimationClass = "";
                    ReceivedCardAnimationClass = "";

                    StateHasChanged(); // Stellt sicher, dass die UI die Startzustände rendert.

                    // Startet die eigentliche Animationssequenz.
                    await StartAnimationSequenceAsync();
                }
            }
            else if (!IsVisible && _processingAnimation)
            {
                // Räumt auf, wenn die Komponente unsichtbar wird.
                CleanUpAnimation();
            }
        }

        // Führt die mehrstufige Animationssequenz mit zeitlichen Verzögerungen aus.
        private async Task StartAnimationSequenceAsync()
        {
            _animationCts = new CancellationTokenSource();
            var token = _animationCts.Token;

            try
            {
                // Kurze Verzögerung, damit die UI die initialen Zustände rendern kann.
                await Task.Delay(50, token);
                if (token.IsCancellationRequested) return;

                // Phase 1: Karten fliegen von den Spielern in die Mitte des Bildschirms.
                AnimationStepClass = "step-fly-in";
                GivenCardAnimationClass = "fly-in-from-bottom-to-left-center";
                ReceivedCardAnimationClass = "fly-in-from-top-to-right-center";
                StateHasChanged();
                await Task.Delay(1000, token);
                if (token.IsCancellationRequested) return;

                // Phase 1.5: Kurze Pause, während die Karten in der Mitte sind.
                AnimationStepClass = "step-pause-center";
                GivenCardAnimationClass = "in-center-left";
                ReceivedCardAnimationClass = "in-center-right";
                StateHasChanged();
                await Task.Delay(500, token);
                if (token.IsCancellationRequested) return;

                // Phase 2: Beide Karten drehen sich gleichzeitig um (flippen).
                AnimationStepClass = "step-flip";
                IsGivenCardFlipped = true;   // Eigene Karte wird zugedeckt.
                IsReceivedCardFlipped = false; // Gegnerische Karte wird aufgedeckt.
                StateHasChanged();
                await Task.Delay(700, token);
                if (token.IsCancellationRequested) return;

                // Phase 3: Die getauschten Karten bewegen sich zu ihren neuen Besitzern.
                AnimationStepClass = "step-fly-out";
                GivenCardAnimationClass = "fly-out-to-opponent-from-left"; // Eigene (jetzt zugedeckt) fliegt nach oben.
                ReceivedCardAnimationClass = "fly-out-to-player-from-right";  // Gegnerische (jetzt aufgedeckt) fliegt nach unten.
                StateHasChanged();
                await Task.Delay(1000, token);
                if (token.IsCancellationRequested) return;

                // Finale Aufräumarbeiten.
                await FinishAnimation();
            }
            catch (TaskCanceledException)
            {
                // Die Animation wurde abgebrochen.
            }
            finally
            {
                if (_processingAnimation)
                {
                    await FinishAnimation();
                }
            }
        }

        // Wird nach Abschluss der Sequenz aufgerufen, um den Callback auszulösen.
        private async Task FinishAnimation()
        {
            if (_processingAnimation)
            {
                _processingAnimation = false;
                if (OnSwapAnimationFinished.HasDelegate)
                {
                    await OnSwapAnimationFinished.InvokeAsync();
                }
                CleanUpAnimation(false); // Räumt auf, ohne den Callback erneut auszulösen.
                StateHasChanged();
            }
        }

        // Setzt alle Animationszustände zurück und bricht laufende Tasks ab.
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

        // Verhindert, dass ein Klick auf das Overlay die Animation schliesst.
        private static void TryCloseAnimation() { }

        // Gibt die Ressourcen frei, wenn die Komponente zerstört wird.
        public void Dispose()
        {
            CleanUpAnimation(false);
            GC.SuppressFinalize(this);
        }
    }
}