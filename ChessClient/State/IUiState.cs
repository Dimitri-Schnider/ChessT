using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace ChessClient.State
{
    // Definiert den Vertrag für den State-Container, der allgemeine UI-Zustände wie Info-Boxen und Animationen verwaltet.
    public interface IUiState
    {
        event Action? StateChanged;

        // --- Properties für die Info-Box ---
        string CurrentInfoMessageForBox { get; }
        bool InfoBoxShowActionButton { get; }
        string InfoBoxActionButtonText { get; }
        EventCallback InfoBoxOnActionButtonClicked { get; }

        // --- Properties für Verbindungs- und Ladezustände ---
        bool IsConnecting { get; }
        bool IsCountdownVisible { get; }
        string CountdownMessage { get; }
        bool IsCreatingGame { get; }

        // --- Properties für globale Animationen ---
        bool ShowWinAnimation { get; }
        bool ShowLossAnimation { get; }

        // --- Methoden zur Steuerung des UI-Zustands ---
        void TriggerWinAnimation();
        void TriggerLossAnimation();
        void HideEndGameAnimations();
        Task SetCurrentInfoMessageForBoxAsync(string message, bool autoClear = false, int durationMs = 5000, bool showActionButton = false, string actionButtonText = "Abbrechen", EventCallback? onActionButtonClicked = null);
        void ClearCurrentInfoMessageForBox();
        void SetIsConnecting(bool isConnecting);
        void ShowCountdown(string message);
        void HideCountdown();
        void SetIsCreatingGame(bool isCreating);
    }
}