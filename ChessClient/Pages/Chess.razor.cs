using Chess.Logging;
using ChessClient.Layout;
using ChessClient.Models;
using ChessClient.Services;
using ChessClient.State;
using ChessClient.Utils;
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
    public enum BoardInteractivityState
    {
        MyTurn,
        OpponentTurn,
        ModalOpen,
        AwaitingCardSelection,
        Animating,
        GameNotRunning
    }
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
                    ModalState.OpenErrorModal($"Spiel mit ID '{id}' konnte nicht gefunden werden.");
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
            ModalState.CloseCreateGameModal();
            UiState.SetIsCreatingGame(true);

            try
            {
                var createResult = await GameOrchestrationService.CreateGameOnServerAsync(args);
                if (createResult is null) return;

                UiState.SetIsCreatingGame(false);
                await GameOrchestrationService.ConnectAndRegisterPlayerToHubAsync(createResult.GameId, createResult.PlayerId);
                MyMainLayout.UpdateActiveGameId(createResult.GameId);
                if (!GameCoreState.IsPvCGame)
                {
                    ModalState.OpenInviteLinkModal(InviteLink);
                }
                await UpdateGameActiveStateForLeaveWarning(true);
            }
            catch (Exception ex)
            {
                ModalState.OpenErrorModal($"Ein unerwarteter Fehler ist aufgetreten: {ex.Message}");
            }
            finally
            {
                if (UiState.IsCreatingGame)
                {
                    UiState.SetIsCreatingGame(false);
                }
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
            await GameOrchestrationService.HandleSquareClickForCardAsync(algebraicCoord);
        }

        private async Task HandlePieceTypeSelectedFromModal(PieceType selectedType)
        {
            if (ModalState.ShowPawnPromotionModalSpecifically)
            {
                await GameOrchestrationService.ProcessPawnPromotionAsync(selectedType);
            }
            else if (CardState.IsCardActivationPending && CardState.ActiveCardForBoardSelection?.Id == CardConstants.Wiedergeburt)
            {
                await GameOrchestrationService.HandlePieceTypeSelectedFromModalAsync(selectedType);
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
            var lastAnimatedCard = AnimationState.LastAnimatedCard;
            var pendingSwapDetails = AnimationState.PendingSwapAnimationDetails;

            AnimationState.FinishCardActivationAnimation();

            if (lastAnimatedCard?.Id == CardConstants.CardSwap)
            {
                if (pendingSwapDetails != null)
                {
                    // Fall 1: Details sind schon da. Animation sofort starten.
                    AnimationState.StartCardSwapAnimation(pendingSwapDetails.CardGiven, pendingSwapDetails.CardReceived);
                    AnimationState.SetPendingSwapAnimationDetails(null);
                }
                else
                {
                    // Fall 2: Details sind noch nicht da. Flag setzen und warten.
                    AnimationState.SetGenericAnimationFinishedForSwap(true);
                }
            }
        }

        private void HandleSwapAnimationFinished()
        {
            AnimationState.FinishCardSwapAnimation();
        }

        private void CloseCreateGameModal() => ModalState.CloseCreateGameModal();
        private void CloseJoinGameModal() => ModalState.CloseJoinGameModal();
        private void HandlePieceSelectionModalCancelled() => CardState.ResetCardActivationState(true, "Auswahl abgebrochen.");

        private BoardInteractivityState CurrentBoardState
        {
            get
            {
                // Prüfe Zustände mit höchster Priorität zuerst
                if (GameCoreState is null || UiState is null || CardState is null || ModalState is null || AnimationState is null)
                    return BoardInteractivityState.GameNotRunning;
                if (!GameCoreState.IsGameRunning || !string.IsNullOrEmpty(GameCoreState.EndGameMessage))
                    return BoardInteractivityState.GameNotRunning;
                if (UiState.IsCountdownVisible || AnimationState.IsCardActivationAnimating || AnimationState.IsCardSwapAnimating)
                    return BoardInteractivityState.Animating;
                if (ModalState.ShowCreateGameModal || ModalState.ShowJoinGameModal || ModalState.ShowPieceSelectionModal || ModalState.ShowCardInfoPanelModal)
                    return BoardInteractivityState.ModalOpen;
                if (CardState.IsCardActivationPending)
                    return BoardInteractivityState.AwaitingCardSelection;
                if (CardState.IsAwaitingTurnConfirmation)
                    return BoardInteractivityState.OpponentTurn;
                if (GameCoreState.MyColor != GameCoreState.CurrentTurnPlayer)
                    return BoardInteractivityState.OpponentTurn;
                // Wenn keine der obigen Bedingungen zutrifft, ist der Spieler am Zug
                return BoardInteractivityState.MyTurn;
            }
        }

        private bool IsChessboardEnabled()
        {
            var state = CurrentBoardState;
            return state is BoardInteractivityState.MyTurn or BoardInteractivityState.AwaitingCardSelection;
        }


        private bool IsBoardInCardSelectionMode() => CardState.IsCardActivationPending && CardState.ActiveCardForBoardSelection != null && CardState.ActiveCardForBoardSelection.Id is CardConstants.Teleport or CardConstants.Positionstausch or CardConstants.Wiedergeburt or CardConstants.SacrificeEffect;
        private Player? GetPlayerColorForCardPieceSelection() => (CardState.IsCardActivationPending && CardState.ActiveCardForBoardSelection?.Id is CardConstants.Teleport or CardConstants.Positionstausch && string.IsNullOrEmpty(CardState.FirstSquareSelectedForTeleportOrSwap)) ? GameCoreState.MyColor : null;
        private string? GetFirstSelectedSquareForCardEffect() => CardState.FirstSquareSelectedForTeleportOrSwap;
        private void ToggleMobilePlayedCardsHistory() => _showMobilePlayedCardsHistory = !_showMobilePlayedCardsHistory;
        private void StartNewGameFromEndGame() => NavManager.NavigateTo(NavManager.Uri, forceLoad: true);
        private new void StateHasChanged() => InvokeAsync(base.StateHasChanged);

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