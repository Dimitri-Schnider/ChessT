using ChessNetwork.DTOs;
using ChessNetwork;
using ChessClient.Models;

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

        CardDto? GetCardDefinitionById(string cardTypeId);
        void SetInitialHand(InitialHandDto initialHandDto);
        void AddReceivedCardToHand(CardDto drawnCard, int newDrawPileCount);
        void HandleCardPlayedByMe(Guid cardInstanceId, string cardTypeId);

        void SelectCardForInfoPanel(CardDto? card, bool isPreview);
        Task SetSelectedHandCardAsync(CardDto card, IGameCoreState gameCoreState, IUiState uiState);
        // void SetCurrentActivationHandler(IClientCardActivationHandler? handler); // Entfernt
        // Task ResetCardActivationHandlerAsync(); // Entfernt
        void SetIsCardActivationPending(bool isPending);
        void AddToMyPlayedHistory(PlayedCardInfo cardInfo);
        void AddToOpponentPlayedHistory(PlayedCardInfo cardInfo);
        // void MarkSingleUseCardAsUsed(string cardTypeId); // Entfernt
        void ClearPlayedCardsHistory();
        void ClearSelectedCardForInfoPanel();
        void DeselectActiveHandCard();

        List<CapturedPieceTypeDto>? CapturedPiecesForRebirth { get; }
        Task LoadCapturedPiecesForRebirthAsync(Guid gameId, Guid playerId, IGameSession gameSession);
        void ClearCapturedPiecesForRebirth();

        void UpdateHandAndDrawPile(InitialHandDto newHandInfo);
    }
}