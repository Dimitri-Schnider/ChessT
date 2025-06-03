// File: [SolutionDir]/ChessClient/State/IModalState.cs
using System;
using ChessLogic;
using ChessNetwork.DTOs;
using System.Collections.Generic;
using ChessClient.Models;

namespace ChessClient.State
{
    public interface IModalState
    {
        event Action? StateChanged;
        bool ShowCreateGameModal { get; }
        string PlayerNameForCreateModal { get; }
        Player SelectedColorForCreateModal { get; }
        int SelectedInitialTimeMinutesForCreateModal { get; }
        bool ShowJoinGameModal { get; }
        string PlayerNameForJoinModal { get; }
        string GameIdInputForJoinModal { get; }
        bool ShowInviteLinkModal { get; }
        string InviteLink { get; }

        bool ShowPieceSelectionModal { get; }
        string PieceSelectionModalTitle { get; }
        string PieceSelectionModalPrompt { get; }
        List<PieceSelectionChoiceInfo>? PieceSelectionModalChoices { get; }
        Player PieceSelectionModalPlayerColor { get; }
        bool PieceSelectionModalShowCancelButton { get; }

        bool ShowPawnPromotionModalSpecifically { get; }
        MoveDto? PendingPromotionMove { get; }

        // NEU: Für CardInfoPanel-Modal
        bool ShowCardInfoPanelModal { get; }
        CardDto? CardForInfoPanelModal { get; }
        bool IsCardInInfoPanelModalActivatable { get; }
        bool IsCardInInfoPanelModalPreviewOnly { get; }

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

        // NEU: Methoden für CardInfoPanel-Modal
        void OpenCardInfoPanelModal(CardDto card, bool isActivatable, bool isPreviewOnly);
        void CloseCardInfoPanelModal();
    }
}