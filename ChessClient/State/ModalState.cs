// File: [SolutionDir]\ChessClient\State\ModalState.cs
using System;
using System.Threading.Tasks;
using ChessLogic;
using ChessNetwork.DTOs;
using System.Collections.Generic;
using System.Linq;
using ChessClient.Models;

namespace ChessClient.State
{
    public class ModalState : IModalState
    {
        public event Action? StateChanged;
        protected virtual void OnStateChanged() => StateChanged?.Invoke();

        public bool ShowCreateGameModal { get; private set; }
        public string PlayerNameForCreateModal { get; private set; } = "";
        public Player SelectedColorForCreateModal { get; private set; } = Player.White;
        public int SelectedInitialTimeMinutesForCreateModal { get; private set; } = 15;
        public bool ShowJoinGameModal { get; private set; }
        public string PlayerNameForJoinModal { get; private set; } = "";
        public string GameIdInputForJoinModal { get; private set; } = "";

        public bool ShowInviteLinkModal { get; private set; }
        public string InviteLink { get; private set; } = "";
        public bool ShowPieceSelectionModal { get; private set; }
        public string PieceSelectionModalTitle { get; private set; } = "";
        public string PieceSelectionModalPrompt { get; private set; } = "";
        public List<PieceSelectionChoiceInfo>? PieceSelectionModalChoices { get; private set; }
        public Player PieceSelectionModalPlayerColor { get; private set; }
        public bool PieceSelectionModalShowCancelButton { get; private set; } = true;
        public bool ShowPawnPromotionModalSpecifically { get; private set; }
        public MoveDto? PendingPromotionMove { get; private set; }

        public bool ShowCardInfoPanelModal { get; private set; }
        public CardDto? CardForInfoPanelModal { get; private set; }
        public bool IsCardInInfoPanelModalActivatable { get; private set; }
        public bool IsCardInInfoPanelModalPreviewOnly { get; private set; }
        public bool ShowErrorModal { get; private set; }
        public string ErrorModalMessage { get; private set; } = "";


        public ModalState()
        {
        }

        public void OpenCreateGameModal()
        {
            CloseAllModals();
            ShowCreateGameModal = true;
            PlayerNameForCreateModal = "";
            SelectedColorForCreateModal = Player.White;
            SelectedInitialTimeMinutesForCreateModal = 15;
            OnStateChanged();
        }

        public void CloseCreateGameModal()
        {
            ShowCreateGameModal = false;
            OnStateChanged();
        }

        public void UpdateCreateGameModalArgs(string name, Player color, int timeMinutes)
        {
            PlayerNameForCreateModal = name;
            SelectedColorForCreateModal = color;
            SelectedInitialTimeMinutesForCreateModal = timeMinutes;
        }

        public void OpenJoinGameModal(string? initialGameId = null)
        {
            CloseAllModals();
            ShowJoinGameModal = true;
            PlayerNameForJoinModal = "";
            GameIdInputForJoinModal = initialGameId ?? "";
            OnStateChanged();
        }

        public void CloseJoinGameModal()
        {
            ShowJoinGameModal = false;
            OnStateChanged();
        }
        public void UpdateJoinGameModalArgs(string name, string gameId)
        {
            PlayerNameForJoinModal = name;
            GameIdInputForJoinModal = gameId;
        }

        public void OpenInviteLinkModal(string inviteLink)
        {
            InviteLink = inviteLink;
            ShowInviteLinkModal = true;
            OnStateChanged();
        }

        public void CloseInviteLinkModal()
        {
            ShowInviteLinkModal = false;
            OnStateChanged();
        }

        public void OpenPieceSelectionModal(string title, string prompt, List<PieceSelectionChoiceInfo> choices, Player playerColor, bool showCancelButton = true)
        {
            CloseAllModals();
            PieceSelectionModalTitle = title;
            PieceSelectionModalPrompt = prompt;
            PieceSelectionModalChoices = choices;
            PieceSelectionModalPlayerColor = playerColor;
            PieceSelectionModalShowCancelButton = showCancelButton;
            ShowPieceSelectionModal = true;
            OnStateChanged();
        }

        public void ClosePieceSelectionModal()
        {
            ShowPieceSelectionModal = false;
            PieceSelectionModalChoices = null;
            OnStateChanged();
        }

        public void OpenPawnPromotionModal(MoveDto pendingMove, Player myColor)
        {
            PendingPromotionMove = pendingMove;

            var promotionChoices = new List<PieceSelectionChoiceInfo>
            {
                new PieceSelectionChoiceInfo(PieceType.Queen, true, "Dame wählen"),
                new PieceSelectionChoiceInfo(PieceType.Rook, true, "Turm wählen"),
                new PieceSelectionChoiceInfo(PieceType.Bishop, true, "Läufer wählen"),
                new PieceSelectionChoiceInfo(PieceType.Knight, true, "Springer wählen")
            };

            OpenPieceSelectionModal(
                title: "Figur für Bauernumwandlung wählen",
                prompt: "Dein Bauer hat die gegnerische Grundlinie erreicht! Wähle eine Figur für die Umwandlung:",
                choices: promotionChoices,
                playerColor: myColor,
                showCancelButton: false
            );

            ShowPawnPromotionModalSpecifically = true;
            OnStateChanged();
        }

        public void ClosePawnPromotionModal()
        {
            ShowPawnPromotionModalSpecifically = false;
            ClosePieceSelectionModal();
        }

        public void ClearPendingPromotionMove()
        {
            PendingPromotionMove = null;
        }

        public void OpenCardInfoPanelModal(CardDto card, bool isActivatable, bool isPreviewOnly)
        {
            CloseAllModals();
            CardForInfoPanelModal = card;
            IsCardInInfoPanelModalActivatable = isActivatable;
            IsCardInInfoPanelModalPreviewOnly = isPreviewOnly;
            ShowCardInfoPanelModal = true;
            OnStateChanged();
        }

        public void CloseCardInfoPanelModal()
        {
            ShowCardInfoPanelModal = false;
            CardForInfoPanelModal = null;
            OnStateChanged();
        }

        // NEU: Methoden zur Steuerung des Fehler-Modals
        public void OpenErrorModal(string message)
        {
            CloseAllModals(); // Stellt sicher, dass kein anderes Modal offen ist
            ErrorModalMessage = message;
            ShowErrorModal = true;
            OnStateChanged();
        }

        public void CloseErrorModal()
        {
            if (ShowErrorModal)
            {
                ShowErrorModal = false;
                OnStateChanged();
            }
        }

        private void CloseAllModals()
        {
            ShowCreateGameModal = false;
            ShowJoinGameModal = false;
            ShowPieceSelectionModal = false;
            ShowPawnPromotionModalSpecifically = false;
            ShowCardInfoPanelModal = false;
            ShowErrorModal = false; // Schliesst auch das Error-Modal, falls ein neues Modal geöffnet wird
        }
    }
}