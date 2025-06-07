// File: [SolutionDir]\ChessClient\State\CardState.cs
using ChessClient.Models;
using ChessNetwork.Configuration;
using ChessLogic;
using ChessNetwork;
using ChessNetwork.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChessClient.State
{
    public class CardState : ICardState
    {
        public event Action? StateChanged;
        protected virtual void OnStateChanged() => StateChanged?.Invoke();

        public CardDto? SelectedCardForInfoPanel { get; private set; }
        public bool IsCardActivationPending { get; private set; }
        public List<CardDto> PlayerHandCards { get; private set; } = [];
        public int MyDrawPileCount { get; private set; }
        public bool AreCardsRevealed { get; private set; }
        public List<PlayedCardInfo> MyPlayedCardsForHistory { get; private set; } = [];
        public List<PlayedCardInfo> OpponentPlayedCardsForHistory { get; private set; } = [];
        public bool IsPreviewingPlayedCard { get; private set; }
        public Guid? SelectedCardInstanceIdInHand { get; private set; }
        public List<CapturedPieceTypeDto>? CapturedPiecesForRebirth { get; private set; }

        public CardDto? ActiveCardForBoardSelection { get; private set; }
        public bool IsAwaitingRebirthTargetSquareSelection { get; private set; }
        public string? FirstSquareSelectedForTeleportOrSwap { get; private set; }
        public bool IsAwaitingSacrificePawnSelection { get; private set; }
        public PieceType? PieceTypeSelectedForRebirth { get; private set; }
        public bool IsAwaitingTurnConfirmation { get; private set; }

        private readonly IModalState _modalState;
        private readonly IUiState _uiState;
        private readonly IHighlightState _highlightState;

        public CardState(IModalState modalState, IUiState uiState, IHighlightState highlightState)
        {
            _modalState = modalState;
            _uiState = uiState;
            _highlightState = highlightState;
        }

        public void StartCardActivation(CardDto card)
        {
            IsCardActivationPending = true;
            ActiveCardForBoardSelection = card;
            IsAwaitingRebirthTargetSquareSelection = false;
            FirstSquareSelectedForTeleportOrSwap = null;
            IsAwaitingSacrificePawnSelection = false;
            PieceTypeSelectedForRebirth = null;
            IsAwaitingTurnConfirmation = false;
            _highlightState.ClearAllActionHighlights();
            OnStateChanged();
        }

        public void ResetCardActivationState(bool fromCancel, string? messageToKeep = null)
        {
            bool wasPending = IsCardActivationPending;
            var previouslyActiveCard = ActiveCardForBoardSelection;

            IsCardActivationPending = false;
            DeselectActiveHandCard();
            ActiveCardForBoardSelection = null;

            _modalState.ClosePieceSelectionModal();
            _modalState.CloseCardInfoPanelModal();

            _highlightState.ClearAllActionHighlights();
            _highlightState.ClearCardTargetSquaresForSelection();

            PieceTypeSelectedForRebirth = null;
            IsAwaitingRebirthTargetSquareSelection = false;
            FirstSquareSelectedForTeleportOrSwap = null;
            IsAwaitingSacrificePawnSelection = false;

            if (!fromCancel)
            {
                IsAwaitingTurnConfirmation = false;
            }

            if (!string.IsNullOrEmpty(messageToKeep))
            {
                _ = _uiState.SetCurrentInfoMessageForBoxAsync(messageToKeep, true, 4000);
            }
            else if (fromCancel && wasPending && previouslyActiveCard != null)
            {
                _ = _uiState.SetCurrentInfoMessageForBoxAsync($"Aktivierung von '{previouslyActiveCard.Name}' abgebrochen.", true, 4000);
            }

            OnStateChanged();
        }

        public void SetAwaitingRebirthTargetSquareSelection(PieceType pieceType)
        {
            PieceTypeSelectedForRebirth = pieceType;
            IsAwaitingRebirthTargetSquareSelection = true;
            OnStateChanged();
        }

        public void SetFirstSquareForTeleportOrSwap(string square)
        {
            FirstSquareSelectedForTeleportOrSwap = square;
            OnStateChanged();
        }

        public void SetAwaitingSacrificePawnSelection(bool isAwaiting)
        {
            IsAwaitingSacrificePawnSelection = isAwaiting;
            OnStateChanged();
        }

        public void SetAwaitingTurnConfirmation(bool isAwaiting)
        {
            if (IsAwaitingTurnConfirmation == isAwaiting) return;
            IsAwaitingTurnConfirmation = isAwaiting;
            OnStateChanged();
        }

        public CardDto? GetCardDefinitionById(string cardTypeId)
        {
            // This method might be obsolete now as the client should receive full CardDtos.
            return null;
        }

        public void UpdateHandAndDrawPile(InitialHandDto newHandInfo)
        {
            PlayerHandCards.Clear();
            if (newHandInfo?.Hand != null) PlayerHandCards.AddRange(newHandInfo.Hand);
            MyDrawPileCount = newHandInfo?.DrawPileCount ?? 0;
            if (SelectedCardInstanceIdInHand.HasValue && !PlayerHandCards.Any(c => c.InstanceId == SelectedCardInstanceIdInHand.Value))
            {
                SelectedCardInstanceIdInHand = null;
            }
            OnStateChanged();
        }

        public void SetInitialHand(InitialHandDto initialHandDto)
        {
            PlayerHandCards.Clear();
            if (initialHandDto?.Hand != null) PlayerHandCards.AddRange(initialHandDto.Hand);
            MyDrawPileCount = initialHandDto?.DrawPileCount ?? 0;
            MyPlayedCardsForHistory.Clear();
            OpponentPlayedCardsForHistory.Clear();
            ResetCardActivationState(false);
            AreCardsRevealed = false;
            ClearCapturedPiecesForRebirth();
            OnStateChanged();
        }

        public void RevealCards()
        {
            if (!AreCardsRevealed)
            {
                AreCardsRevealed = true;
                OnStateChanged();
            }
        }

        public void AddReceivedCardToHand(CardDto drawnCard, int newDrawPileCount)
        {
            if (PlayerHandCards.Any(card => card.InstanceId == drawnCard.InstanceId))
            {
                return; // Prevent adding duplicate instances
            }
            if (!drawnCard.Name.Contains(CardConstants.NoMoreCardsName) && !drawnCard.Name.Contains(CardConstants.ReplacementCardName))
            {
                PlayerHandCards.Add(drawnCard);
            }
            MyDrawPileCount = newDrawPileCount;
            OnStateChanged();
        }

        public void SelectCardForInfoPanel(CardDto? card, bool isPreview)
        {
            if (card != null)
            {
                _modalState.OpenCardInfoPanelModal(card, !isPreview, isPreview);
            }
        }

        public async Task SetSelectedHandCardAsync(CardDto card, IGameCoreState gameCoreState, IUiState uiState)
        {
            if (gameCoreState.MyColor == gameCoreState.CurrentTurnPlayer && !IsCardActivationPending)
            {
                SelectedCardInstanceIdInHand = card.InstanceId;
                // The orchestration service will determine activatability.
                _modalState.OpenCardInfoPanelModal(card, true, false);
                await uiState.SetCurrentInfoMessageForBoxAsync($"Karte '{card.Name}' ausgewählt.");
            }
        }

        public void SetIsCardActivationPending(bool isPending)
        {
            if (IsCardActivationPending == isPending) return;
            IsCardActivationPending = isPending;
            OnStateChanged();
        }

        public void HandleCardPlayedByMe(Guid cardInstanceId, string cardTypeId)
        {
            PlayerHandCards.RemoveAll(c => c.InstanceId == cardInstanceId);
            OnStateChanged();
        }

        public void AddToMyPlayedHistory(PlayedCardInfo cardInfo)
        {
            MyPlayedCardsForHistory.Add(cardInfo);
            OnStateChanged();
        }

        public void AddToOpponentPlayedHistory(PlayedCardInfo cardInfo)
        {
            OpponentPlayedCardsForHistory.Add(cardInfo);
            OnStateChanged();
        }

        public void ClearPlayedCardsHistory()
        {
            MyPlayedCardsForHistory.Clear();
            OpponentPlayedCardsForHistory.Clear();
            OnStateChanged();
        }

        public void ClearSelectedCardForInfoPanel()
        {
            SelectedCardInstanceIdInHand = null;
            OnStateChanged();
        }

        public void DeselectActiveHandCard()
        {
            if (SelectedCardInstanceIdInHand != null)
            {
                SelectedCardInstanceIdInHand = null;
                OnStateChanged();
            }
        }

        public async Task LoadCapturedPiecesForRebirthAsync(Guid gameId, Guid playerId, IGameSession gameSession)
        {
            try
            {
                var captured = await gameSession.GetCapturedPiecesAsync(gameId, playerId);
                CapturedPiecesForRebirth = captured?.Where(p => p.Type != PieceType.Pawn).ToList() ?? [];
            }
            catch
            {
                CapturedPiecesForRebirth = [];
            }
            finally
            {
                OnStateChanged();
            }
        }

        public void ClearCapturedPiecesForRebirth()
        {
            CapturedPiecesForRebirth = null;
            OnStateChanged();
        }
    }
}