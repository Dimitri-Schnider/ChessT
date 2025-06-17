using System;
using System.Threading.Tasks;
using ChessLogic;
using ChessNetwork.DTOs;
using System.Collections.Generic;
using System.Linq;
using ChessClient.Models;

namespace ChessClient.State
{
    // Verwaltet den Zustand aller Modal-Dialoge in der Anwendung.
    // Stellt sicher, dass immer nur ein Modal zur gleichen Zeit aktiv sein kann.
    public class ModalState : IModalState
    {
        public event Action? StateChanged;
        protected virtual void OnStateChanged() => StateChanged?.Invoke();

        // --- CreateGameModal ---
        public bool ShowCreateGameModal { get; private set; }
        public string PlayerNameForCreateModal { get; private set; } = "";
        public Player SelectedColorForCreateModal { get; private set; } = Player.White;
        public int SelectedInitialTimeMinutesForCreateModal { get; private set; } = 15;

        // --- JoinGameModal ---
        public bool ShowJoinGameModal { get; private set; }
        public string PlayerNameForJoinModal { get; private set; } = "";
        public string GameIdInputForJoinModal { get; private set; } = "";

        // --- InviteLinkModal ---
        public bool ShowInviteLinkModal { get; private set; }
        public string InviteLink { get; private set; } = "";

        // --- PieceSelectionModal (für Bauernumwandlung & Wiedergeburt) ---
        public bool ShowPieceSelectionModal { get; private set; }
        public string PieceSelectionModalTitle { get; private set; } = "";
        public string PieceSelectionModalPrompt { get; private set; } = "";
        public List<PieceSelectionChoiceInfo>? PieceSelectionModalChoices { get; private set; }
        public Player PieceSelectionModalPlayerColor { get; private set; }
        public bool PieceSelectionModalShowCancelButton { get; private set; } = true;

        // --- Spezifische Zustände für Modals ---
        public bool ShowPawnPromotionModalSpecifically { get; private set; }
        public MoveDto? PendingPromotionMove { get; private set; }

        // --- CardInfoPanel-Modal ---
        public bool ShowCardInfoPanelModal { get; private set; }
        public CardDto? CardForInfoPanelModal { get; private set; }
        public bool IsCardInInfoPanelModalActivatable { get; private set; }
        public bool IsCardInInfoPanelModalPreviewOnly { get; private set; }

        // --- ErrorModal ---
        public bool ShowErrorModal { get; private set; }
        public string ErrorModalMessage { get; private set; } = "";

        public ModalState() { }

        // Öffnet das "Spiel erstellen"-Modal und setzt die Standardwerte.
        public void OpenCreateGameModal()
        {
            CloseAllModals(); // Stellt sicher, dass alle anderen Modals geschlossen sind.
            ShowCreateGameModal = true;
            OnStateChanged();
        }

        public void CloseCreateGameModal()
        {
            ShowCreateGameModal = false;
            OnStateChanged();
        }

        // Aktualisiert die Argumente des CreateGame-Modals.
        public void UpdateCreateGameModalArgs(string name, Player color, int timeMinutes)
        {
            PlayerNameForCreateModal = name;
            SelectedColorForCreateModal = color;
            SelectedInitialTimeMinutesForCreateModal = timeMinutes;
        }

        // Öffnet das "Spiel beitreten"-Modal, optional mit einer vorausgefüllten Spiel-ID.
        public void OpenJoinGameModal(string? initialGameId = null)
        {
            CloseAllModals();
            ShowJoinGameModal = true;
            GameIdInputForJoinModal = initialGameId ?? "";
            OnStateChanged();
        }

        public void CloseJoinGameModal()
        {
            ShowJoinGameModal = false;
            OnStateChanged();
        }

        // Aktualisiert die Argumente des JoinGame-Modals.
        public void UpdateJoinGameModalArgs(string name, string gameId)
        {
            PlayerNameForJoinModal = name;
            GameIdInputForJoinModal = gameId;
        }

        // Öffnet das Modal mit dem Einladungslink.
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

        // Öffnet das universelle Figurenauswahl-Modal.
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
            PieceSelectionModalChoices = null; // Gibt Speicher frei.
            OnStateChanged();
        }

        // Öffnet das Figurenauswahl-Modal speziell für die Bauernumwandlung.
        public void OpenPawnPromotionModal(MoveDto pendingMove, Player myColor)
        {
            PendingPromotionMove = pendingMove;
            var promotionChoices = new List<PieceSelectionChoiceInfo>
            {
                new(PieceType.Queen, true), new(PieceType.Rook, true), new(PieceType.Bishop, true), new(PieceType.Knight, true)
            };
            OpenPieceSelectionModal("Figur umwandeln", "Wähle eine Figur:", promotionChoices, myColor, false);
            ShowPawnPromotionModalSpecifically = true; // Setzt ein Flag für diese spezifische Nutzung.
            OnStateChanged();
        }

        // Schliesst das Bauernumwandlungs-Modal.
        public void ClosePawnPromotionModal()
        {
            ShowPawnPromotionModalSpecifically = false;
            ClosePieceSelectionModal();
        }

        // Löscht den zwischengespeicherten Umwandlungszug.
        public void ClearPendingPromotionMove()
        {
            PendingPromotionMove = null;
        }

        // Öffnet das Info-Panel für eine Karte.
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

        // Öffnet das Fehler-Modal mit einer Nachricht.
        public void OpenErrorModal(string message, bool closeOtherModals = true)
        {
            if (closeOtherModals)
            {
                CloseAllModals();
            }
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

        // Private Hilfsmethode, um sicherzustellen, dass immer nur ein Modal offen ist.
        private void CloseAllModals()
        {
            ShowCreateGameModal = false;
            ShowJoinGameModal = false;
            ShowPieceSelectionModal = false;
            ShowPawnPromotionModalSpecifically = false;
            ShowCardInfoPanelModal = false;
            ShowErrorModal = false;
        }
    }
}