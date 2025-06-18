using ChessNetwork.DTOs;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using ChessNetwork.Configuration;
using System;

namespace ChessClient.Pages.Components.Animations
{
    // Steuert die Vollbild-Animation, die beim Aktivieren einer Karte abgespielt wird.
    public partial class CardActivationAnimation : ComponentBase, IDisposable
    {
        [Parameter] public bool IsVisible { get; set; }                     // Parameter zur Steuerung der Sichtbarkeit.
        [Parameter] public CardDto? CardToDisplay { get; set; }             // Die Karte, die animiert werden soll.
        [Parameter] public bool IsOwnCardAnimation { get; set; }            // Gibt an, ob es die eigene Karte ist (bestimmt die Start-Flip-Richtung).
        [Parameter] public EventCallback OnAnimationFinished { get; set; }  // Callback, der ausgelöst wird, wenn die Animation abgeschlossen ist.

        // Internes Feld für den Flip-Zustand.
        private bool _isFlipped;
        // Öffentliche Property, um den Flip-Zustand zu lesen und kontrolliert zu setzen.
        public bool IsFlipped { get => _isFlipped; private set { if (_isFlipped != value) _isFlipped = value; } }

        // Internes Feld für den Leuchteffekt.
        private bool _glowEffect;
        // Öffentliche Property, um den Leuchteffekt zu lesen und kontrolliert zu setzen.
        public bool GlowEffect { get => _glowEffect; private set { if (_glowEffect != value) _glowEffect = value; } }

        // Internes Feld für die "Wegfliegen"-Animation.
        private bool _isMovingToHistory;
        // Öffentliche Property für die "Wegfliegen"-Animation.
        public bool IsMovingToHistory { get => _isMovingToHistory; private set { if (_isMovingToHistory != value) _isMovingToHistory = value; } }

        // Hilfsmittel, um die asynchrone Animation bei Bedarf abbrechen zu können.
        private CancellationTokenSource _animationCts = new CancellationTokenSource();

        // Flag, das anzeigt, ob die Animation gerade verarbeitet wird.
        private bool _processingAnimation;

        // Definiert die Dauer der einzelnen Animationsphasen in Millisekunden.
        private const int ShowBackDurationMs = 1200;
        private const int FlipAnimDurationMs = 400;
        private const int GlowVisibleDurationMs = 1300;
        private const int MoveToHistoryAnimDurationMs = 250;

        // Lifecycle-Methode, die auf Parameter-Änderungen reagiert.
        protected override void OnParametersSet()
        {
            // Startet die Animation, wenn die Komponente sichtbar wird.
            if (IsVisible && CardToDisplay != null)
            {
                if (!_processingAnimation)
                {
                    // Setzt die initialen Zustände für die Animation.
                    _isFlipped = !IsOwnCardAnimation; // Gegnerische Karte startet umgedreht.
                    _glowEffect = false;
                    _isMovingToHistory = false;
                    // Startet die asynchrone Animationssequenz.
                    _ = StartAnimationSequenceAsync();
                }
            }
            // Bricht die Animation ab, wenn die Komponente unsichtbar wird.
            else if (!IsVisible && _processingAnimation)
            {
                CleanUpAnimation();
            }
            else if (!IsVisible)
            {
                _processingAnimation = false;
            }
        }

        // Führt die mehrstufige Animationssequenz aus.
        private async Task StartAnimationSequenceAsync()
        {
            await Task.Yield(); // Gibt der UI kurz Zeit zum Rendern.
            if (!IsVisible || CardToDisplay == null) { _processingAnimation = false; return; }

            _processingAnimation = true;
            _animationCts = new CancellationTokenSource();
            CancellationToken token = _animationCts.Token;

            try
            {
                // Phase 1: Gegnerische Karte wird aufgedeckt.
                if (!IsOwnCardAnimation) StateHasChanged();
                if (!IsOwnCardAnimation)
                {
                    await Task.Delay(ShowBackDurationMs, token);
                    if (token.IsCancellationRequested) return;

                    IsFlipped = false; // Dreht die Karte auf die Vorderseite.
                    StateHasChanged();
                    await Task.Delay(FlipAnimDurationMs, token);
                    if (token.IsCancellationRequested) return;
                }
                else
                {
                    // Bei eigener Karte wird die Aufdeck-Phase übersprungen, aber die Zeit gewartet.
                    await Task.Delay(ShowBackDurationMs + FlipAnimDurationMs, token);
                    if (token.IsCancellationRequested) return;
                }

                // Phase 2: Die Karte leuchtet auf.
                GlowEffect = true;
                StateHasChanged();
                await Task.Delay(GlowVisibleDurationMs, token);
                if (token.IsCancellationRequested) return;

                // Phase 3: Die Karte fliegt aus dem Bild (in Richtung Ablagestapel).
                GlowEffect = false;
                IsMovingToHistory = true;
                StateHasChanged();
                await Task.Delay(MoveToHistoryAnimDurationMs, token);
                if (token.IsCancellationRequested) return;
            }
            catch (TaskCanceledException)
            {
                // Die Animation wurde vorzeitig abgebrochen.
            }
            finally
            {
                // Ruft den Callback auf, um die aufrufende Komponente zu benachrichtigen.
                if (OnAnimationFinished.HasDelegate)
                {
                    await InvokeAsync(OnAnimationFinished.InvokeAsync);
                }
                CleanUpAnimation();
            }
        }

        // Setzt alle Animationszustände zurück und bricht laufende Tasks ab.
        private void CleanUpAnimation()
        {
            if (_animationCts != null && !_animationCts.IsCancellationRequested)
            {
                _animationCts.Cancel();
            }
            _animationCts?.Dispose();
            _processingAnimation = false;
            // Setzt die internen Felder zurück.
            _isFlipped = false;
            _glowEffect = false;
            _isMovingToHistory = false;
        }

        // Leere Methode, um Klicks auf das Overlay abzufangen, ohne etwas zu tun.
        private static void TryCloseAnimation() { }

        // Gibt die Ressourcen frei, wenn die Komponente zerstört wird.
        public void Dispose()
        {
            CleanUpAnimation();
            GC.SuppressFinalize(this);
        }
    }
}