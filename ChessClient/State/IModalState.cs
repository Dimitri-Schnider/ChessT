using System;
using ChessLogic;
using ChessNetwork.DTOs;
using System.Collections.Generic;
using ChessClient.Models;

namespace ChessClient.State
{
    // Definiert den Vertrag für den State-Container, der alle Modal-Dialoge verwaltet.
    public interface IModalState
    {
        // Event zur Benachrichtigung über Zustandsänderungen.
        event Action? StateChanged;

        // --- Properties für die verschiedenen Modals ---
        bool ShowCreateGameModal { get; }
        bool ShowJoinGameModal { get; }
        bool ShowInviteLinkModal { get; }
        bool ShowPieceSelectionModal { get; }
        bool ShowPawnPromotionModalSpecifically { get; }
        bool ShowCardInfoPanelModal { get; }
        bool ShowErrorModal { get; }

        // --- Daten, die von den Modals benötigt werden ---
        string PlayerNameForCreateModal { get; }
        Player SelectedColorForCreateModal { get; }
        int SelectedInitialTimeMinutesForCreateModal { get; }
        string PlayerNameForJoinModal { get; }
        string GameIdInputForJoinModal { get; }
        string InviteLink { get; }
        string PieceSelectionModalTitle { get; }
        string PieceSelectionModalPrompt { get; }
        List<PieceSelectionChoiceInfo>? PieceSelectionModalChoices { get; }
        Player PieceSelectionModalPlayerColor { get; }
        bool PieceSelectionModalShowCancelButton { get; }
        MoveDto? PendingPromotionMove { get; }
        CardDto? CardForInfoPanelModal { get; }
        bool IsCardInInfoPanelModalActivatable { get; }
        bool IsCardInInfoPanelModalPreviewOnly { get; }
        string ErrorModalMessage { get; }

        // --- Methoden zur Steuerung der Modals ---
        void OpenErrorModal(string message, bool closeOtherModals = true);
        void CloseErrorModal();
        void OpenCreateGameModal();
        void CloseCreateGameModal();
        void UpdateCreateGameModalArgs(string name, Player color, int timeMinutes);
        void OpenJoinGameModal(string? initialGameId = null);
        void CloseJoinGameModal();
        void UpdateJoinGameModalArgs(string name, string gameId);
        void OpenInviteLinkModal(string inviteLink);
        void CloseInviteLinkModal();
        void OpenPieceSelectionModal(string title, string prompt, List<PieceSelectionChoiceInfo> choices, Player playerColor, bool showCancelButton = true);
        void ClosePieceSelectionModal();
        void OpenPawnPromotionModal(MoveDto pendingMove, Player myColor);
        void ClosePawnPromotionModal();
        void ClearPendingPromotionMove();
        void OpenCardInfoPanelModal(CardDto card, bool isActivatable, bool isPreviewOnly);
        void CloseCardInfoPanelModal();
    }
}