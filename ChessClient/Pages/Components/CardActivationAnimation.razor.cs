using ChessNetwork.DTOs;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using ChessNetwork.Configuration;
using System;

namespace ChessClient.Pages.Components
{
    public partial class CardActivationAnimation : ComponentBase, IDisposable
    {
        [Parameter] public bool IsVisible { get; set; }
        [Parameter] public CardDto? CardToDisplay { get; set; }
        [Parameter] public bool IsOwnCardAnimation { get; set; }
        [Parameter] public EventCallback OnAnimationFinished { get; set; }

        private bool _isFlipped;
        public bool IsFlipped { get => _isFlipped; private set { if (_isFlipped != value) _isFlipped = value; } }

        private bool _glowEffect;
        public bool GlowEffect { get => _glowEffect; private set { if (_glowEffect != value) _glowEffect = value; } }

        private bool _isMovingToHistory;
        public bool IsMovingToHistory { get => _isMovingToHistory; private set { if (_isMovingToHistory != value) _isMovingToHistory = value; } }

        private CancellationTokenSource _animationCts = new CancellationTokenSource();
        private const int ShowBackDurationMs = 1200;
        private const int FlipAnimDurationMs = 400;
        private const int GlowVisibleDurationMs = 1300;
        private const int MoveToHistoryAnimDurationMs = 250;

        private bool _processingAnimation;

        protected override void OnParametersSet()   
        {
            if (IsVisible && CardToDisplay != null)
            {
                if (!_processingAnimation)
                {
                    _isFlipped = !IsOwnCardAnimation;
                    _glowEffect = false;
                    _isMovingToHistory = false;
                    _ = StartAnimationSequenceAsync();
                }
            }
            else if (!IsVisible && _processingAnimation)
            {
                CleanUpAnimation();
            }
            else if (!IsVisible)
            {
                _processingAnimation = false;
            }
        }

        private async Task StartAnimationSequenceAsync()
        {
            await Task.Yield();
            if (!IsVisible || CardToDisplay == null) { _processingAnimation = false; return; }

            _processingAnimation = true;
            _animationCts = new CancellationTokenSource();
            CancellationToken token = _animationCts.Token;

            try
            {
                if (!IsOwnCardAnimation) StateHasChanged();

                if (!IsOwnCardAnimation)
                {
                    await Task.Delay(ShowBackDurationMs, token);
                    if (token.IsCancellationRequested) return;

                    IsFlipped = false;
                    StateHasChanged();
                    await Task.Delay(FlipAnimDurationMs, token);
                    if (token.IsCancellationRequested) return;
                }
                else
                {
                    await Task.Delay(ShowBackDurationMs + FlipAnimDurationMs, token);
                    if (token.IsCancellationRequested) return;
                }

                GlowEffect = true;
                StateHasChanged();
                await Task.Delay(GlowVisibleDurationMs, token);
                if (token.IsCancellationRequested) return;

                GlowEffect = false;
                IsMovingToHistory = true;
                StateHasChanged();
                await Task.Delay(MoveToHistoryAnimDurationMs, token);
                if (token.IsCancellationRequested) return;
            }
            catch (TaskCanceledException)
            {
                // Animation wurde abgebrochen
            }
            finally
            {
                if (OnAnimationFinished.HasDelegate)
                {
                    await InvokeAsync(OnAnimationFinished.InvokeAsync);
                }
                CleanUpAnimation();
            }
        }

        private void CleanUpAnimation()
        {
            if (_animationCts != null && !_animationCts.IsCancellationRequested)
            {
                _animationCts.Cancel();
            }
            _animationCts?.Dispose();
            _processingAnimation = false;
            _isFlipped = false;
            _glowEffect = false;
            _isMovingToHistory = false;
        }

        private static void TryCloseAnimation() { }

        public void Dispose()
        {
            CleanUpAnimation();
            GC.SuppressFinalize(this);
        }
    }
}