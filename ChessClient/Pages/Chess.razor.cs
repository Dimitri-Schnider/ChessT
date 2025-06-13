using Chess.Logging;
using ChessClient.Configuration;
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
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ChessClient.Pages
{
    public enum BoardInteractivityState
    {
        MyTurn, OpponentTurn, ModalOpen, AwaitingCardSelection, Animating, GameNotRunning
    }

    public partial class Chess : IAsyncDisposable
    {
        [Inject]
        private IConfiguration Configuration
        {
            get; set;
        } = default!;
        [Inject] private IGameSession Game { get; set; } = default!;
        [Inject]
        private NavigationManager NavManager
        {
            get; set;
        } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
        [Inject]
        private ModalService ModalService
        {
            get; set;
        } = default!;
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
        [Inject] private TourService TourService { get; set; } = default!;
        private bool _showMobilePlayedCardsHistory;
        private bool _isGameActiveForLeaveWarning;
        private string InviteLink => GameCoreState.GameId == Guid.Empty ? "" : $"{NavManager.BaseUri}chess?gameId={GameCoreState.GameId}";

        private DotNetObjectReference<Chess> _dotNetHelperForTour;
        private bool _isTutorialRunning;

        protected override async Task OnInitializedAsync()
        {
            if (HubSubscriptionService == null!) throw new InvalidOperationException($"Dienst {nameof(HubSubscriptionService)} nicht injiziert.");
            if (GameOrchestrationService == null!) throw new InvalidOperationException($"Dienst {nameof(GameOrchestrationService)} nicht injiziert.");
            await JSRuntime.InvokeVoidAsync("navigationInterop.addBeforeUnloadListener");
            SubscribeToStateChanges();
            HubSubscriptionService.Initialize();
            ModalService.ShowCreateGameModalRequested += () => ModalState.OpenCreateGameModal();
            ModalService.ShowJoinGameModalRequested += () => ModalState.OpenJoinGameModal(GameCoreState.GameIdFromQueryString);
            GameCoreState.StateChanged += OnGameCoreStateChanged; // NEU: Auf State-Änderungen reagieren
            TourService.TourRequested += StartTutorialAsync;
            await InitializePageBasedOnUrlAsync();
        }

        // Diese Methode reagiert auf Änderungen im GameCoreState
        private async void OnGameCoreStateChanged()
        {
            // Prüfen, ob eine Endspiel-Nachricht neu gesetzt wurde
            if (!string.IsNullOrEmpty(GameCoreState.EndGameMessage))
            {
                // Deaktiviert die Warnung, wenn das Spiel zu Ende ist.
                await UpdateGameActiveStateForLeaveWarning(false);

                if (GameCoreState.EndGameMessage.Contains("gewonnen", StringComparison.OrdinalIgnoreCase))
                {
                    UiState.TriggerWinAnimation();
                }
                else
                {
                    UiState.TriggerLossAnimation();
                }
            }
            else
            {
                // Wenn eine neue Partie gestartet wird, Animationen ausblenden
                UiState.HideEndGameAnimations();
            }

            // Muss StateHasChanged aufrufen, da dies ausserhalb des normalen Render-Zyklus passieren kann
            StateHasChanged();
        }

        protected override void OnAfterRender(bool firstRender)
        {
            if (!firstRender && !string.IsNullOrEmpty(GameCoreState?.EndGameMessage))
            {
                MyMainLayout?.SetCanDownloadGameHistory(true);
            }
        }

        private async Task StartTutorialAsync()
        {
            _isTutorialRunning = true;
            _dotNetHelperForTour = DotNetObjectReference.Create(this);
            await JSRuntime.InvokeVoidAsync("tourInterop.startTour", _dotNetHelperForTour);
        }

        [JSInvokable]
        public async Task PrepareUiForTourStep(string stepTitle)
        {
            switch (stepTitle)
            {
                case "Das Schachbrett":
                    GameCoreState.ResetForNewGame();
                    var board1 = new BoardDto(new PieceDto?[8][] { new PieceDto?[8], new PieceDto?[8], new PieceDto?[8], new PieceDto?[8], new PieceDto?[8], new PieceDto?[8], new PieceDto?[8], new PieceDto?[8] });
                    var createResult = new CreateGameResultDto { GameId = Guid.NewGuid(), PlayerId = Guid.NewGuid(), Color = Player.White, Board = board1 };

                    var tourGameParams = new CreateGameParameters
                    {
                        Name = "Du",
                        Color = Player.White,

                        TimeMinutes = 5,
                        OpponentType = OpponentType.Computer,
                        ComputerDifficulty = ComputerDifficulty.Medium
                    };
      
                    GameCoreState.InitializeNewGame(createResult, tourGameParams);
                    if (GameCoreState.BoardDto != null)
                    {
                        GameCoreState.BoardDto.Squares[6][4] = PieceDto.WhitePawn;
                        GameCoreState.BoardDto.Squares[1][4] = PieceDto.BlackPawn;
                        GameCoreState.BoardDto.Squares[7][6] = PieceDto.WhiteKnight;
                    }
                    GameCoreState.SetGameRunning(true);
                    CardState.SetInitialHand(new InitialHandDto(new(), 10));
                    HighlightState.ClearAllActionHighlights();
                    AnimationState.FinishCardActivationAnimation();
                    break;
                case "Dein Zug":
                    HighlightState.SetHighlights("e2", "e4", false);
                    if (GameCoreState.BoardDto != null)
                    {
                        GameCoreState.BoardDto.Squares[6][4] = null;
                        GameCoreState.BoardDto.Squares[4][4] = PieceDto.WhitePawn;
                    }
                    break;
                case "Karten erhalten":
                    HighlightState.SetHighlights("e7", "e5", false);
                    if (GameCoreState.BoardDto != null)
                    {
                        GameCoreState.BoardDto.Squares[1][4] = null;
                        GameCoreState.BoardDto.Squares[3][4] = PieceDto.BlackPawn;
                    }
                    CardState.AddReceivedCardToHand(new CardDto { InstanceId = Guid.NewGuid(), Id = "ExtraZug", Name = "Extra-Zug", Description = "Spiele einen zweiten Zug direkt nach diesem.", ImageUrl = "img/cards/art/1-Extrazug_Art.png" }, 9);
                    break;
                case "Karten-Aktivierung":
                    HighlightState.ClearAllActionHighlights();
                    var cardToAnimate = CardState.PlayerHandCards.FirstOrDefault();
                    if (cardToAnimate != null)
                    {
                        CardState.HandleCardPlayedByMe(cardToAnimate.InstanceId, cardToAnimate.Id);
                        AnimationState.StartCardActivationAnimation(cardToAnimate, true);
                    }
                    break;
                case "Der Extra-Zug":
                    AnimationState.FinishCardActivationAnimation();
                    HighlightState.SetHighlights("g1", "f3", false);
                    if (GameCoreState.BoardDto != null)
                    {
                        GameCoreState.BoardDto.Squares[7][6] = null;
                        GameCoreState.BoardDto.Squares[5][5] = PieceDto.WhiteKnight;
                    }
                    break;
                case "Zug ausnutzen":
                    HighlightState.SetHighlights("f3", "e5", true);
                    if (GameCoreState.BoardDto != null)
                    {
                        GameCoreState.BoardDto.Squares[5][5] = null;
                        GameCoreState.BoardDto.Squares[3][4] = PieceDto.WhiteKnight;
                    }
                    break;
            }
            StateHasChanged(); ;
            // Diese Verzögerung gibt dem Blazor-Renderer Zeit, die UI zu aktualisieren,
            // bevor die Kontrolle an JavaScript zurückgegeben wird.
            await Task.Delay(20);
        }

        [JSInvokable]
        public void EndTutorial()
        {
            if (_isTutorialRunning)
            {
                _isTutorialRunning = false;
                GameCoreState.ResetForNewGame();
                HighlightState.ClearAllActionHighlights();
                CardState.SetInitialHand(new InitialHandDto(new(), 0));
                StateHasChanged();
            }
            _dotNetHelperForTour?.Dispose();
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
                }
            }
        }
        private async Task SubmitCreateGame(CreateGameParameters args)
        {

            // 1. Prüfen, ob bereits ein Spiel aktiv war
            if (GameCoreState.GameId != Guid.Empty)
            {
                // 2. Bestehende SignalR-Verbindung und zugehörige Events sauber trennen
                if (HubSubscriptionService is IAsyncDisposable disposable)
                {
                    await disposable.DisposeAsync();
                }

                // 3. Den gesamten Client-Zustand explizit zurücksetzen
                GameCoreState.ResetForNewGame();
                HighlightState.ClearAllActionHighlights();
                CardState.SetInitialHand(new InitialHandDto(new(), 0)); // Leere Hand setzen

                // 4. Hub-Events neu initialisieren für das kommende Spiel
                HubSubscriptionService.Initialize();
            }


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
                    AnimationState.StartCardSwapAnimation(pendingSwapDetails.CardGiven, pendingSwapDetails.CardReceived);
                    AnimationState.SetPendingSwapAnimationDetails(null);
                }
                else
                {
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
                if (_isTutorialRunning) return BoardInteractivityState.Animating;
                if (GameCoreState is null || UiState is null || CardState is null || ModalState is null || AnimationState is null) return BoardInteractivityState.GameNotRunning;
                if (!GameCoreState.IsGameRunning || !string.IsNullOrEmpty(GameCoreState.EndGameMessage)) return BoardInteractivityState.GameNotRunning;
                if (UiState.IsCountdownVisible || AnimationState.IsCardActivationAnimating || AnimationState.IsCardSwapAnimating) return BoardInteractivityState.Animating;
                if (ModalState.ShowCreateGameModal || ModalState.ShowJoinGameModal || ModalState.ShowPieceSelectionModal || ModalState.ShowCardInfoPanelModal) return BoardInteractivityState.ModalOpen;
                if (CardState.IsCardActivationPending) return BoardInteractivityState.AwaitingCardSelection;
                if (CardState.IsAwaitingTurnConfirmation) return BoardInteractivityState.OpponentTurn;
                if (GameCoreState.MyColor != GameCoreState.CurrentTurnPlayer) return BoardInteractivityState.OpponentTurn;
                return BoardInteractivityState.MyTurn;
            }
        }
        private bool IsChessboardEnabled()
        {
            var state = CurrentBoardState;
            return state is BoardInteractivityState.MyTurn or BoardInteractivityState.AwaitingCardSelection;
        }

        private void CloseWinLossModal()
        {
            GameCoreState.ClearEndGameMessage();
        }

        private bool IsBoardInCardSelectionMode() => CardState.IsCardActivationPending && CardState.ActiveCardForBoardSelection != null && CardState.ActiveCardForBoardSelection.Id is CardConstants.Teleport or CardConstants.Positionstausch or CardConstants.Wiedergeburt or CardConstants.SacrificeEffect;
        private Player? GetPlayerColorForCardPieceSelection() => (CardState.IsCardActivationPending && CardState.ActiveCardForBoardSelection?.Id is CardConstants.Teleport or CardConstants.Positionstausch && string.IsNullOrEmpty(CardState.FirstSquareSelectedForTeleportOrSwap)) ? GameCoreState.MyColor : null;
        private string? GetFirstSelectedSquareForCardEffect() => CardState.FirstSquareSelectedForTeleportOrSwap;
        private void ToggleMobilePlayedCardsHistory() => _showMobilePlayedCardsHistory = !_showMobilePlayedCardsHistory;
        private void StartNewGameFromEndGame()
        {
            // Alle relevanten Spielzustände zurücksetzen
            GameCoreState.ResetForNewGame();
            HighlightState.ClearAllActionHighlights();
            CardState.SetInitialHand(new InitialHandDto(new(), 0));

            // Das Hauptlayout aktualisieren, um anzuzeigen, dass kein Spiel aktiv ist
            if (MyMainLayout != null)
            {
                MyMainLayout.UpdateActiveGameId(Guid.Empty);
            }

            // Das Modal zum Erstellen eines neuen Spiels anfordern
            ModalService.RequestShowCreateGameModal();
            // UI-Update sicherstellen
            StateHasChanged();
        }
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
            if (_isTutorialRunning) return;
            if (_isGameActiveForLeaveWarning == isActive) return;
            _isGameActiveForLeaveWarning = isActive;
            await JSRuntime.InvokeVoidAsync("navigationInterop.setGameActiveState", isActive);
        }

        private async Task DownloadGameHistory()
        {
            if (GameCoreState.GameId != Guid.Empty)
            {
                string? serverBaseUrlFromConfig = Configuration.GetValue<string>("ServerBaseUrl");
                string serverBaseUrl = ClientConstants.DefaultServerBaseUrl;

                if (!string.IsNullOrEmpty(serverBaseUrlFromConfig))
                {
                    serverBaseUrl = serverBaseUrlFromConfig;
                }

                var downloadUrl = $"{serverBaseUrl.TrimEnd('/')}/api/games/{GameCoreState.GameId}/downloadhistory";
                if (JSRuntime != null)
                {
                    await JSRuntime.InvokeVoidAsync("window.open", downloadUrl, "_blank");
                }
            }
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
            GameCoreState.StateChanged -= OnGameCoreStateChanged;
            ModalService.ShowCreateGameModalRequested -= ModalState.OpenCreateGameModal;
            ModalService.ShowJoinGameModalRequested -= () => ModalState.OpenJoinGameModal(GameCoreState.GameIdFromQueryString);
            TourService.TourRequested -= StartTutorialAsync;
            _dotNetHelperForTour?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}