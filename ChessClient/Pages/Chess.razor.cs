using Chess.Logging;
using ChessClient.Configuration;
using ChessClient.Layout;
using ChessClient.Models;
using ChessClient.Services.Connectivity;
using ChessClient.Services.Game;
using ChessClient.Services.UI;
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
    // Definiert die möglichen Interaktivitäts-Zustände des Schachbretts.
    // Dient als State Machine zur Steuerung der UI.
    public enum BoardInteractivityState
    {
        MyTurn, OpponentTurn, ModalOpen, AwaitingCardSelection, Animating, GameNotRunning
    }

    // Die Code-Behind-Klasse für die Hauptseite Chess.razor.
    // Dies ist die umfangreichste und wichtigste Klasse im Client, da sie
    // alle Dienste, Zustände und Benutzerinteraktionen koordiniert.
    public partial class Chess : IAsyncDisposable
    {
        #region Injections & Cascading Parameters
        // Injizierte Dienste und State Container
        [Inject] private IConfiguration Configuration { get; set; } = default!;
        [Inject] private IGameSession Game { get; set; } = default!;                                            // Für HTTP-basierte Serverkommunikation.
        [Inject] private NavigationManager NavManager { get; set; } = default!;                                 // Für URL-Manipulation und Navigation.
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;                                         // Für JavaScript-Interop.
        [Inject] private ModalService ModalService { get; set; } = default!;                                    // Zur Anforderung globaler Modals.
        [CascadingParameter(Name = "MyMainLayout")] private MainLayout MyMainLayout { get; set; } = default!;   // Referenz auf das Hauptlayout.

        // ChessHubService direkt injizieren, um Gruppen verlassen zu können.
        [Inject] private ChessHubService HubService { get; set; } = default!;

        // State-Container
        [Inject] private IUiState UiState { get; set; } = default!;
        [Inject] private IModalState ModalState { get; set; } = default!;
        [Inject] private IGameCoreState GameCoreState { get; set; } = default!;
        [Inject] private IHighlightState HighlightState { get; set; } = default!;
        [Inject] private IAnimationState AnimationState { get; set; } = default!;
        [Inject] private ICardState CardState { get; set; } = default!;

        // Orchestrierungs-Dienste
        [Inject] private GameOrchestrationService GameOrchestrationService { get; set; } = default!;            // Koordiniert komplexe Spielabläufe.
        [Inject] private HubSubscriptionService HubSubscriptionService { get; set; } = default!;                // Verwaltet die SignalR-Hub-Events.
        [Inject] private IChessLogger Logger { get; set; } = default!;
        [Inject] private TourService TourService { get; set; } = default!;                                      // Für die interaktive Tour.
        #endregion

        // Private Felder und Eigenschaften
        private bool _showMobilePlayedCardsHistory; // Zustand für die Sichtbarkeit der Kartenhistorie auf Mobilgeräten.
        private bool _isGameActiveForLeaveWarning;  // Zustand für die "Seite verlassen?"-Warnung des Browsers.
        private string InviteLink => GameCoreState.GameId == Guid.Empty ? "" : $"{NavManager.BaseUri}chess?gameId={GameCoreState.GameId}";

        private DotNetObjectReference<Chess>? _dotNetHelperForTour; // Referenz auf diese Instanz für die Tour-JS-Interop.
        private bool _isTutorialRunning;

        // Lifecycle-Methoden

        // Initialisiert die Komponente, abonniert alle notwendigen Events und prüft die URL.
        protected override async Task OnInitializedAsync()
        {
            if (HubSubscriptionService == null!) throw new InvalidOperationException($"Dienst {nameof(HubSubscriptionService)} nicht injiziert.");
            if (GameOrchestrationService == null!) throw new InvalidOperationException($"Dienst {nameof(GameOrchestrationService)} nicht injiziert.");

            // Registriert den Browser-Event-Handler, um den Benutzer vor dem Verlassen der Seite während eines Spiels zu warnen.
            await JSRuntime.InvokeVoidAsync("navigationInterop.addBeforeUnloadListener");

            // Abonniert die StateChanged-Events aller State-Container, um die UI bei Änderungen neu zu rendern.
            SubscribeToStateChanges();

            HubSubscriptionService.Initialize(); // Initialisiert die SignalR-Event-Handler.

            // Abonniert Events vom ModalService, um Modals auf Anfrage zu öffnen.
            ModalService.ShowCreateGameModalRequested += () => ModalState.OpenCreateGameModal();
            ModalService.ShowJoinGameModalRequested += () => ModalState.OpenJoinGameModal(GameCoreState.GameIdFromQueryString);

            GameCoreState.StateChanged += OnGameCoreStateChanged; // Spezieller Handler für Spielende-Logik.
            TourService.TourRequested += StartTutorialAsync; // Handler für den Start der Tour.

            // Prüft, ob in der URL eine 'gameId' übergeben wurde, um einem Spiel direkt beizutreten.
            await InitializePageBasedOnUrlAsync();
        }

        // Spezieller Handler, der auf Änderungen im GameCoreState reagiert, insbesondere auf das Spielende.
        private async void OnGameCoreStateChanged()
        {
            if (!string.IsNullOrEmpty(GameCoreState.EndGameMessage))
            {
                // Deaktiviert die Warnung beim Verlassen der Seite, da das Spiel vorbei ist.
                await UpdateGameActiveStateForLeaveWarning(false);

                // Löst die entsprechende globale Animation (Sieg/Niederlage) aus.
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
                // Versteckt die Animationen, wenn ein neues Spiel gestartet wird.
                UiState.HideEndGameAnimations();
            }

            // UI-Update anstossen, da dies ausserhalb des normalen Render-Zyklus passieren kann.
            StateHasChanged();
        }

        // Nach dem Rendern wird sichergestellt, dass das Layout über den Status des Spielverlaufs informiert ist.
        protected override void OnAfterRender(bool firstRender)
        {
            if (!firstRender && !string.IsNullOrEmpty(GameCoreState?.EndGameMessage))
            {
                MyMainLayout?.SetCanDownloadGameHistory(true);
            }
        }

        #region Tour-Logik
        // Startet die interaktive Tour mittels JS-Interop.
        private async Task StartTutorialAsync()
        {
            _isTutorialRunning = true;
            _dotNetHelperForTour = DotNetObjectReference.Create(this);
            await JSRuntime.InvokeVoidAsync("tourInterop.startTour", _dotNetHelperForTour);
        }

        // Wird von JavaScript während der Tour aufgerufen, um die UI für den jeweiligen Schritt vorzubereiten.
        [JSInvokable]
        public async Task PrepareUiForTourStep(string stepTitle)
        {
            // Manipuliert den Client-Zustand, um die gewünschte Szene für jeden Tour-Schritt zu erstellen.
            switch (stepTitle)
            {
                case "Das Schachbrett":
                    // Initialisiert ein leeres Spiel für die Tour.
                    GameCoreState.ResetForNewGame();
                    var board1 = new BoardDto(new PieceDto?[8][] { new PieceDto?[8], new PieceDto?[8], new PieceDto?[8], new PieceDto?[8], new PieceDto?[8], new PieceDto?[8], new PieceDto?[8], new PieceDto?[8] });
                    var createResult = new CreateGameResultDto { GameId = Guid.NewGuid(), PlayerId = Guid.NewGuid(), Color = Player.White, Board = board1 };
                    var tourGameDto = new CreateGameDto
                    {
                        PlayerName = "Du",
                        Color = Player.White,
                        InitialMinutes = 5,
                        OpponentType = OpponentType.Computer, // Enum aus ChessNetwork.DTOs
                        ComputerDifficulty = ComputerDifficulty.Medium // Enum aus ChessNetwork.DTOs
                    };
                    GameCoreState.InitializeNewGame(createResult, tourGameDto);

                    if (GameCoreState.BoardDto != null)
                    {
                        // Platziert manuell Figuren für die Demo.
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
                    // Fügt eine Demo-Karte zur Hand hinzu.
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
            // Diese Verzögerung gibt dem Blazor-Renderer Zeit, die UI zu aktualisieren, bevor die Kontrolle an JavaScript zurückgegeben wird.
            await Task.Delay(20);
        }

        // Wird von JavaScript aufgerufen, um die Tour zu beenden und den Zustand zurückzusetzen.
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
        #endregion

        #region Event-Handler für UI-Interaktionen

        // Verarbeitet die URL beim Laden der Seite.
        private async Task InitializePageBasedOnUrlAsync()
        {
            GameCoreState.SetGameSpecificDataInitialized(false);
            var uri = NavManager.ToAbsoluteUri(NavManager.Uri);
            if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("gameId", out var id))
            {
                GameCoreState.SetGameIdFromQuery(id.ToString(), false);
                try
                {
                    // Prüft, ob das Spiel auf dem Server existiert.
                    await Game.GetGameInfoAsync(Guid.Parse(id.ToString()));
                    GameCoreState.SetGameIdFromQuery(id.ToString(), true);
                    // Öffnet automatisch das "Spiel beitreten"-Modal.
                    ModalState.OpenJoinGameModal(id.ToString());
                }
                catch (Exception)
                {
                    ModalState.OpenErrorModal($"Spiel mit ID '{id}' konnte nicht gefunden werden.");
                }
            }
        }

        // Handler für das "Spiel erstellen"-Modal.
        private async Task SubmitCreateGame(CreateGameDto dto)
        {
            // Wenn bereits ein Spiel aktiv war, wird alles sauber zurückgesetzt.
            if (GameCoreState.GameId != Guid.Empty)
            {
                // Explizit die SignalR-Gruppe des alten Spiels verlassen.
                var oldGameId = GameCoreState.GameId;
                if (HubService.IsConnected)
                {
                    await HubService.LeaveGameGroupAsync(oldGameId);
                }

                if (HubSubscriptionService is IAsyncDisposable disposable)
                {
                    await disposable.DisposeAsync(); // Trennt die alte SignalR-Verbindung.
                }
                GameCoreState.ResetForNewGame();
                HighlightState.ClearAllActionHighlights();
                CardState.SetInitialHand(new InitialHandDto(new(), 0));
                HubSubscriptionService.Initialize(); // Re-initialisiert die Hub-Handler für das neue Spiel.
            }

            ModalState.CloseCreateGameModal();
            UiState.SetIsCreatingGame(true);
            try
            {
                // Das DTO wird direkt an den OrchestrationService weitergegeben.
                var createResult = await GameOrchestrationService.CreateGameOnServerAsync(dto);
                if (createResult is null) return;

                UiState.SetIsCreatingGame(false);
                await GameOrchestrationService.ConnectAndRegisterPlayerToHubAsync(createResult.GameId, createResult.PlayerId);
                MyMainLayout.UpdateActiveGameId(createResult.GameId);
                // Zeigt den Einladungslink an, ausser bei Spielen gegen den Computer.
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

        // Handler für das "Spiel beitreten"-Modal.
        private async Task SubmitJoinGame(JoinGameParameters args)
        {
            var (success, gameId) = await GameOrchestrationService.JoinExistingGameAsync(args.Name, args.GameId);
            if (success)
            {
                MyMainLayout.UpdateActiveGameId(gameId);
                await UpdateGameActiveStateForLeaveWarning(true);
            }
        }

        // Handler für Züge vom Schachbrett.
        private async Task HandlePlayerMove(MoveDto clientMove)
        {
            await GameOrchestrationService.ProcessPlayerMoveAsync(clientMove);
        }

        // Handler für Klicks auf Felder im Kartenmodus.
        private async Task HandleSquareClickForCard(string algebraicCoord)
        {
            await GameOrchestrationService.HandleSquareClickForCardAsync(algebraicCoord);
        }

        // Handler für die Figurenauswahl (Bauernumwandlung oder Wiedergeburt).
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

        // Handler für die Kartenaktivierung aus dem Info-Panel.
        private async Task HandleActivateCardFromModal(CardDto cardToActivate)
        {
            ModalState.CloseCardInfoPanelModal();
            await GameOrchestrationService.ActivateCardAsync(cardToActivate);
        }

        // Handler für das Schliessen/Abbrechen des Karten-Info-Panels.
        private void HandleCloseCardInfoModal()
        {
            if (!ModalState.IsCardInInfoPanelModalPreviewOnly)
            {
                CardState.ResetCardActivationState(true, "Aktivierung abgebrochen.");
            }
            ModalState.CloseCardInfoPanelModal();
        }

        // Handler für die Auswahl einer Karte aus der Historie (zur Vorschau).
        private void HandlePlayedCardSelected(CardDto card)
        {
            CardState.SelectCardForInfoPanel(card, true);
        }

        // Handler, die nach Abschluss von Animationen aufgerufen werden.
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

        // Einfache Handler zum Schliessen von Modals.
        private void CloseCreateGameModal() => ModalState.CloseCreateGameModal();
        private void CloseJoinGameModal() => ModalState.CloseJoinGameModal();
        private void HandlePieceSelectionModalCancelled() => CardState.ResetCardActivationState(true, "Auswahl abgebrochen.");
        private void CloseWinLossModal() => GameCoreState.ClearEndGameMessage();

        #endregion

        #region UI State Logic

        // Berechnet den aktuellen Interaktivitäts-Zustand des Bretts.
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

        // Bestimmt basierend auf dem 'CurrentBoardState', ob das Schachbrett klickbar ist.
        private bool IsChessboardEnabled()
        {
            var state = CurrentBoardState;
            return state is BoardInteractivityState.MyTurn or BoardInteractivityState.AwaitingCardSelection;
        }

        // Hilfsmethoden, um Daten an die ChessBoard-Komponente zu übergeben.
        private bool IsBoardInCardSelectionMode() => CardState.IsCardActivationPending && CardState.ActiveCardForBoardSelection != null && CardState.ActiveCardForBoardSelection.Id is CardConstants.Teleport or CardConstants.Positionstausch or CardConstants.Wiedergeburt or CardConstants.SacrificeEffect;
        private Player? GetPlayerColorForCardPieceSelection() => (CardState.IsCardActivationPending && CardState.ActiveCardForBoardSelection?.Id is CardConstants.Teleport or CardConstants.Positionstausch && string.IsNullOrEmpty(CardState.FirstSquareSelectedForTeleportOrSwap)) ? GameCoreState.MyColor : null;
        private string? GetFirstSelectedSquareForCardEffect() => CardState.FirstSquareSelectedForTeleportOrSwap;

        // Schaltet die Sichtbarkeit der Kartenhistorie auf Mobilgeräten um.
        private void ToggleMobilePlayedCardsHistory() => _showMobilePlayedCardsHistory = !_showMobilePlayedCardsHistory;

        // Startet den Prozess für ein neues Spiel aus dem Endspiel-Modal heraus.
        private void StartNewGameFromEndGame()
        {
            GameCoreState.ResetForNewGame();
            HighlightState.ClearAllActionHighlights();
            CardState.SetInitialHand(new InitialHandDto(new(), 0));

            if (MyMainLayout != null)
            {
                MyMainLayout.UpdateActiveGameId(Guid.Empty);
            }

            ModalService.RequestShowCreateGameModal();
            StateHasChanged();
        }

        #endregion

        #region State Management & Cleanup

        // Wrapper um base.StateHasChanged, um sicherzustellen, dass es im UI-Thread ausgeführt wird.
        private new void StateHasChanged() => InvokeAsync(base.StateHasChanged);

        // Abonniert die StateChanged-Events aller State-Container.
        private void SubscribeToStateChanges()
        {
            UiState.StateChanged += StateHasChanged;
            ModalState.StateChanged += StateHasChanged;
            GameCoreState.StateChanged += StateHasChanged;
            HighlightState.StateChanged += StateHasChanged;
            AnimationState.StateChanged += StateHasChanged;
            CardState.StateChanged += StateHasChanged;
        }

        // Deregistriert alle StateChanged-Events, um Memory Leaks zu vermeiden.
        private void UnsubscribeFromStateChanges()
        {
            UiState.StateChanged -= StateHasChanged;
            ModalState.StateChanged -= StateHasChanged;
            GameCoreState.StateChanged -= StateHasChanged;
            HighlightState.StateChanged -= StateHasChanged;
            AnimationState.StateChanged -= StateHasChanged;
            CardState.StateChanged -= StateHasChanged;
        }

        // Aktualisiert den Zustand für die "Seite verlassen?"-Warnung.
        private async Task UpdateGameActiveStateForLeaveWarning(bool isActive)
        {
            if (_isTutorialRunning) return;
            if (_isGameActiveForLeaveWarning == isActive) return;
            _isGameActiveForLeaveWarning = isActive;
            await JSRuntime.InvokeVoidAsync("navigationInterop.setGameActiveState", isActive);
        }

        // Löst den Download des Spielverlaufs aus.
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

        // Räumt alle Ressourcen, Events und JS-Interop-Handler auf, wenn die Komponente zerstört wird.
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
        #endregion
    }
}