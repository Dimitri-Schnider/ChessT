// File: [SolutionDir]/ChessClient/State/ModalState.cs
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

        // NEU: Eigenschaften für CardInfoPanel-Modal
        public bool ShowCardInfoPanelModal { get; private set; }
        public CardDto? CardForInfoPanelModal { get; private set; }
        public bool IsCardInInfoPanelModalActivatable { get; private set; }
        public bool IsCardInInfoPanelModalPreviewOnly { get; private set; }


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
            // OnStateChanged wird nicht benötigt, da es nur die internen Werte für das bereits offene Modal aktualisiert
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
            // OnStateChanged nicht nötig
        }

        public void OpenInviteLinkModal(string inviteLink)
        {
            // Kann parallel zu anderen Modals angezeigt werden oder andere schliessen?
            // Fürs Erste schliessen wir andere nicht.
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
            CloseAllModals(); // Schliesst andere Hauptmodals
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
            ShowPawnPromotionModalSpecifically = true; // Markiert, dass dies ein Promotion-Modal ist

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
                showCancelButton: false // Bei Promotion meist keine Abbruch-Option
            );
        }

        public void ClosePawnPromotionModal()
        {
            ShowPawnPromotionModalSpecifically = false;
            ClosePieceSelectionModal(); // Die generische Methode schliesst das PieceSelectionModal
        }

        public void ClearPendingPromotionMove()
        {
            PendingPromotionMove = null;
            // OnStateChanged, falls das UI darauf reagieren muss, dass kein Zug mehr ansteht
            // OnStateChanged();
        }

        // NEU: Methoden für CardInfoPanel-Modal
        public void OpenCardInfoPanelModal(CardDto card, bool isActivatable, bool isPreviewOnly)
        {
            CloseAllModals(); // Schliesst andere Hauptmodals
            CardForInfoPanelModal = card;
            IsCardInInfoPanelModalActivatable = isActivatable;
            IsCardInInfoPanelModalPreviewOnly = isPreviewOnly;
            ShowCardInfoPanelModal = true;
            OnStateChanged();
        }

        public void CloseCardInfoPanelModal()
        {
            ShowCardInfoPanelModal = false;
            CardForInfoPanelModal = null; // Karte zurücksetzen beim Schliessen
            OnStateChanged();
        }

        private void CloseAllModals()
        {
            ShowCreateGameModal = false;
            ShowJoinGameModal = false;
            ShowPieceSelectionModal = false;
            ShowPawnPromotionModalSpecifically = false;
            ShowCardInfoPanelModal = false; // NEU
            // InviteLinkModal bleibt davon unberührt, da es eher eine Benachrichtigung ist
        }
    }
}