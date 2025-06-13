using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace ChessClient.State
{
    public class UiState : IUiState
    {
        public event Action? StateChanged;
        protected virtual void OnStateChanged() => StateChanged?.Invoke();

        public string CurrentInfoMessageForBox { get; private set; } = "";
        public bool IsConnecting { get; private set; }
        public bool IsCountdownVisible { get; private set; }
        public string CountdownMessage { get; private set; } = "";
        public bool IsCreatingGame { get; private set; }
        public bool InfoBoxShowActionButton { get; private set; }
        public string InfoBoxActionButtonText { get; private set; } = "Abbrechen";
        public EventCallback InfoBoxOnActionButtonClicked { get; private set; }

        public bool ShowWinAnimation { get; private set; }
        public bool ShowLossAnimation { get; private set; }

        public UiState() { }

        public void TriggerWinAnimation()
        {
            ShowWinAnimation = true;
            OnStateChanged();
        }

        public void TriggerLossAnimation()
        {
            ShowLossAnimation = true;
            OnStateChanged();
        }

        public void HideEndGameAnimations()
        {
            ShowWinAnimation = false;
            ShowLossAnimation = false;
            OnStateChanged();
        }

        public Task SetCurrentInfoMessageForBoxAsync(string message, bool autoClear = false, int durationMs = 5000, bool showActionButton = false, string actionButtonText = "Abbrechen", EventCallback? onActionButtonClicked = null)
        {
            CurrentInfoMessageForBox = message;
            InfoBoxShowActionButton = showActionButton;
            InfoBoxActionButtonText = actionButtonText;
            InfoBoxOnActionButtonClicked = onActionButtonClicked ?? new EventCallback();
            OnStateChanged();
            if (string.IsNullOrEmpty(message) && !showActionButton)
            {
                ClearCurrentInfoMessageForBox();
            }
            return Task.CompletedTask;
        }

        public void ClearCurrentInfoMessageForBox()
        {
            CurrentInfoMessageForBox = "";
            InfoBoxShowActionButton = false;
            InfoBoxActionButtonText = "Abbrechen";
            InfoBoxOnActionButtonClicked = new EventCallback();
            OnStateChanged();
        }

        public void SetIsConnecting(bool isConnecting)
        {
            IsConnecting = isConnecting;
            OnStateChanged();
        }

        public void ShowCountdown(string message)
        {
            CountdownMessage = message;
            if (!IsCountdownVisible)
            {
                IsCountdownVisible = true;
            }
            OnStateChanged();
        }

        public void HideCountdown()
        {
            if (IsCountdownVisible)
            {
                IsCountdownVisible = false;
                CountdownMessage = "";
                OnStateChanged();
            }
        }
        public void SetIsCreatingGame(bool isCreating)
        {
            if (IsCreatingGame == isCreating) return;
            IsCreatingGame = isCreating;
            OnStateChanged();
        }
    }
}