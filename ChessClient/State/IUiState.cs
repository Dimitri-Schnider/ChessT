using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
namespace ChessClient.State
{
    public interface IUiState
    {
        event Action? StateChanged;
        string ErrorMessage { get; }
        string CurrentInfoMessageForBox { get; }
        bool IsConnecting { get; }
        bool IsCountdownVisible { get; }
        string CountdownMessage { get; }
        bool IsCreatingGame { get; }

        bool InfoBoxShowActionButton { get; }
        string InfoBoxActionButtonText { get; }
        EventCallback InfoBoxOnActionButtonClicked { get; }
        void SetErrorMessage(string message);
        void ClearErrorMessage();
        Task SetCurrentInfoMessageForBoxAsync(string message, bool autoClear = false, int durationMs = 5000, bool showActionButton = false, string actionButtonText = "Abbrechen", EventCallback? onActionButtonClicked = null);
        void ClearCurrentInfoMessageForBox();
        void SetIsConnecting(bool isConnecting);
        void ShowCountdown(string message);
        void HideCountdown();
        void SetIsCreatingGame(bool isCreating);
    }
}