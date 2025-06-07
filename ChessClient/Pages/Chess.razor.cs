// File: [SolutionDir]\ChessClient\Pages\Chess.razor.cs
using Chess.Logging;
using ChessClient.Layout;
using ChessClient.Models;
using ChessClient.Services;
using ChessClient.State;
using ChessLogic;
using ChessNetwork;
using ChessNetwork.Configuration;
using ChessNetwork.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace ChessClient.Pages
{
    public partial class Chess : IAsyncDisposable
    {
        [Inject] private IConfiguration Configuration { get; set; } = default!;
        [Inject] private IGameSession Game { get; set; } = default!;
        [Inject] private NavigationManager NavManager { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] private ModalService ModalService { get; set; } = default!;
        [CascadingParameter(Name = "MyMainLayout")] private MainLayout MyMainLayout { get; set; } = default!;
        [Inject] private IUiState UiState { get; set; } = default!;
        [Inject] private IModalState ModalState { get; set; } = default!;
        [Inject] private IGameCoreState GameCoreState { get; set; } = default!;
        [Inject] private IHighlightState HighlightState { get; set; } = default!;
        [Inject] private IAnimationState AnimationState { get; set; } = default!;
        [Inject] private ICardState CardState { get; set; } = default!;
        [Inject] private GameOrchestrationService GameOrchestrationService { get; set; } = default!;
        [Inject] private HubSubscriptionService HubSubscriptionService { get; set; } = default!;
        [Inject] private IChessLogger Logger { get; set; } = default!;

        private bool _showMobilePlayedCardsHistory;
        private bool _isGameActiveForLeaveWarning;

        private string InviteLink => GameCoreState.GameId == Guid.Empty ? "" : $"{NavManager.BaseUri}chess?gameId={GameCoreState.GameId}";

        protected override async Task OnInitializedAsync()
        {
            if (HubSubscriptionService == null!) throw new InvalidOperationException($"Dienst {nameof(HubSubscriptionService)} nicht injiziert.");
            if (GameOrchestrationService == null!) throw new InvalidOperationException($"Dienst {nameof(GameOrchestrationService)} nicht injiziert.");

            await JSRuntime.InvokeVoidAsync("navigationInterop.addBeforeUnloadListener");

            SubscribeToStateChanges();

            HubSubscriptionService.Initialize();

            ModalService.ShowCreateGameModalRequested += () => ModalState.OpenCreateGameModal();
            ModalService.ShowJoinGameModalRequested += () => ModalState.OpenJoinGameModal(GameCoreState.GameIdFromQueryString);

            await InitializePageBasedOnUrlAsync();
        }

        private async Task InitializePageBasedOnUrlAsync()
        {
            GameCoreState.SetGameSpecificDataInitialized(false);
            var uri = NavManager.ToAbsoluteUri(NavManager.Uri);
            if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("gameId", out var id))
            {
                GameCoreState.SetGameIdFromQuery(id.ToString(), false);
                try
                {
                    await Game.GetGameInfoAsync(Guid.Parse(id.ToString()));
                    GameCoreState.SetGameIdFromQuery(id.ToString(), true);
                    ModalState.OpenJoinGameModal(id.ToString());
                }
                catch (Exception)
                {
                    UiState.SetErrorMessage($"Spiel mit ID '{id}' konnte nicht gefunden werden.");
                    ModalState.OpenCreateGameModal();
                }
            }
            else if (GameCoreState.CurrentPlayerInfo == null)
            {
                ModalState.OpenCreateGameModal();
            }
        }

        private async Task SubmitCreateGame(CreateGameParameters args)
        {
            var (success, gameId) = await GameOrchestrationService.CreateNewGameAsync(args);
            if (success)
            {
                MyMainLayout.UpdateActiveGameId(gameId);
                if (!GameCoreState.IsPvCGame)
                {
                    ModalState.OpenInviteLinkModal(InviteLink);
                }
                await UpdateGameActiveStateForLeaveWarning(true);
            }
        }

        private async Task SubmitJoinGame(JoinGameParameters args)
        {
            var (success, gameId) = await GameOrchestrationService.JoinExistingGameAsync(args.Name, args.GameId);
            if (success)
            {
                MyMainLayout.UpdateActiveGameId(gameId);
                await UpdateGameActiveStateForLeaveWarning(true);
            }
        }

        private async Task HandlePlayerMove(MoveDto clientMove)
        {
            await GameOrchestrationService.ProcessPlayerMoveAsync(clientMove);
        }

        private async Task HandleSquareClickForCard(string algebraicCoord)
        {
            if (!CardState.IsCardActivationPending || CardState.ActiveCardForBoardSelection == null) return;

            string cardId = CardState.ActiveCardForBoardSelection.Id;
            var request = new ActivateCardRequestDto { CardInstanceId = CardState.SelectedCardInstanceIdInHand ?? Guid.Empty, CardTypeId = cardId };

            // Logik für die zwei-Klick-Aktionen
            if (cardId is CardConstants.Teleport or CardConstants.Positionstausch)
            {
                if (string.IsNullOrEmpty(CardState.FirstSquareSelectedForTeleportOrSwap))
                {
                    CardState.SetFirstSquareForTeleportOrSwap(algebraicCoord);
                }
                else
                {
                    request.FromSquare = CardState.FirstSquareSelectedForTeleportOrSwap;
                    request.ToSquare = algebraicCoord;
                    await GameOrchestrationService.FinalizeCardActivationOnServerAsync(request, CardState.ActiveCardForBoardSelection);
                }
            }
            else if (cardId is CardConstants.Wiedergeburt && CardState.IsAwaitingRebirthTargetSquareSelection)
            {
                request.PieceTypeToRevive = CardState.PieceTypeSelectedForRebirth;
                request.TargetRevivalSquare = algebraicCoord;
                await GameOrchestrationService.FinalizeCardActivationOnServerAsync(request, CardState.ActiveCardForBoardSelection);
            }
            else if (cardId is CardConstants.SacrificeEffect && CardState.IsAwaitingSacrificePawnSelection)
            {
                request.FromSquare = algebraicCoord;
                await GameOrchestrationService.FinalizeCardActivationOnServerAsync(request, CardState.ActiveCardForBoardSelection);
            }
        }

        private async Task HandlePieceTypeSelectedFromModal(PieceType selectedType)
        {
            if (ModalState.ShowPawnPromotionModalSpecifically)
            {
                await GameOrchestrationService.ProcessPawnPromotionAsync(selectedType);
            }
            else if (CardState.IsCardActivationPending && CardState.ActiveCardForBoardSelection?.Id == CardConstants.Wiedergeburt)
            {
                CardState.SetAwaitingRebirthTargetSquareSelection(selectedType);
            }
        }

        private async Task HandleActivateCardFromModal(CardDto cardToActivate)
        {
            ModalState.CloseCardInfoPanelModal();
            await GameOrchestrationService.ActivateCardAsync(cardToActivate);
        }

        private void HandleCloseCardInfoModal()
        {
            if (!ModalState.IsCardInInfoPanelModalPreviewOnly)
            {
                CardState.ResetCardActivationState(true, "Aktivierung abgebrochen.");
            }
            ModalState.CloseCardInfoPanelModal();
        }

        private void HandlePlayedCardSelected(CardDto card)
        {
            CardState.SelectCardForInfoPanel(card, true);
        }

        private void HandleGenericAnimationFinished()
        {
            AnimationState.FinishCardActivationAnimation();
            // Die Logik zum Starten der Swap-Animation befindet sich jetzt im AnimationState
        }

        private void HandleSwapAnimationFinished()
        {
            AnimationState.FinishCardSwapAnimation();
        }

        private void CloseCreateGameModal() => ModalState.CloseCreateGameModal();
        private void CloseJoinGameModal() => ModalState.CloseJoinGameModal();
        private void HandlePieceSelectionModalCancelled() => CardState.ResetCardActivationState(true, "Auswahl abgebrochen.");

        private bool IsChessboardEnabled()
        {
            if (UiState.IsCountdownVisible || !CardState.AreCardsRevealed || CardState.IsAwaitingTurnConfirmation || ModalState.ShowCreateGameModal || ModalState.ShowJoinGameModal || ModalState.ShowPieceSelectionModal || ModalState.ShowCardInfoPanelModal)
            {
                return false;
            }

            if (CardState.IsCardActivationPending)
            {
                string? cardId = CardState.ActiveCardForBoardSelection?.Id;
                return cardId is CardConstants.Teleport or CardConstants.Positionstausch or CardConstants.Wiedergeburt or CardConstants.SacrificeEffect;
            }

            return GameCoreState.OpponentJoined && GameCoreState.MyColor == GameCoreState.CurrentTurnPlayer && string.IsNullOrEmpty(GameCoreState.EndGameMessage);
        }

        private bool IsBoardInCardSelectionMode() => CardState.IsCardActivationPending && CardState.ActiveCardForBoardSelection != null && CardState.ActiveCardForBoardSelection.Id is CardConstants.Teleport or CardConstants.Positionstausch or CardConstants.Wiedergeburt or CardConstants.SacrificeEffect;
        private Player? GetPlayerColorForCardPieceSelection() => (CardState.IsCardActivationPending && CardState.ActiveCardForBoardSelection?.Id is CardConstants.Teleport or CardConstants.Positionstausch && string.IsNullOrEmpty(CardState.FirstSquareSelectedForTeleportOrSwap)) ? GameCoreState.MyColor : null;
        private string? GetFirstSelectedSquareForCardEffect() => CardState.FirstSquareSelectedForTeleportOrSwap;
        private void ToggleMobilePlayedCardsHistory() => _showMobilePlayedCardsHistory = !_showMobilePlayedCardsHistory;
        private void StartNewGameFromEndGame() => NavManager.NavigateTo(NavManager.Uri, forceLoad: true);
        private void StateHasChanged() => InvokeAsync(base.StateHasChanged);

        private void SubscribeToStateChanges()
        {
            UiState.StateChanged += StateHasChanged;
            ModalState.StateChanged += StateHasChanged;
            GameCoreState.StateChanged += StateHasChanged;
            HighlightState.StateChanged += StateHasChanged;
            AnimationState.StateChanged += StateHasChanged;
            CardState.StateChanged += StateHasChanged;
        }

        private void UnsubscribeFromStateChanges()
        {
            UiState.StateChanged -= StateHasChanged;
            ModalState.StateChanged -= StateHasChanged;
            GameCoreState.StateChanged -= StateHasChanged;
            HighlightState.StateChanged -= StateHasChanged;
            AnimationState.StateChanged -= StateHasChanged;
            CardState.StateChanged -= StateHasChanged;
        }

        private async Task UpdateGameActiveStateForLeaveWarning(bool isActive)
        {
            if (_isGameActiveForLeaveWarning == isActive) return;
            _isGameActiveForLeaveWarning = isActive;
            await JSRuntime.InvokeVoidAsync("navigationInterop.setGameActiveState", isActive);
        }

        public async ValueTask DisposeAsync()
        {
            await UpdateGameActiveStateForLeaveWarning(false);
            if (JSRuntime != null)
            {
                await JSRuntime.InvokeVoidAsync("navigationInterop.removeBeforeUnloadListener");
            }
            UnsubscribeFromStateChanges();
            if (HubSubscriptionService is IAsyncDisposable disposable)
            {
                await disposable.DisposeAsync();
            }
            ModalService.ShowCreateGameModalRequested -= ModalState.OpenCreateGameModal;
            ModalService.ShowJoinGameModalRequested -= () => ModalState.OpenJoinGameModal(GameCoreState.GameIdFromQueryString);
            GC.SuppressFinalize(this);
        }
    }
}