using ChessNetwork.DTOs;
using ChessNetwork;
using ChessClient.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChessLogic;

namespace ChessClient.State
{
    public interface ICardState
    {
        event Action? StateChanged;
        CardDto? SelectedCardForInfoPanel { get; }
        bool IsCardActivationPending { get; }
        List<CardDto> PlayerHandCards { get; }
        int MyDrawPileCount { get; }
        List<PlayedCardInfo> MyPlayedCardsForHistory { get; }
        List<PlayedCardInfo> OpponentPlayedCardsForHistory { get; }
        bool IsPreviewingPlayedCard { get; }
        Guid? SelectedCardInstanceIdInHand { get; }
        bool AreCardsRevealed { get; }

        // NEUE Properties für den Aktivierungsprozess
        CardDto? ActiveCardForBoardSelection { get; }
        bool IsAwaitingRebirthTargetSquareSelection { get; }
        string? FirstSquareSelectedForTeleportOrSwap { get; }
        bool IsAwaitingSacrificePawnSelection { get; }
        PieceType? PieceTypeSelectedForRebirth { get; }
        bool IsAwaitingTurnConfirmation { get; }

        CardDto? GetCardDefinitionById(string cardTypeId);
        void SetInitialHand(InitialHandDto initialHandDto);
        void AddReceivedCardToHand(CardDto drawnCard, int newDrawPileCount);
        void HandleCardPlayedByMe(Guid cardInstanceId, string cardTypeId);

        void SelectCardForInfoPanel(CardDto? card, bool isPreview);
        Task SetSelectedHandCardAsync(CardDto card, IGameCoreState gameCoreState, IUiState uiState);
        void SetIsCardActivationPending(bool isPending);
        void AddToMyPlayedHistory(PlayedCardInfo cardInfo);
        void AddToOpponentPlayedHistory(PlayedCardInfo cardInfo);
        void ClearPlayedCardsHistory();
        void ClearSelectedCardForInfoPanel();
        void DeselectActiveHandCard();
        void RevealCards();

        List<CapturedPieceTypeDto>? CapturedPiecesForRebirth { get; }
        Task LoadCapturedPiecesForRebirthAsync(Guid gameId, Guid playerId, IGameSession gameSession);
        void ClearCapturedPiecesForRebirth();
        void UpdateHandAndDrawPile(InitialHandDto newHandInfo);

        // NEUE Methoden zur Zustandsverwaltung
        void StartCardActivation(CardDto card);
        void ResetCardActivationState(bool fromCancel, string? messageToKeep = null);
        void SetAwaitingRebirthTargetSquareSelection(PieceType pieceType);
        void SetFirstSquareForTeleportOrSwap(string square);
        void SetAwaitingSacrificePawnSelection(bool isAwaiting);
        void SetAwaitingTurnConfirmation(bool isAwaiting);
    }
}