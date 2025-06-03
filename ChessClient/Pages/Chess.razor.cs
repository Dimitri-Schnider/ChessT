using Chess.Logging;
using ChessClient.Configuration;
using ChessClient.Extensions;
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
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System; 
using System.Threading.Tasks; 

namespace ChessClient.Pages
{
    public partial class Chess : IAsyncDisposable
    {
        [Inject] private IConfiguration Configuration { get; set; } = default!;
        [Inject] private IGameSession Game { get; set; } = default!;
        [Inject] private ChessHubService HubService { get; set; } = default!;
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
        [Inject] private IChessLogger Logger { get; set; } = default!;
        public PlayerDto? CurrentPlayerInfo => GameCoreState?.CurrentPlayerInfo;
        public BoardDto? BoardDtoProp => GameCoreState?.BoardDto;
        public Player MyColor => GameCoreState?.MyColor ?? Player.White;
        private string InviteLink => GameCoreState?.GameId == Guid.Empty || GameCoreState == null ? "" : $"{NavManager.BaseUri}chess?gameId={GameCoreState.GameId}";
        private bool _isExtraTurnSequenceActive;
        private int _extraTurnMovesMade;
        private CardDto? _lastActivatedCardForGenericAnimation;
        private CardSwapAnimationDetailsDto? _pendingSwapAnimationDetails;

        private PieceType? _pieceTypeSelectedForRebirth;
        private bool _isAwaitingRebirthTargetSquareSelection;
        private string? _firstSquareSelectedForTeleportOrSwap;
        private bool _isAwaitingTurnConfirmationAfterCard;
        private bool _showMobilePlayedCardsHistory;
        private bool _isAwaitingSacrificePawnSelection;

        private CardDto? _activeCardForBoardSelectionProcess;
        private void ToggleMobilePlayedCardsHistory()
        {
            _showMobilePlayedCardsHistory = !_showMobilePlayedCardsHistory;
            InvokeAsync(StateHasChanged); 
        }

        private void ComponentStateChanged()
        {
            InvokeAsync(StateHasChanged);
        }

        protected override async Task OnInitializedAsync()
        {
            // Strikte Überprüfung kritischer DI-Dienste
            // Wenn einer dieser Dienste null ist, kann die Komponente nicht korrekt funktionieren.
            // Eine Exception hier hilft, Konfigurations- oder Initialisierungsfehler frühzeitig zu erkennen.
            if (Configuration == null!) throw new InvalidOperationException($"Dienst {nameof(Configuration)} nicht injiziert.");
            if (Game == null!) throw new InvalidOperationException($"Dienst {nameof(Game)} nicht injiziert.");
            if (HubService == null!) throw new InvalidOperationException($"Dienst {nameof(HubService)} nicht injiziert.");
            if (NavManager == null!) throw new InvalidOperationException($"Dienst {nameof(NavManager)} nicht injiziert.");
            if (JSRuntime == null!) throw new InvalidOperationException($"Dienst {nameof(JSRuntime)} nicht injiziert.");
            if (ModalService == null!) throw new InvalidOperationException($"Dienst {nameof(ModalService)} nicht injiziert.");
            if (MyMainLayout == null!) throw new InvalidOperationException($"Kaskadierender Parameter {nameof(MyMainLayout)} nicht bereitgestellt.");
            if (UiState == null!) throw new InvalidOperationException($"Dienst {nameof(UiState)} nicht injiziert.");
            if (ModalState == null!) throw new InvalidOperationException($"Dienst {nameof(ModalState)} nicht injiziert.");
            if (GameCoreState == null!) throw new InvalidOperationException($"Dienst {nameof(GameCoreState)} nicht injiziert.");
            if (HighlightState == null!) throw new InvalidOperationException($"Dienst {nameof(HighlightState)} nicht injiziert.");
            if (AnimationState == null!) throw new InvalidOperationException($"Dienst {nameof(AnimationState)} nicht injiziert.");
            if (CardState == null!) throw new InvalidOperationException($"Dienst {nameof(CardState)} nicht injiziert.");
            if (GameOrchestrationService == null!) throw new InvalidOperationException($"Dienst {nameof(GameOrchestrationService)} nicht injiziert.");
            if (Logger == null!) throw new InvalidOperationException($"Dienst {nameof(Logger)} nicht injiziert.");

            SubscribeToStateChanges();
            HubService.OnTurnChanged += HandleHubTurnChanged;
            HubService.OnTimeUpdate += HandleHubTimeUpdate;
            HubService.OnPlayerJoined += HandlePlayerJoinedClient;
            HubService.OnPlayerLeft += HandlePlayerLeftClient;
            HubService.OnPlayCardActivationAnimation += HandlePlayCardActivationAnimation;
            HubService.OnReceiveInitialHand += HandleReceiveInitialHand;
            HubService.OnCardAddedToHand += HandleCardAddedToHand;
            HubService.OnUpdateHandContents += HandleUpdateHandContents;
            HubService.OnPlayerEarnedCardDraw += HandlePlayerEarnedCardDrawNotification;
            HubService.OnReceiveCardSwapAnimationDetails += HandleReceiveCardSwapAnimationDetails;

            ModalService.ShowCreateGameModalRequested += OpenCreateGameModalHandler;
            ModalService.ShowJoinGameModalRequested += OpenJoinGameModalHandler;
            RegisterCardEventHandlers();
            await InitializePageBasedOnUrl();
        }

        private void HandleReceiveInitialHand(InitialHandDto initialHandDto)
        {
            // Die Prüfung auf null für CardState wurde durch die strikte Prüfung in OnInitializedAsync abgedeckt.
            // Es wird davon ausgegangen, dass CardState hier nicht null sein kann.
            CardState.SetInitialHand(initialHandDto);
        }

        private void HandleUpdateHandContents(InitialHandDto newHandInfo)
        {
            // Strikte Prüfung in OnInitializedAsync, daher hier keine explizite Service-Null-Prüfung mehr.
            // Es wird davon ausgegangen, dass CardState und UiState hier nicht null sein können.
            CardState.UpdateHandAndDrawPile(newHandInfo);
            _ = UiState.SetCurrentInfoMessageForBoxAsync("Deine Handkarten wurden aktualisiert.", true, 3000);
        }

        private void HandleCardAddedToHand(CardDto drawnCard, int newDrawPileCount)
        {
            // Strikte Prüfung in OnInitializedAsync.
            CardState.AddReceivedCardToHand(drawnCard, newDrawPileCount);
            if (drawnCard != null && !drawnCard.Name.Contains(CardConstants.NoMoreCardsName) && !drawnCard.Name.Contains(CardConstants.ReplacementCardName))
            {
                _ = UiState.SetCurrentInfoMessageForBoxAsync($"Neue Karte '{drawnCard.Name}' erhalten!", true, 3000);
            }
            else if (drawnCard != null)
            {
                _ = UiState.SetCurrentInfoMessageForBoxAsync(drawnCard.Description, true, 5000);
            }
        }

        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();
        }

        private async Task InitializePageBasedOnUrl()
        {
            // Strikte Prüfung in OnInitializedAsync.
            GameCoreState.SetGameSpecificDataInitialized(false);
            Uri uri = NavManager.ToAbsoluteUri(NavManager.Uri);
            string? gameIdFromQuery = null;
            if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("gameId", out Microsoft.Extensions.Primitives.StringValues id))
            {
                gameIdFromQuery = id.ToString();
            }

            GameCoreState.SetGameIdFromQuery(gameIdFromQuery, false);
            if (!string.IsNullOrWhiteSpace(gameIdFromQuery))
            {
                if (Guid.TryParse(gameIdFromQuery, out Guid parsedGuidFromQuery))
                {
                    try
                    {
                        GameInfoDto info = await Game.GetGameInfoAsync(parsedGuidFromQuery);
                        GameCoreState.SetGameIdFromQuery(gameIdFromQuery, true);
                        TimeUpdateDto timeUpdate = await Game.GetTimeUpdateAsync(parsedGuidFromQuery);
                        GameCoreState.UpdateDisplayedTimes(timeUpdate.WhiteTime, timeUpdate.BlackTime, timeUpdate.PlayerWhoseTurnItIs);
                        // MyMainLayout wird in OnInitializedAsync geprüft.
                        MyMainLayout.UpdateActiveGameId(GameCoreState.GameId != Guid.Empty ? GameCoreState.GameId : parsedGuidFromQuery);
                        ModalState.OpenJoinGameModal(parsedGuidFromQuery.ToString());
                        if (info.HasOpponent)
                        {
                            GameCoreState.SetOpponentJoined(true);
                            await UpdatePlayerNames();
                        }
                    }
                    catch (Exception ex)
                    {
                        UiState.SetErrorMessage(string.Format(CultureInfo.CurrentCulture, "Spiel mit ID '{0}' konnte nicht geladen werden: {1}", gameIdFromQuery, ex.Message));
                        GameCoreState.SetGameIdFromQuery(gameIdFromQuery, false);
                        MyMainLayout.UpdateActiveGameId(Guid.Empty);
                        Logger.LogClientSignalRConnectionWarning($"Fehler beim Laden von Spiel {gameIdFromQuery}: {ex.Message}");
                    }
                }
                else
                {
                    UiState.SetErrorMessage("Die Game-ID in der URL ist ungueltig.");
                    MyMainLayout.UpdateActiveGameId(Guid.Empty);
                }
            }
            else if (GameCoreState.CurrentPlayerInfo == null) // Statt GameCoreState == null
            {
                ModalState.OpenCreateGameModal();
                MyMainLayout.UpdateActiveGameId(Guid.Empty);
            }

            if (GameCoreState.CurrentPlayerInfo != null && GameCoreState.GameId != Guid.Empty && !GameCoreState.IsGameSpecificDataInitialized)
            {
                await LoadInitialBoardAndStatusAndTime();
            }
        }
        private void SubscribeToStateChanges()
        {
            // Die State-Objekte selbst werden in OnInitializedAsync auf null geprüft.
            UiState.StateChanged += ComponentStateChanged;
            ModalState.StateChanged += ComponentStateChanged;
            GameCoreState.StateChanged += ComponentStateChanged;
            HighlightState.StateChanged += ComponentStateChanged;
            AnimationState.StateChanged += ComponentStateChanged;
            CardState.StateChanged += ComponentStateChanged;
        }

        private void UnsubscribeFromStateChanges()
        {
            // Die State-Objekte selbst werden in OnInitializedAsync auf null geprüft und sollten hier nicht null sein, wenn DisposeAsync korrekt aufgerufen wird.
            UiState.StateChanged -= ComponentStateChanged;
            ModalState.StateChanged -= ComponentStateChanged;
            GameCoreState.StateChanged -= ComponentStateChanged;
            HighlightState.StateChanged -= ComponentStateChanged;
            AnimationState.StateChanged -= ComponentStateChanged;
            CardState.StateChanged -= ComponentStateChanged;
        }

        protected bool IsChessboardEnabled()
        {
            if (ModalState == null || CardState == null || GameCoreState == null || HighlightState == null) return false;
            if (_isAwaitingTurnConfirmationAfterCard) return false;

            if (ModalState.ShowCreateGameModal || ModalState.ShowJoinGameModal ||
                ModalState.ShowPieceSelectionModal || ModalState.ShowCardInfoPanelModal) return false;

            if (CardState.IsCardActivationPending && _activeCardForBoardSelectionProcess != null)
            {
                string cardId = _activeCardForBoardSelectionProcess.Id;
                if (cardId == CardConstants.Teleport || cardId == CardConstants.Positionstausch ||
                   (cardId == CardConstants.Wiedergeburt && _isAwaitingRebirthTargetSquareSelection))
                {
                    return true;
                }
            }

            return GameCoreState.OpponentJoined &&
                   GameCoreState.MyColor == GameCoreState.CurrentTurnPlayer &&
                   string.IsNullOrEmpty(GameCoreState.EndGameMessage) &&
                   !CardState.IsCardActivationPending;
        }

        protected bool IsCardActivatable(CardDto? card)
        {
            if (card == null || CardState == null || GameCoreState == null || ModalState == null) return false;
            if (ModalState.ShowPieceSelectionModal) return false;

            if (CardState.IsCardActivationPending && CardState.SelectedCardInstanceIdInHand != null && CardState.SelectedCardInstanceIdInHand != card.InstanceId)
            {
                return false;
            }

            if (GameCoreState.CurrentPlayerInfo == null ||
                !GameCoreState.OpponentJoined ||
                GameCoreState.MyColor != GameCoreState.CurrentTurnPlayer ||
                !string.IsNullOrEmpty(GameCoreState.EndGameMessage))
            {
                return false;
            }

            if (card.Id == CardConstants.SubtractTime)
            {
                if (GameCoreState.CurrentPlayerInfo == null) return false;
                Player opponentColor = GameCoreState.MyColor.Opponent();
                string opponentTimeDisplay = opponentColor == Player.White ? GameCoreState.WhiteTimeDisplay : GameCoreState.BlackTimeDisplay;
                if (TimeSpan.TryParseExact(opponentTimeDisplay, @"mm\:ss", CultureInfo.InvariantCulture, out TimeSpan opponentTime))
                {
                    if (opponentTime < TimeSpan.FromMinutes(3))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            if (card.Id == CardConstants.CardSwap)
            {
                if (CardState.PlayerHandCards == null || !CardState.PlayerHandCards.Any(c => c.InstanceId != card.InstanceId))
                {
                    return false;
                }
            }
            return true;
        }

        private void HandlePlayCardActivationAnimation(CardDto cardForAnimation, Guid playerIdActivating, Player playerColorActivating)
        {
            Logger.LogHandlePlayCardActivationAnimation(cardForAnimation.Id, playerIdActivating, playerColorActivating);
            // Annahme: GameCoreState, AnimationState, UiState, Logger sind nach OnInitializedAsync nicht null.

            if (cardForAnimation != null && GameCoreState.CurrentPlayerInfo != null)
            {
                _lastActivatedCardForGenericAnimation = cardForAnimation;
                AnimationState.StartCardActivationAnimation(cardForAnimation, GameCoreState.MyColor == playerColorActivating);
                _ = UiState.SetCurrentInfoMessageForBoxAsync($"Karte '{cardForAnimation.Name}' wird aktiviert...");
                Logger.LogGenericCardAnimationStartedForCard(cardForAnimation.Name);
            }
            else
            {
                // Diese Log-Meldung bezieht sich nun eher auf fehlende *Daten* als auf fehlende *Dienste*.
                Logger.LogClientCriticalServicesNullOnInit($"HandlePlayCardActivationAnimation: cardForAnimation oder CurrentPlayerInfo ist null. cardForAnimationIsNull: {cardForAnimation == null}, playerInfoIsNull: {GameCoreState.CurrentPlayerInfo == null}");
            }
        }

        private void HandleClientAnimationFinished()
        {
            string? lastCardId = _lastActivatedCardForGenericAnimation?.Id;
            bool wasSwapCard = lastCardId == CardConstants.CardSwap;
            Logger.LogHandleClientAnimationFinishedTriggered(lastCardId, _pendingSwapAnimationDetails == null);
            AnimationState?.FinishCardActivationAnimation();
            if (wasSwapCard && _pendingSwapAnimationDetails != null && AnimationState != null)
            {
                Logger.LogActuallyStartingSpecificSwapAnim(_pendingSwapAnimationDetails.CardGiven.Name, _pendingSwapAnimationDetails.CardReceived.Name);
                AnimationState.StartCardSwapAnimation(_pendingSwapAnimationDetails.CardGiven, _pendingSwapAnimationDetails.CardReceived);
                _pendingSwapAnimationDetails = null;
            }
            else if (wasSwapCard && _pendingSwapAnimationDetails == null)
            {
                Logger.LogClientSignalRConnectionWarning("Generische Animation für Kartentausch beendet, aber keine _pendingSwapAnimationDetails gefunden. Spezifische Tauschanimation wird nicht gestartet.");
            }

            _lastActivatedCardForGenericAnimation = null;
            InvokeAsync(StateHasChanged);
        }

        private void HandleSpecificCardSwapAnimationFinished()
        {
            AnimationState?.FinishCardSwapAnimation();
            Logger.LogSpecificCardSwapAnimationFinishedClient();
            InvokeAsync(StateHasChanged);
        }

        private async Task InitializeGameRelatedData()
        {
            if (GameCoreState == null || CardState == null || HighlightState == null || Game == null) return;
            if (GameCoreState.IsGameSpecificDataInitialized) return;

            await UpdatePlayerNames();
            if (GameCoreState.GameId != Guid.Empty)
            {
                TimeUpdateDto initialTimeUpdate = await Game.GetTimeUpdateAsync(GameCoreState.GameId);
                if (initialTimeUpdate != null)
                {
                    GameCoreState.UpdateDisplayedTimes(initialTimeUpdate.WhiteTime, initialTimeUpdate.BlackTime, initialTimeUpdate.PlayerWhoseTurnItIs);
                }
            }
            MyMainLayout?.SetCanDownloadGameHistory(GameCoreState.OpponentJoined && GameCoreState.BoardDto != null);
            GameCoreState.SetGameSpecificDataInitialized(true);
            _isExtraTurnSequenceActive = false;
            _extraTurnMovesMade = 0;
        }

        private void OpenCreateGameModalHandler() => ModalState?.OpenCreateGameModal();
        private void CloseCreateGameModal() => ModalState?.CloseCreateGameModal();
        private async Task SubmitCreateGame(CreateGameParameters args)
        {
            if (GameOrchestrationService == null || UiState == null || ModalState == null) return;
            await TriggerCreateNewGame(args.Name, args.Color, args.TimeMinutes);
            if (string.IsNullOrEmpty(UiState.ErrorMessage))
            {
                ModalState.CloseCreateGameModal();
            }
        }

        private void OpenJoinGameModalHandler()
        {
            if (GameCoreState == null || ModalState == null) return;
            string? initialId = null;
            if (GameCoreState.IsGameIdFromQueryValidAndExists)
            {
                Uri uri = NavManager.ToAbsoluteUri(NavManager.Uri);
                if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("gameId", out Microsoft.Extensions.Primitives.StringValues id))
                {
                    initialId = id.ToString();
                }
            }
            ModalState.OpenJoinGameModal(initialId ?? ModalState.GameIdInputForJoinModal);
        }
        private void CloseJoinGameModal() => ModalState?.CloseJoinGameModal();
        private async Task SubmitJoinGame(JoinGameParameters args)
        {
            if (GameOrchestrationService == null || UiState == null || ModalState == null) return;
            await TriggerJoinExistingGame(args.Name, args.GameId);
            if (string.IsNullOrEmpty(UiState.ErrorMessage))
            {
                ModalState.CloseJoinGameModal();
            }
        }

        public async Task TriggerCreateNewGame(string name, Player color, int time)
        {
            if (GameOrchestrationService == null || ModalState == null || GameCoreState == null || HubService == null) return;
            (bool success, Guid gameId) = await GameOrchestrationService.CreateNewGameAsync(name, color, time);
            if (success && GameCoreState.CurrentPlayerInfo != null)
            {
                MyMainLayout?.UpdateActiveGameId(gameId);
                await ResetMessagesAndTimersAsync(TimeSpan.FromMinutes(time));
                await InitializeGameRelatedData();
                await InitializeSignalRConnection();
                if (HubService.IsConnected)
                {
                    await HubService.RegisterPlayerWithHubAsync(gameId, GameCoreState.CurrentPlayerInfo.Id);
                }
                else
                {
                    Logger.LogClientSignalRConnectionWarning("SignalR nicht verbunden nach InitializeSignalRConnection in TriggerCreateNewGame. PlayerId-Registrierung und Starthand-Empfang verzögert.");
                }

                if (!GameCoreState.OpponentJoined && gameId != Guid.Empty)
                {
                    ModalState.OpenInviteLinkModal(InviteLink);
                }
            }
            else
            {
                MyMainLayout?.UpdateActiveGameId(Guid.Empty);
            }
        }

        public async Task TriggerJoinExistingGame(string name, string gameIdToJoin)
        {
            if (GameOrchestrationService == null || HubService == null || GameCoreState == null) return;
            (bool success, Guid gameId) = await GameOrchestrationService.JoinExistingGameAsync(name, gameIdToJoin);
            if (success && GameCoreState.CurrentPlayerInfo != null)
            {
                MyMainLayout?.UpdateActiveGameId(gameId);
                await ResetMessagesAndTimersAsync();
                await InitializeGameRelatedData();
                await InitializeSignalRConnection();
                if (HubService.IsConnected)
                {
                    await HubService.RegisterPlayerWithHubAsync(gameId, GameCoreState.CurrentPlayerInfo.Id);
                }
                else
                {
                    Logger.LogClientSignalRConnectionWarning("SignalR nicht verbunden nach InitializeSignalRConnection in TriggerJoinExistingGame. PlayerId-Registrierung und Starthand-Empfang verzögert.");
                }
            }
            else
            {
                MyMainLayout?.UpdateActiveGameId(Guid.Empty);
            }
        }

        private async void HandlePlayerLeftClient(string playerNameLeaving, int playerCount)
        {
            if (GameCoreState == null || UiState == null) return;
            if (playerCount < 2)
            {
                GameCoreState.SetOpponentJoined(false);
                _ = UiState.SetCurrentInfoMessageForBoxAsync(string.Format(CultureInfo.CurrentCulture, "Spieler '{0}' hat das Spiel verlassen.", playerNameLeaving));
                if (string.IsNullOrEmpty(GameCoreState.EndGameMessage))
                {
                    GameCoreState.SetEndGameMessage(string.Format(CultureInfo.CurrentCulture, "Spieler '{0}' hat aufgegeben. Du gewinnst!", playerNameLeaving));
                }
                MyMainLayout?.SetCanDownloadGameHistory(false);
            }
            KeyValuePair<Player, string> playerToRemoveEntry = GameCoreState.PlayerNames.FirstOrDefault(kvp => kvp.Value == playerNameLeaving);
            if (playerToRemoveEntry.Key != Player.None)
            {
                Dictionary<Player, string> updatedNames = new(GameCoreState.PlayerNames);
                updatedNames.Remove(playerToRemoveEntry.Key);
                GameCoreState.UpdatePlayerNames(updatedNames);
            }
            await UpdatePlayerNames();
        }

        private async Task UpdatePlayerNames()
        {
            if (GameCoreState == null) return;
            bool changed = false;

            if (GameCoreState.CurrentPlayerInfo != null)
            {
                string? currentOwnName = GameCoreState.PlayerNames.GetValueOrDefault(GameCoreState.MyColor);
                if (currentOwnName != GameCoreState.CurrentPlayerInfo.Name)
                {
                    GameCoreState.SetPlayerName(GameCoreState.MyColor, GameCoreState.CurrentPlayerInfo.Name);
                    changed = true;
                }
            }

            if (GameCoreState.GameId != Guid.Empty && GameCoreState.CurrentPlayerInfo != null && GameCoreState.OpponentJoined)
            {
                Player opponentColor = GameCoreState.MyColor.Opponent();
                string? currentOpponentName = GameCoreState.PlayerNames.GetValueOrDefault(opponentColor);

                if (string.IsNullOrEmpty(currentOpponentName))
                {
                    try
                    {
                        OpponentInfoDto? opponentInfo = await Game.GetOpponentInfoAsync(GameCoreState.GameId, GameCoreState.CurrentPlayerInfo.Id);
                        if (opponentInfo != null && opponentInfo.OpponentColor == opponentColor)
                        {
                            GameCoreState.SetPlayerName(opponentColor, opponentInfo.OpponentName);
                            changed = true;
                        }
                        else if (opponentInfo != null)
                        {
                            Logger.LogUpdatePlayerNamesMismatch(opponentInfo.OpponentColor, opponentColor);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex is HttpRequestException httpEx && httpEx.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            Logger.LogUpdatePlayerNamesNotFound(ex.Message);
                        }
                        else
                        {
                            Logger.LogUpdatePlayerNamesError(ex);
                        }
                    }
                }
            }

            if (changed)
            {
                await InvokeAsync(StateHasChanged);
            }
        }

        private async void HandlePlayerJoinedClient(string playerNameJoining, int playerCount)
        {
            if (GameCoreState == null || UiState == null) return;
            if (playerCount == 2)
            {
                GameCoreState.SetOpponentJoined(true);
                _ = UiState.SetCurrentInfoMessageForBoxAsync(string.Format(CultureInfo.CurrentCulture, "Spieler '{0}' ist beigetreten. Das Spiel kann beginnen!", playerNameJoining));
                MyMainLayout?.SetCanDownloadGameHistory(true);
            }
            else
            {
                _ = UiState.SetCurrentInfoMessageForBoxAsync(string.Format(CultureInfo.CurrentCulture, "Spieler '{0}' ist beigetreten. Warte auf weiteren Spieler...", playerNameJoining));
            }
            await UpdatePlayerNames();
        }

        private async Task InitializeSignalRConnection()
        {
            if (Configuration == null || UiState == null || GameCoreState == null || HubService == null)
            {
                UiState?.SetErrorMessage("Kritische Komponenten nicht initialisiert für SignalR.");
                Logger.LogClientCriticalServicesNullOnInit("InitializeSignalRConnection");
                return;
            }

            if (GameCoreState.GameId == Guid.Empty || GameCoreState.CurrentPlayerInfo == null) return;

            if (!HubService.IsConnected)
            {
                UiState.SetIsConnecting(true);
                try
                {
                    string? serverBaseUrlFromConfig = Configuration.GetValue<string>("ServerBaseUrl");
                    string serverBaseUrl = ClientConstants.DefaultServerBaseUrl;
                    if (!string.IsNullOrEmpty(serverBaseUrlFromConfig))
                    {
                        serverBaseUrl = serverBaseUrlFromConfig;
                    }

                    if (string.IsNullOrEmpty(serverBaseUrl) || GameCoreState.CurrentPlayerInfo == null)
                    {
                        UiState.SetErrorMessage("Kritischer Fehler: Server-URL oder Spielerdaten nicht verfügbar vor Hub-Join.");
                        Logger.LogClientCriticalServicesNullOnInit("ServerBaseUrl oder CurrentPlayerInfo in InitializeSignalRConnection");
                        UiState.SetIsConnecting(false); return;
                    }

                    await HubService.StartAsync($"{serverBaseUrl.TrimEnd('/')}{ClientConstants.ChessHubRelativePath}");
                    UiState.SetIsConnecting(false);
                    if (!GameCoreState.IsGameSpecificDataInitialized)
                    {
                        await LoadInitialBoardAndStatusAndTime();
                    }
                }
                catch (Exception ex)
                {
                    UiState.SetErrorMessage(string.Format(CultureInfo.CurrentCulture, "Fehler beim Verbinden mit SignalR: {0}", ex.ToString()));
                    Logger.LogClientSignalRConnectionWarning($"Fehler beim Verbinden mit SignalR: {ex}");
                    UiState.SetIsConnecting(false);
                }
            }
        }

        private async Task LoadInitialBoardAndStatusAndTime()
        {
            // Annahme: GameCoreState, UiState, Game, MyMainLayout sind nach OnInitializedAsync nicht null.
            if (GameCoreState.CurrentPlayerInfo == null || GameCoreState.GameId == Guid.Empty) return;
            if (GameCoreState.IsGameSpecificDataInitialized && GameCoreState.BoardDto != null) return;

            try
            {
                BoardDto board = await Game.GetBoardAsync(GameCoreState.GameId);
                GameCoreState.UpdateBoard(board); // GameCoreState ist nicht null
                GameStatusDto status = await Game.GetGameStatusAsync(GameCoreState.GameId, GameCoreState.CurrentPlayerInfo.Id);
                await ProcessGameStatusAsync(status, false);
                Player currentTurn = await Game.GetCurrentTurnPlayerAsync(GameCoreState.GameId);
                GameCoreState.SetCurrentTurnPlayer(currentTurn);
                GameInfoDto gameInfo = await Game.GetGameInfoAsync(GameCoreState.GameId);
                GameCoreState.SetOpponentJoined(gameInfo.HasOpponent);
                MyMainLayout.UpdateActiveGameId(GameCoreState.GameId);
                MyMainLayout.SetCanDownloadGameHistory(GameCoreState.OpponentJoined && GameCoreState.BoardDto != null);
                await InitializeGameRelatedData();
            }
            catch (Exception ex)
            {
                UiState.SetErrorMessage($"Fehler beim Laden des Spielstands/Zeit: {ex.Message}");
                Logger.LogClientSignalRConnectionWarning($"Fehler beim Laden des Spielstands/Zeit: {ex.Message}");
                MyMainLayout.UpdateActiveGameId(Guid.Empty);
            }
        }

        private void HandleHubTurnChanged(BoardDto newBoard, Player nextPlayer, GameStatusDto statusForNextPlayer, string? lastMoveFromServerFrom, string? lastMoveFromServerTo, List<AffectedSquareInfo>? cardEffectSquaresFromServer)
        {
            if (GameCoreState == null || HighlightState == null || CardState == null || AnimationState == null || UiState == null || ModalState == null) return;
            if (_isAwaitingTurnConfirmationAfterCard)
            {
                _isAwaitingTurnConfirmationAfterCard = false;
                Logger.LogClientSignalRConnectionWarning("[ChessPage] Server turn confirmation received, resetting _isAwaitingTurnConfirmationAfterCard flag.");
            }

            Player playerWhoseTurnItWas = GameCoreState.CurrentTurnPlayer ?? Player.None;

            GameCoreState.UpdateBoard(newBoard);
            GameCoreState.SetCurrentTurnPlayer(nextPlayer);

            bool highlightLogicWasHandledByExtraTurn = false;
            if (_isExtraTurnSequenceActive && GameCoreState.CurrentPlayerInfo != null && GameCoreState.MyColor == playerWhoseTurnItWas)
            {
                _extraTurnMovesMade++;
                bool isThisTheThirdMoveOverallByMe = (_extraTurnMovesMade == 2);
                HighlightState.SetHighlights(lastMoveFromServerFrom, lastMoveFromServerTo, true, isThisTheThirdMoveOverallByMe);
                highlightLogicWasHandledByExtraTurn = true;
                if (isThisTheThirdMoveOverallByMe || GameCoreState.MyColor != nextPlayer)
                {
                    _isExtraTurnSequenceActive = false;
                }
            }

            if (!highlightLogicWasHandledByExtraTurn)
            {
                // *** BEGINN DER ÄNDERUNG FÜR BUG 2 (HIGHLIGHTS FÜR KARTENEFFEKTE) ***
                if (cardEffectSquaresFromServer != null && cardEffectSquaresFromServer.Count > 0)
                {
                    // Wenn Karteneffekte vorhanden sind, diese spezifisch mit SetHighlightForCardEffect behandeln,
                    // damit SquareComponent die korrekten CSS-Klassen für Karteneffekte verwendet.
                    HighlightState.SetHighlightForCardEffect(
                        cardEffectSquaresFromServer.Select(eff => (eff.Square, eff.Type)).ToList()
                    );
                }
                else
                {
                    // Andernfalls Standard-Zughighlight anwenden.
                    HighlightState.SetHighlights(lastMoveFromServerFrom, lastMoveFromServerTo, false);
                }
                // *** ENDE DER ÄNDERUNG FÜR BUG 2 ***
                _isExtraTurnSequenceActive = false; // Sicherstellen, dass dies zurückgesetzt wird, wenn keine Extrazug-Sequenz mehr aktiv ist.
            }

            bool isModalInteractionPendingForRebirth = ModalState.ShowPieceSelectionModal && _activeCardForBoardSelectionProcess?.Id == CardConstants.Wiedergeburt;
            if (CardState.IsCardActivationPending && !isModalInteractionPendingForRebirth)
            {
                _ = ResetCardActivationStateAsync(fromCancelFlow: true, specificMessageToKeep: "Zug gewechselt, Kartenauswahl abgebrochen.");
            }

            CardState.DeselectActiveHandCard();
            if (GameCoreState.CurrentPlayerInfo != null)
            {
                GameCoreState.SetOpponentJoined(true);
                MyMainLayout?.SetCanDownloadGameHistory(true);
            }

            bool amIMatt = false;
            if (statusForNextPlayer == GameStatusDto.Checkmate)
            {
                if (GameCoreState.CurrentPlayerInfo != null && GameCoreState.MyColor == nextPlayer) { GameCoreState.SetEndGameMessage("Schachmatt! Du hast verloren."); amIMatt = true; }
                else if (GameCoreState.CurrentPlayerInfo != null && GameCoreState.MyColor != nextPlayer) { GameCoreState.SetEndGameMessage("Schachmatt! Du hast gewonnen!"); }
                else { GameCoreState.SetEndGameMessage($"Schachmatt! {nextPlayer.Opponent()} gewinnt."); }
            }
            else if (statusForNextPlayer == GameStatusDto.TimeOut)
            {
                if (GameCoreState.CurrentPlayerInfo != null && GameCoreState.MyColor == nextPlayer)
                {
                    GameCoreState.SetEndGameMessage("Zeit abgelaufen! Du hast gewonnen!");
                }
                else if (GameCoreState.CurrentPlayerInfo != null && GameCoreState.MyColor == nextPlayer.Opponent())
                {
                    GameCoreState.SetEndGameMessage("Zeit abgelaufen! Du hast verloren.");
                }
                else
                {
                    GameCoreState.SetEndGameMessage($"Zeit abgelaufen! {nextPlayer} gewinnt.");
                }
            }

            if (GameCoreState.CurrentPlayerInfo != null && GameCoreState.MyColor == nextPlayer && !amIMatt && string.IsNullOrEmpty(GameCoreState.EndGameMessage))
            {
                _ = ProcessGameStatusAsync(statusForNextPlayer, true);
            }
            else if (string.IsNullOrEmpty(GameCoreState.EndGameMessage))
            {
                _ = ProcessGameStatusAsync(statusForNextPlayer, false);
            }
            InvokeAsync(StateHasChanged);
        }

        private void HandleHubTimeUpdate(TimeUpdateDto timeUpdate)
        {
            if (GameCoreState == null) return;
            GameCoreState.UpdateDisplayedTimes(timeUpdate.WhiteTime, timeUpdate.BlackTime, timeUpdate.PlayerWhoseTurnItIs);
            if (timeUpdate.PlayerWhoseTurnItIs.HasValue)
            {
                GameCoreState.SetCurrentTurnPlayer(timeUpdate.PlayerWhoseTurnItIs.Value);
            }
        }

        private async Task ProcessGameStatusAsync(GameStatusDto status, bool isRelevantForMyCheckStatus)
        {
            if (UiState == null || GameCoreState == null) return;
            if (isRelevantForMyCheckStatus)
            {
                if (status == GameStatusDto.Check)
                {
                    await UiState.SetCurrentInfoMessageForBoxAsync("Du stehst im Schach!");
                }
                else if (UiState.CurrentInfoMessageForBox == "Du stehst im Schach!")
                {
                    UiState.ClearCurrentInfoMessageForBox();
                }
            }
            if (string.IsNullOrEmpty(GameCoreState.EndGameMessage))
            {
                if (status == GameStatusDto.Stalemate)
                {
                    GameCoreState.SetEndGameMessage("Patt! Unentschieden.");
                }
                else if (status is GameStatusDto.Draw50MoveRule or GameStatusDto.DrawInsufficientMaterial or GameStatusDto.DrawThreefoldRepetition)
                {
                    GameCoreState.SetEndGameMessage("Unentschieden!");
                }
            }
        }

        private void StartNewGameFromEndGame()
        {
            NavManager.NavigateTo(NavManager.Uri, forceLoad: true);
        }

        private async Task ResetMessagesAndTimersAsync(TimeSpan? initialTime = null)
        {
            if (UiState == null || GameCoreState == null || CardState == null || AnimationState == null || HighlightState == null || ModalState == null) return;
            UiState.ClearErrorMessage(); GameCoreState.ClearEndGameMessage();
            await ResetCardActivationStateAsync(fromCancelFlow: false);
            AnimationState.FinishCardActivationAnimation();
            GameCoreState.SetGameSpecificDataInitialized(false); CardState.ClearPlayedCardsHistory();
            HighlightState.ClearAllActionHighlights();
            _isExtraTurnSequenceActive = false; _extraTurnMovesMade = 0;
            TimeSpan timeToDisplay = initialTime ?? TimeSpan.FromMinutes(ModalState.SelectedInitialTimeMinutesForCreateModal);
            GameCoreState.UpdateDisplayedTimes(timeToDisplay, timeToDisplay, Player.White);
            GameCoreState.SetCurrentTurnPlayer(Player.White);
        }

        private async Task HandlePlayerMove(MoveDto clientMove)
        {
            if (GameCoreState == null || CardState == null || ModalState == null || UiState == null || GameOrchestrationService == null || HighlightState == null) return;
            if (ModalState.ShowPieceSelectionModal || ModalState.ShowCardInfoPanelModal) return;

            if (CardState.IsCardActivationPending && _activeCardForBoardSelectionProcess != null)
            {
                string cardId = _activeCardForBoardSelectionProcess.Id;
                if (cardId == CardConstants.Teleport || cardId == CardConstants.Positionstausch ||
                   (cardId == CardConstants.Wiedergeburt && _isAwaitingRebirthTargetSquareSelection))
                {
                    return;
                }
            }

            if (GameCoreState.CurrentPlayerInfo == null || GameCoreState.GameId == Guid.Empty ||
                !string.IsNullOrEmpty(GameCoreState.EndGameMessage) || CardState.IsCardActivationPending)
            {
                return;
            }

            if (!GameCoreState.OpponentJoined) { UiState.SetErrorMessage("Warte, bis dein Gegner beigetreten ist."); return; }
            if (GameCoreState.MyColor != GameCoreState.CurrentTurnPlayer) { UiState.SetErrorMessage("Nicht dein Zug."); return; }

            PlayerMoveProcessingResult result = await GameOrchestrationService.ProcessPlayerMoveAsync(clientMove);
            if (result.Outcome == PlayerMoveOutcome.PawnPromotionPending && result.PendingPromotionMove != null)
            {
                ModalState.OpenPawnPromotionModal(result.PendingPromotionMove, GameCoreState.MyColor);
                await UiState.SetCurrentInfoMessageForBoxAsync("Wähle eine Figur für die Umwandlung.");
            }
        }

        private async Task HandlePromotionConfirmed(PieceType promotionType)
        {
            if (ModalState?.PendingPromotionMove == null || GameCoreState?.CurrentPlayerInfo == null || UiState == null || Game == null) return;
            ModalState.ClosePawnPromotionModal();
            MoveDto pendingMove = ModalState.PendingPromotionMove;
            ModalState.ClearPendingPromotionMove();
            MoveDto moveWithPromotion = new(pendingMove.From, pendingMove.To, GameCoreState.CurrentPlayerInfo.Id, promotionType);
            await UiState.SetCurrentInfoMessageForBoxAsync($"Figur wird zu {promotionType} umgewandelt...");
            try
            {
                MoveResultDto result = await Game.SendMoveAsync(GameCoreState.GameId, moveWithPromotion);
                if (result.IsValid)
                {
                    UiState.ClearErrorMessage();
                }
                else
                {
                    UiState.SetErrorMessage(result.ErrorMessage ?? "Unbekannter Fehler beim Umwandlungszug.");
                }
            }
            catch (Exception ex)
            {
                UiState.SetErrorMessage($"Fehler beim Senden des Umwandlungszugs: {ex.Message}");
                Logger.LogClientSignalRConnectionWarning($"Fehler beim Senden des Umwandlungszugs: {ex.Message}");
            }
            UiState.ClearCurrentInfoMessageForBox();
        }

        private async Task HandlePromotionCancelled()
        {
            if (ModalState == null || UiState == null) return;
            ModalState.ClosePawnPromotionModal();
            ModalState.ClearPendingPromotionMove();
            await UiState.SetCurrentInfoMessageForBoxAsync("Bauernumwandlung abgebrochen.");
        }

        private async Task HandlePieceTypeSelectedFromModal(PieceType selectedType)
        {
            if (ModalState == null || CardState == null || GameCoreState == null || UiState == null || HighlightState == null) return;
            if (ModalState.ShowPawnPromotionModalSpecifically && ModalState.PendingPromotionMove != null)
            {
                await HandlePromotionConfirmed(selectedType);
            }
            else if (_activeCardForBoardSelectionProcess?.Id == CardConstants.Wiedergeburt && CardState.IsCardActivationPending)
            {
                ModalState.ClosePieceSelectionModal();
                _pieceTypeSelectedForRebirth = selectedType;

                List<string> originalSquares = PieceHelperClient.GetOriginalStartSquares(selectedType, GameCoreState.MyColor);
                List<string> validTargetSquaresOnBoard = [];
                if (GameCoreState.BoardDto?.Squares != null)
                {
                    foreach (string squareString in originalSquares)
                    {
                        (int row, int col) = PositionHelper.ToIndices(squareString);
                        if (row >= 0 && row < 8 && col >= 0 && col < 8 && GameCoreState.BoardDto.Squares[row][col] == null)
                        {
                            validTargetSquaresOnBoard.Add(squareString);
                        }
                    }
                }

                if (validTargetSquaresOnBoard.Count == 0)
                {
                    await UiState.SetCurrentInfoMessageForBoxAsync($"Keine freien Ursprungsfelder für {selectedType} verfügbar. Wiederbelebung fehlgeschlagen.");
                    ActivateCardRequestDto failedRebirthRequest = new()
                    {
                        CardInstanceId = CardState.SelectedCardInstanceIdInHand ?? Guid.Empty,
                        CardTypeId = CardConstants.Wiedergeburt,
                        PieceTypeToRevive = selectedType,
                        TargetRevivalSquare = null
                    };
                    if (_activeCardForBoardSelectionProcess != null)
                    {
                        await FinalizeCardActivationOnServerAsync(failedRebirthRequest, _activeCardForBoardSelectionProcess);
                    }
                    else
                    {
                        await ResetCardActivationStateAsync(true, "Fehler: Karteninformation für Server nicht verfügbar.");
                    }
                    return;
                }

                if (validTargetSquaresOnBoard.Count == 1)
                {
                    string targetSquare = validTargetSquaresOnBoard.Single();
                    ActivateCardRequestDto requestDto = new()
                    {
                        CardInstanceId = CardState.SelectedCardInstanceIdInHand ?? Guid.Empty,
                        CardTypeId = CardConstants.Wiedergeburt,
                        PieceTypeToRevive = _pieceTypeSelectedForRebirth,
                        TargetRevivalSquare = targetSquare
                    };
                    await UiState.SetCurrentInfoMessageForBoxAsync($"Wiederbelebe {selectedType} auf {targetSquare}...");
                    if (_activeCardForBoardSelectionProcess != null)
                    {
                        await FinalizeCardActivationOnServerAsync(requestDto, _activeCardForBoardSelectionProcess);
                    }
                    else
                    {
                        await ResetCardActivationStateAsync(true, "Fehler: Karteninformation für Server nicht verfügbar.");
                    }
                }
                else
                {
                    _isAwaitingRebirthTargetSquareSelection = true;
                    HighlightState.SetCardTargetSquaresForSelection(validTargetSquaresOnBoard);
                    await UiState.SetCurrentInfoMessageForBoxAsync($"Wähle ein leeres Ursprungsfeld auf dem Brett für {_pieceTypeSelectedForRebirth}.");
                }
            }
            else
            {
                ModalState.ClosePieceSelectionModal();
            }
        }

        private async Task HandlePieceSelectionModalCancelled()
        {
            if (ModalState == null || CardState == null || UiState == null) return;
            string? cancelMessage = null;

            if (ModalState.ShowPawnPromotionModalSpecifically)
            {
                await HandlePromotionCancelled();
                cancelMessage = "Bauernumwandlung abgebrochen.";
            }
            else if (CardState.IsCardActivationPending && _activeCardForBoardSelectionProcess != null)
            {
                cancelMessage = $"Aktivierung von '{_activeCardForBoardSelectionProcess.Name}' abgebrochen.";
            }
            await ResetCardActivationStateAsync(fromCancelFlow: true, specificMessageToKeep: cancelMessage);
        }

        private async Task SetCardActionInfoBoxMessage(string message, bool showCancelButton)
        {
            if (UiState is State.UiState concreteUiState) // Dein Cast zu konkreter Klasse
            {
                await concreteUiState.SetCurrentInfoMessageForBoxAsync(message,
                    autoClear: !showCancelButton, 
                    durationMs: showCancelButton ? 0 : 4000, 
                    showActionButton: showCancelButton,
                    actionButtonText: "Auswahl abbrechen",
                    onActionButtonClicked: EventCallback.Factory.Create(this, async () => await ResetCardActivationStateAsync(true, "Kartenaktion abgebrochen."))
                );
            }
            else
            {
                await UiState.SetCurrentInfoMessageForBoxAsync(message,
                                                                autoClear: !showCancelButton,
                                                                durationMs: showCancelButton ? 0 : 4000,
                                                                showActionButton: false,
                                                                actionButtonText: "",
                                                                onActionButtonClicked: null);
            }
        }

        private async Task HandlePlayedCardSelectedForInfoPanel(CardDto card)
        {
            if (CardState == null || UiState == null || ModalState == null) return;

            if (CardState.IsCardActivationPending)
            {
                await UiState.SetCurrentInfoMessageForBoxAsync("Bitte warte, bis die vorherige Kartenaktion abgeschlossen ist.");
                return;
            }
            ModalState.OpenCardInfoPanelModal(card, isActivatable: false, isPreviewOnly: true);
        }

        private async Task HandleActivateCardFromModal(CardDto cardToActivate)
        {
            if (ModalState == null) return;
            ModalState.CloseCardInfoPanelModal();
            await HandleActivateCard(cardToActivate);
        }

        private async Task HandleCloseCardInfoModal()
        {
            if (ModalState == null || CardState == null || UiState == null) return;

            bool wasPreview = ModalState.IsCardInInfoPanelModalPreviewOnly;
            CardDto? cardThatWasInModal = ModalState.CardForInfoPanelModal;

            ModalState.CloseCardInfoPanelModal();

            if (!wasPreview && CardState.IsCardActivationPending)
            {
                await ResetCardActivationStateAsync(true, $"Aktivierung von '{cardThatWasInModal?.Name ?? "Karte"}' abgebrochen.");
            }
            else if (!wasPreview && _activeCardForBoardSelectionProcess != null && _activeCardForBoardSelectionProcess.InstanceId == cardThatWasInModal?.InstanceId)
            {
                await ResetCardActivationStateAsync(true, $"Auswahl für '{_activeCardForBoardSelectionProcess.Name}' abgebrochen.");
            }
            else if (!wasPreview)
            {
                CardState.DeselectActiveHandCard();
            }
            await InvokeAsync(StateHasChanged);
        }

        // File: [SolutionDir]\ChessClient\Pages\Chess.razor.cs

        private async Task HandleActivateCard(CardDto cardToActivate)
        {
            if (UiState == null || GameCoreState == null || CardState == null || ModalState == null || HighlightState == null || Game == null) return;

            // Konsistenzprüfung: Die zur Aktivierung ausgewählte Karte sollte die im CardState hinterlegte sein.
            if (CardState.SelectedCardInstanceIdInHand != cardToActivate.InstanceId)
            {
                Logger.LogClientSignalRConnectionWarning($"[HandleActivateCard] KRITISCHE DISKREPANZ: cardToActivate.InstanceId ({cardToActivate.InstanceId}) != CardState.SelectedCardInstanceIdInHand ({CardState.SelectedCardInstanceIdInHand}). Dies sollte nicht passieren. Setze SelectedCardInstanceIdInHand auf cardToActivate.InstanceId.");
                // Versuche, den Zustand zu korrigieren, indem die aktive Karte im State gesetzt wird.
                // Dies setzt voraus, dass eine solche Methode im CardState existiert oder hinzugefügt wird,
                // die keine UI-Interaktion (wie das Öffnen des Modals) auslöst.
                // Beispiel: CardState.ForceSetSelectedCardInstanceId(cardToActivate.InstanceId);
                // Wenn nicht, dann ist der Fehler im Flow vor diesem Aufruf.
                // Für jetzt: Wir gehen davon aus, dass 'cardToActivate' die Absicht des Nutzers ist.
            }

            if (!IsCardActivatable(cardToActivate))
            {
                await SetCardActionInfoBoxMessage($"Karte '{cardToActivate.Name}' kann momentan nicht aktiviert werden.", false);
                // Kein Reset hier, da die Karte nicht aktiviert wurde und der Auswahlstatus für diese Karte bestehen bleiben könnte.
                // Der Nutzer muss eine andere Aktion wählen oder das InfoPanel/Auswahl abbrechen.
                CardState.SetIsCardActivationPending(false); // Sicherstellen, dass keine Aktivierung als "laufend" markiert bleibt.
                return;
            }

            // Wichtige Zustandsvariablen *vor* potenziellen await-Aufrufen sichern
            Guid cardInstanceIdForRequest = cardToActivate.InstanceId;
            string cardTypeIdForRequest = cardToActivate.Id;
            CardDto cardDefinitionForFinalize = new CardDto // Erstelle eine Kopie, um Zustandskonflikte zu vermeiden
            {
                InstanceId = cardToActivate.InstanceId,
                Id = cardToActivate.Id,
                Name = cardToActivate.Name,
                Description = cardToActivate.Description,
                ImageUrl = cardToActivate.ImageUrl
            };

            // Zustand für laufende Kartenaktivierung setzen
            CardState.SetIsCardActivationPending(true);
            _activeCardForBoardSelectionProcess = cardDefinitionForFinalize; // Verwende die Kopie/gesicherte Definition
            _firstSquareSelectedForTeleportOrSwap = null;
            _isAwaitingRebirthTargetSquareSelection = false;
            _pieceTypeSelectedForRebirth = null;
            HighlightState.ClearAllActionHighlights();
            _isAwaitingSacrificePawnSelection = false;

            // Logik für spezifische Karten, die eine Brettinteraktion erfordern
            if (cardTypeIdForRequest == CardConstants.Teleport)
            {
                await SetCardActionInfoBoxMessage("Teleport: Wähle eine deiner Figuren auf dem Brett aus.", true);
            }
            else if (cardTypeIdForRequest == CardConstants.Positionstausch)
            {
                await SetCardActionInfoBoxMessage("Positionstausch: Wähle deine erste Figur auf dem Brett aus.", true);
            }
            else if (cardTypeIdForRequest == CardConstants.SacrificeEffect) // Name aus CardConstants verwenden
            {
                _isAwaitingSacrificePawnSelection = true;
                List<string> pawnSquares = new List<string>();
                if (GameCoreState.BoardDto != null && GameCoreState.CurrentPlayerInfo != null)
                {
                    for (int r = 0; r < 8; r++)
                    {
                        for (int f = 0; f < 8; f++)
                        {
                            PieceDto? piece = GameCoreState.BoardDto.Squares[r][f];
                            if (piece.HasValue &&
                                piece.Value.IsOfPlayerColor(GameCoreState.MyColor) &&
                                piece.Value.ToString().Contains("Pawn", StringComparison.OrdinalIgnoreCase))
                            {
                                pawnSquares.Add(PositionHelper.ToAlgebraic(r, f));
                            }
                        }
                    }
                }
                if (pawnSquares.Count > 0)
                {
                    HighlightState.SetCardTargetSquaresForSelection(pawnSquares);
                    await SetCardActionInfoBoxMessage("Opfergabe: Wähle einen deiner Bauern zum Opfern aus.", true);
                }
                else
                {
                    // Keine Bauern vorhanden, Karte direkt mit Fehlschlag/Verfall abwickeln
                    await UiState.SetCurrentInfoMessageForBoxAsync("Keine eigenen Bauern zum Opfern vorhanden. Karte verfällt.", true, 4000);
                    // Wichtig: IsCardActivationPending hier nicht auf false setzen, das macht FinalizeCardActivationOnServerAsync
                    ActivateCardRequestDto failedSacrificeRequest = new()
                    {
                        CardInstanceId = cardInstanceIdForRequest,
                        CardTypeId = cardTypeIdForRequest,
                        FromSquare = null // Wichtig: Explizit null, da kein Bauer gewählt wurde
                    };
                    await FinalizeCardActivationOnServerAsync(failedSacrificeRequest, cardDefinitionForFinalize);
                }
            }
            else if (cardTypeIdForRequest == CardConstants.Wiedergeburt)
            {
                await UiState.SetCurrentInfoMessageForBoxAsync("Lade geschlagene Figuren für Wiedergeburt...",
                    autoClear: false,
                    showActionButton: false,
                    actionButtonText: "",
                    onActionButtonClicked: null);

                if (GameCoreState.CurrentPlayerInfo == null || GameCoreState.GameId == Guid.Empty)
                {
                    await ResetCardActivationStateAsync(true, "Fehler: Spielerdaten nicht verfügbar.");
                    return;
                }
                await CardState.LoadCapturedPiecesForRebirthAsync(GameCoreState.GameId, GameCoreState.CurrentPlayerInfo.Id, Game);
                List<PieceType> capturedPieceTypes = CardState.CapturedPiecesForRebirth?.Select(p => p.Type).Distinct().ToList() ?? [];

                if (capturedPieceTypes.Count == 0)
                {
                    await SetCardActionInfoBoxMessage("Keine wiederbelebungsfähigen Figuren geschlagen. Karte verfällt.", false);
                    ActivateCardRequestDto failedRebirthRequest = new() { CardInstanceId = cardInstanceIdForRequest, CardTypeId = cardTypeIdForRequest };
                    await FinalizeCardActivationOnServerAsync(failedRebirthRequest, cardDefinitionForFinalize);
                }
                else
                {
                    UiState.ClearCurrentInfoMessageForBox();
                    List<PieceSelectionChoiceInfo> choicesForModal = [];
                    foreach (PieceType pieceType in capturedPieceTypes)
                    {
                        List<string> originalSquares = PieceHelperClient.GetOriginalStartSquares(pieceType, GameCoreState.MyColor);
                        bool canBeRevivedOnBoard = false;
                        string tooltip = $"Figur: {pieceType}. ";
                        if (originalSquares.Count == 0) { tooltip += "Keine definierten Startfelder."; }
                        else if (GameCoreState.BoardDto?.Squares != null)
                        {
                            foreach (string squareString in originalSquares)
                            {
                                (int row, int col) = PositionHelper.ToIndices(squareString);
                                if (row >= 0 && row < 8 && col >= 0 && col < 8 && GameCoreState.BoardDto.Squares[row][col] == null)
                                { canBeRevivedOnBoard = true; break; }
                            }
                        }
                        if (canBeRevivedOnBoard) { tooltip += "Mindestens ein Startfeld ist frei."; }
                        else { tooltip += "Alle Startfelder sind besetzt."; }
                        choicesForModal.Add(new PieceSelectionChoiceInfo(pieceType, canBeRevivedOnBoard, tooltip));
                    }
                    ModalState.OpenPieceSelectionModal("Figur zur Wiederbelebung wählen", "Wähle eine geschlagene Figur (grau = Startfelder besetzt):", choicesForModal, GameCoreState.MyColor);
                }
            }
            else // Für Karten ohne spezielle Brettauswahl (Zeit, Kartentausch etc.)
            {
                // IsCardActivationPending ist bereits true.
                // SelectedCardInstanceIdInHand ist auch noch die korrekte Instanz-ID der gerade geklickten Karte.
                // _activeCardForBoardSelectionProcess ist gesetzt.

                ActivateCardRequestDto requestDto = new ActivateCardRequestDto
                {
                    CardInstanceId = cardInstanceIdForRequest,
                    CardTypeId = cardTypeIdForRequest
                };

                if (cardToActivate.Id == CardConstants.CardSwap)
                {
                    requestDto.CardInstanceIdToSwapFromHand = CardState.PlayerHandCards?.FirstOrDefault(c => c.InstanceId != cardInstanceIdForRequest)?.InstanceId;
                    if (!requestDto.CardInstanceIdToSwapFromHand.HasValue && (CardState.PlayerHandCards?.Count ?? 0) > 1)
                    {
                        await SetCardActionInfoBoxMessage("Keine andere Karte zum Anbieten für Tausch gefunden. Tausch nicht möglich.", false);
                        await ResetCardActivationStateAsync(true); // Hier Reset, da Finalize nicht sicher erreicht wird
                        return;
                    }
                    await SetCardActionInfoBoxMessage($"Kartentausch von '{cardDefinitionForFinalize.Name}' wird versucht...", false);
                }
                else
                {
                    await SetCardActionInfoBoxMessage($"Aktiviere Karte '{cardDefinitionForFinalize.Name}'...", false);
                }

                // Wichtig: Unmittelbar bevor der await-Call kommt, der die Kontrolle abgibt,
                // sollte der *selektierte Zustand der Handkarte* im CardState zurückgesetzt werden,
                // um zu verhindern, dass bei schnellen UI-Interaktionen oder Re-Rendern die alte ID erneut verwendet wird.
                // _activeCardForBoardSelectionProcess bleibt für den Kontext von FinalizeCardActivationOnServerAsync erhalten.
                CardState.DeselectActiveHandCard();

                await FinalizeCardActivationOnServerAsync(requestDto, cardDefinitionForFinalize);
            }
            await InvokeAsync(StateHasChanged); // UI aktualisieren, um InfoBox, etc. anzuzeigen
        }

        public async Task<CardActivationFinalizationResult> FinalizeCardActivationOnServerAsync(ActivateCardRequestDto requestDto, CardDto activatedCardDefinition)
        {
            if (GameOrchestrationService == null || CardState == null || GameCoreState == null || UiState == null)
            {
                return new CardActivationFinalizationResult(CardActivationOutcome.Error, "Kritische Dienste nicht verfügbar.");
            }

            if (CardState.SelectedCardInstanceIdInHand.HasValue && requestDto.CardInstanceId != CardState.SelectedCardInstanceIdInHand.Value)
            {
                Logger.LogClientSignalRConnectionWarning($"[FinalizeCardActivationOnServerAsync] Korrigiere CardInstanceId im Request von {requestDto.CardInstanceId} zu {CardState.SelectedCardInstanceIdInHand.Value}");
                requestDto = requestDto with { CardInstanceId = CardState.SelectedCardInstanceIdInHand.Value };
            }

            CardActivationFinalizationResult result = await GameOrchestrationService.FinalizeCardActivationAsync(requestDto, activatedCardDefinition);

            string? messageToKeepAfterReset = null;
            bool successOutcome = result.Outcome == CardActivationOutcome.Success;

            if (successOutcome)
            {
                messageToKeepAfterReset = $"Karte '{activatedCardDefinition.Name}' erfolgreich aktiviert!";
                bool cardShouldEndTurn = true;
                if (activatedCardDefinition.Id == CardConstants.ExtraZug)
                {
                    if (GameCoreState?.CurrentPlayerInfo != null && GameCoreState.MyColor == GameCoreState.CurrentTurnPlayer)
                    {
                        _isExtraTurnSequenceActive = true;
                        _extraTurnMovesMade = 0;
                        cardShouldEndTurn = false;
                    }
                }

                if (cardShouldEndTurn)
                {
                    _isAwaitingTurnConfirmationAfterCard = true;
                    Logger.LogClientSignalRConnectionWarning($"[ChessPage] Card '{activatedCardDefinition.Name}' (ID: {activatedCardDefinition.Id}) played, setting _isAwaitingTurnConfirmationAfterCard = true.");
                }
            }
            else
            {
                messageToKeepAfterReset = UiState.ErrorMessage;
            }

            await ResetCardActivationStateAsync(fromCancelFlow: !successOutcome,
                                                 specificMessageToKeep: messageToKeepAfterReset);
            return result;
        }

        // File: [SolutionDir]\ChessClient\Pages\Chess.razor.cs

        private async Task HandleSquareClickForCardTargetSelection(string algebraicCoord)
        {
            if (CardState == null || _activeCardForBoardSelectionProcess == null || !CardState.IsCardActivationPending || UiState == null || GameCoreState == null || HighlightState == null)
            {
                // Wenn ein grundlegender State null ist, hier abbrechen, um NullReferenceExceptions zu vermeiden.
                Logger.LogClientCriticalServicesNullOnInit("[HandleSquareClickForCardTargetSelection] Kritischer State ist null. Breche ab.");
                await ResetCardActivationStateAsync(true, "Fehler bei Kartenaktion (interner Zustand).");
                return;
            }

            // Wichtige Daten sichern, bevor der Zustand potenziell zurückgesetzt wird.
            CardDto activeCardDefinitionForFinalize = _activeCardForBoardSelectionProcess;
            Guid? selectedHandCardInstanceIdForRequest = CardState.SelectedCardInstanceIdInHand;

            // Konsistenzprüfung: Ist die Karte, die wir bearbeiten, noch die, die in CardState als ausgewählt gilt?
            if (!selectedHandCardInstanceIdForRequest.HasValue || selectedHandCardInstanceIdForRequest.Value != activeCardDefinitionForFinalize.InstanceId)
            {
                Logger.LogClientSignalRConnectionWarning($"[HandleSquareClickForCardTargetSelection] Inkonsistenz: _activeCardForBoardSelectionProcess ({activeCardDefinitionForFinalize.Name}, Inst: {activeCardDefinitionForFinalize.InstanceId}) stimmt nicht mit CardState.SelectedCardInstanceIdInHand ({selectedHandCardInstanceIdForRequest}) überein oder letzteres ist null.");
                await ResetCardActivationStateAsync(true, "Fehler bei Kartenauswahl (Inkonsistenz im Zustand).");
                return;
            }

            ActivateCardRequestDto? requestDto = null;
            string originalInfoMessage = UiState.CurrentInfoMessageForBox; // Merken für den Fall, dass wir zurück müssen

            if (activeCardDefinitionForFinalize.Id == CardConstants.Teleport)
            {
                if (string.IsNullOrEmpty(_firstSquareSelectedForTeleportOrSwap))
                {
                    (int r, int f) = PositionHelper.ToIndices(algebraicCoord);
                    PieceDto? pieceOnSquare = GameCoreState.BoardDto?.Squares[r][f];
                    if (pieceOnSquare.HasValue && pieceOnSquare.Value.IsOfPlayerColor(GameCoreState.MyColor))
                    {
                        _firstSquareSelectedForTeleportOrSwap = algebraicCoord;
                        await SetCardActionInfoBoxMessage($"Teleport: Figur auf {algebraicCoord} ausgewählt. Wähle nun ein leeres Zielfeld.", true);
                        HighlightState.ClearCardTargetSquaresForSelection(); // Nur das eine Feld highlighten
                        HighlightState.SetHighlights(algebraicCoord, null, false);
                    }
                    else
                    {
                        await SetCardActionInfoBoxMessage("Teleport: Bitte wähle eine deiner eigenen Figuren.", true);
                    }
                    await InvokeAsync(StateHasChanged);
                    return; // Warte auf den zweiten Klick
                }
                else // Zweiter Klick für Teleport (Zielfeld)
                {
                    (int rTo, int fTo) = PositionHelper.ToIndices(algebraicCoord);
                    if (GameCoreState.BoardDto?.Squares[rTo][fTo] != null)
                    {
                        await SetCardActionInfoBoxMessage("Teleport: Das Zielfeld muss leer sein. Wähle ein anderes oder brich ab.", true);
                        return; // Warte auf korrekten zweiten Klick
                    }
                    requestDto = new ActivateCardRequestDto
                    {
                        CardInstanceId = selectedHandCardInstanceIdForRequest.Value,
                        CardTypeId = activeCardDefinitionForFinalize.Id,
                        FromSquare = _firstSquareSelectedForTeleportOrSwap,
                        ToSquare = algebraicCoord
                    };
                    await SetCardActionInfoBoxMessage($"Teleportiere Figur von {_firstSquareSelectedForTeleportOrSwap} nach {algebraicCoord}...", false);
                }
            }
            else if (activeCardDefinitionForFinalize.Id == CardConstants.Positionstausch)
            {
                if (string.IsNullOrEmpty(_firstSquareSelectedForTeleportOrSwap))
                {
                    (int r, int f) = PositionHelper.ToIndices(algebraicCoord);
                    PieceDto? pieceOnSquare = GameCoreState.BoardDto?.Squares[r][f];
                    if (pieceOnSquare.HasValue && pieceOnSquare.Value.IsOfPlayerColor(GameCoreState.MyColor))
                    {
                        _firstSquareSelectedForTeleportOrSwap = algebraicCoord;
                        await SetCardActionInfoBoxMessage($"Positionstausch: Erste Figur auf {algebraicCoord} ausgewählt. Wähle nun deine zweite Figur.", true);
                        HighlightState.ClearCardTargetSquaresForSelection();
                        HighlightState.SetHighlights(algebraicCoord, null, false);
                    }
                    else
                    {
                        await SetCardActionInfoBoxMessage("Positionstausch: Bitte wähle eine deiner eigenen Figuren.", true);
                    }
                    await InvokeAsync(StateHasChanged);
                    return; // Warte auf den zweiten Klick
                }
                else // Zweiter Klick für Positionstausch
                {
                    (int r, int f) = PositionHelper.ToIndices(algebraicCoord);
                    PieceDto? pieceOnSquare = GameCoreState.BoardDto?.Squares[r][f];
                    if (pieceOnSquare.HasValue && pieceOnSquare.Value.IsOfPlayerColor(GameCoreState.MyColor) && algebraicCoord != _firstSquareSelectedForTeleportOrSwap)
                    {
                        requestDto = new ActivateCardRequestDto
                        {
                            CardInstanceId = selectedHandCardInstanceIdForRequest.Value,
                            CardTypeId = activeCardDefinitionForFinalize.Id,
                            FromSquare = _firstSquareSelectedForTeleportOrSwap,
                            ToSquare = algebraicCoord
                        };
                        await SetCardActionInfoBoxMessage($"Tausche Figuren auf {_firstSquareSelectedForTeleportOrSwap} und {algebraicCoord}...", false);
                    }
                    else
                    {
                        await SetCardActionInfoBoxMessage("Positionstausch: Bitte wähle eine andere deiner eigenen Figuren (nicht dieselbe).", true);
                        return; // Warte auf korrekten zweiten Klick
                    }
                }
            }
            else if (activeCardDefinitionForFinalize.Id == CardConstants.Wiedergeburt && _isAwaitingRebirthTargetSquareSelection)
            {
                if (_pieceTypeSelectedForRebirth == null)
                {
                    await ResetCardActivationStateAsync(true, "Fehler: Figurentyp für Wiedergeburt nicht ausgewählt.");
                    return;
                }
                if (!HighlightState.CardTargetSquaresForSelection.Contains(algebraicCoord))
                {
                    await UiState.SetCurrentInfoMessageForBoxAsync("Ungültiges Feld. Wähle ein hervorgehobenes, leeres Ursprungsfeld.", autoClear: true, durationMs: 3000);
                    return; // Warte auf korrekten Klick
                }
                requestDto = new ActivateCardRequestDto
                {
                    CardInstanceId = selectedHandCardInstanceIdForRequest.Value,
                    CardTypeId = activeCardDefinitionForFinalize.Id,
                    PieceTypeToRevive = _pieceTypeSelectedForRebirth,
                    TargetRevivalSquare = algebraicCoord
                };
                await SetCardActionInfoBoxMessage($"Wiederbelebe {_pieceTypeSelectedForRebirth} auf {algebraicCoord}...", false);
            }
            else if (_isAwaitingSacrificePawnSelection && activeCardDefinitionForFinalize.Id == CardConstants.SacrificeEffect)
            {
                (int r, int c) = PositionHelper.ToIndices(algebraicCoord);
                PieceDto? piece = GameCoreState.BoardDto?.Squares[r][c];

                // Ist das geklickte Feld ein gültiger Bauer des Spielers?
                // HighlightState.CardTargetSquaresForSelection sollte bereits die gültigen Bauern enthalten.
                if (HighlightState.CardTargetSquaresForSelection.Contains(algebraicCoord) &&
                    piece.HasValue && piece.Value.IsOfPlayerColor(GameCoreState.MyColor) &&
                    piece.Value.ToString().Contains("Pawn", StringComparison.OrdinalIgnoreCase))
                {
                    requestDto = new ActivateCardRequestDto
                    {
                        CardInstanceId = selectedHandCardInstanceIdForRequest.Value,
                        CardTypeId = activeCardDefinitionForFinalize.Id,
                        FromSquare = algebraicCoord, // Der ausgewählte Bauer
                        ToSquare = null
                    };
                    await SetCardActionInfoBoxMessage($"Opfere Bauer auf {algebraicCoord}...", false);
                }
                else
                {
                    await UiState.SetCurrentInfoMessageForBoxAsync("Ungültige Auswahl. Bitte wähle einen deiner Bauern (hervorgehoben).", true, 3000);
                    return; // Auswahl nicht finalisieren, Spieler soll erneut wählen oder abbrechen
                }
            }
            else
            {
                // Dieser Fall sollte nicht eintreten, wenn die UI-Logik korrekt ist.
                await UiState.SetCurrentInfoMessageForBoxAsync("Unerwarteter Zustand bei der Feldauswahl für Kartenaktion.", true, 5000);
                await ResetCardActivationStateAsync(true);
                return;
            }

            // Wenn ein requestDto erstellt wurde (d.h. die Auswahl für die Karte ist komplett)
            if (requestDto != null)
            {
                // **WICHTIG:** Aggressives Zurücksetzen des Client-Auswahlstatus, *bevor* der Server-Call gemacht wird.
                // Dies hilft, Race Conditions oder das Senden veralteter Daten bei schnellen UI-Interaktionen zu vermeiden.
                // Der _activeCardForBoardSelectionProcess (bzw. die Kopie cardDefinitionForFinalize) wird an Finalize übergeben.
                string messageForInfoBox = UiState.CurrentInfoMessageForBox;
                if (string.IsNullOrEmpty(messageForInfoBox) || messageForInfoBox.Contains("Wähle"))
                {
                    messageForInfoBox = $"Verarbeite '{activeCardDefinitionForFinalize.Name}'...";
                }

                // SetIsCardActivationPending(true) ist bereits am Anfang von HandleActivateCard gesetzt.
                // Hier fokussieren wir uns auf das Zurücksetzen der *Auswahl*-spezifischen Zustände.
                _firstSquareSelectedForTeleportOrSwap = null;
                _isAwaitingRebirthTargetSquareSelection = false;
                _isAwaitingSacrificePawnSelection = false;
                HighlightState.ClearCardTargetSquaresForSelection();
                // SelectedCardInstanceIdInHand wird in FinalizeCardActivationOnServerAsync -> ResetCardActivationStateAsync genullt
                // _activeCardForBoardSelectionProcess wird ebenfalls dort genullt.

                await FinalizeCardActivationOnServerAsync(requestDto, activeCardDefinitionForFinalize);
            }
            // Kein await InvokeAsync(StateHasChanged) hier, da FinalizeCardActivationOnServerAsync das implizit auslöst
            // oder das SetCardActionInfoBoxMessage es bereits getan hat.
        }

        private async Task ResetCardActivationStateAsync(bool fromCancelFlow = false, string? specificMessageToKeep = null)
        {
            if (CardState == null || HighlightState == null || ModalState == null || UiState == null) return;
            bool wasPending = CardState.IsCardActivationPending;
            CardDto? previouslyActiveBoardSelectionCard = _activeCardForBoardSelectionProcess;

            CardState.SetIsCardActivationPending(false);
            CardState.DeselectActiveHandCard();
            _activeCardForBoardSelectionProcess = null;

            ModalState.ClosePieceSelectionModal();
            ModalState.CloseCardInfoPanelModal();

            HighlightState.ClearAllActionHighlights();
            HighlightState.ClearCardTargetSquaresForSelection(); // Wichtig, um Opfergabe-Highlights zu entfernen

            _pieceTypeSelectedForRebirth = null;
            _isAwaitingRebirthTargetSquareSelection = false;
            _firstSquareSelectedForTeleportOrSwap = null;
            _isAwaitingSacrificePawnSelection = false; // NEU: Zurücksetzen

            if (!string.IsNullOrEmpty(specificMessageToKeep))
            {
                await UiState.SetCurrentInfoMessageForBoxAsync(specificMessageToKeep, true, 4000, false, "", null);
            }
            else if (fromCancelFlow && wasPending && previouslyActiveBoardSelectionCard != null)
            {
                await UiState.SetCurrentInfoMessageForBoxAsync($"Aktivierung von '{previouslyActiveBoardSelectionCard.Name}' abgebrochen.", true, 4000, false, "", null);
            }
            else
            {
                // Lösche InfoBox nur, wenn sie eine "Wähle..." Nachricht enthielt
                if (UiState.CurrentInfoMessageForBox.Contains("Wähle") || UiState.CurrentInfoMessageForBox.Contains("Lade"))
                {
                    UiState.ClearCurrentInfoMessageForBox();
                }
            }
            await InvokeAsync(StateHasChanged);
        }

        private bool IsBoardInCardSelectionMode()
        {
            if (CardState == null || !CardState.IsCardActivationPending || _activeCardForBoardSelectionProcess == null) return false;
            string cardId = _activeCardForBoardSelectionProcess.Id;
            bool isTeleportOrSwap = cardId is CardConstants.Teleport or CardConstants.Positionstausch;
            bool isRebirthTargetSelection = cardId == CardConstants.Wiedergeburt && _isAwaitingRebirthTargetSquareSelection;
            bool isSacrificePawnSelection = cardId == CardConstants.SacrificeEffect && _isAwaitingSacrificePawnSelection;

            bool isSelectingFirstPieceForTeleportOrSwap = isTeleportOrSwap && string.IsNullOrEmpty(_firstSquareSelectedForTeleportOrSwap);
            bool isSelectingSecondPieceOrTargetForTeleportOrSwap = isTeleportOrSwap && !string.IsNullOrEmpty(_firstSquareSelectedForTeleportOrSwap);

            return isSelectingFirstPieceForTeleportOrSwap ||
                   isSelectingSecondPieceOrTargetForTeleportOrSwap ||
                   isRebirthTargetSelection ||
                   isSacrificePawnSelection; // NEU
        }

        private Player? GetPlayerColorForCardPieceSelection()
        {
            if (CardState != null && GameCoreState != null && CardState.IsCardActivationPending && _activeCardForBoardSelectionProcess != null)
            {
                string cardId = _activeCardForBoardSelectionProcess.Id;
                if ((cardId == CardConstants.Teleport || cardId == CardConstants.Positionstausch) && string.IsNullOrEmpty(_firstSquareSelectedForTeleportOrSwap))
                {
                    return GameCoreState.MyColor;
                }
            }
            return null;
        }

        private string? GetFirstSelectedSquareForCardEffect()
        {
            if (CardState != null && CardState.IsCardActivationPending && _activeCardForBoardSelectionProcess != null)
            {
                string cardId = _activeCardForBoardSelectionProcess.Id;
                if ((cardId == CardConstants.Teleport || cardId == CardConstants.Positionstausch) && !string.IsNullOrEmpty(_firstSquareSelectedForTeleportOrSwap))
                {
                    return _firstSquareSelectedForTeleportOrSwap;
                }
            }
            return null;
        }

        private void HandleCardPlayedNotificationClient(Guid playingPlayerId, CardDto playedCardFullDefinition)
        {
            if (CardState == null || GameCoreState?.CurrentPlayerInfo == null || UiState == null || HighlightState == null || ModalState == null) return;
            CardDto? cardDefinitionForHistory = playedCardFullDefinition;
            if (cardDefinitionForHistory != null)
            {
                PlayedCardInfo playedCardInfo = new() { CardDefinition = cardDefinitionForHistory, PlayerId = playingPlayerId, Timestamp = DateTime.UtcNow };
                if (playingPlayerId == GameCoreState.CurrentPlayerInfo.Id)
                {
                    CardState.HandleCardPlayedByMe(cardDefinitionForHistory.InstanceId, cardDefinitionForHistory.Id);
                    CardState.AddToMyPlayedHistory(playedCardInfo);
                    _ = UiState.SetCurrentInfoMessageForBoxAsync($"Du hast Karte '{cardDefinitionForHistory.Name}' gespielt!", true, 3000);
                    if (cardDefinitionForHistory.Id == CardConstants.ExtraZug)
                    {
                        _isExtraTurnSequenceActive = true;
                        _extraTurnMovesMade = 0;
                        HighlightState.SetHighlights(null, null, false);
                    }
                }
                else
                {
                    CardState.AddToOpponentPlayedHistory(playedCardInfo);
                    _ = UiState.SetCurrentInfoMessageForBoxAsync($"Gegner hat Karte '{cardDefinitionForHistory.Name}' gespielt!", true, 3000);
                }
            }
            else if (playingPlayerId != GameCoreState.CurrentPlayerInfo.Id)
            {
                _ = UiState.SetCurrentInfoMessageForBoxAsync($"Gegner hat eine Karte gespielt (Details nicht vollständig empfangen).", true, 3000);
                Logger.LogClientSignalRConnectionWarning("Gegner spielte eine Karte, aber CardDefinition war null.");
            }

            if (ModalState.ShowCardInfoPanelModal && ModalState.CardForInfoPanelModal?.InstanceId == cardDefinitionForHistory?.InstanceId)
            {
                ModalState.CloseCardInfoPanelModal();
            }
            if (CardState.IsCardActivationPending && (_activeCardForBoardSelectionProcess?.InstanceId == cardDefinitionForHistory?.InstanceId || CardState.SelectedCardInstanceIdInHand == cardDefinitionForHistory?.InstanceId))
            {
                _ = ResetCardActivationStateAsync(fromCancelFlow: false);
            }
        }
        private void HandlePlayerEarnedCardDrawNotification(Guid playerIdToDraw)
        {
            if (GameCoreState?.CurrentPlayerInfo != null && playerIdToDraw == GameCoreState.CurrentPlayerInfo.Id)
            {
                Logger.LogClientSignalRConnectionWarning($"[Chess.razor.cs] Info: Server hat einen Kartenzug für mich veranlasst (PlayerId: {playerIdToDraw}). Spezifische Karte kommt via 'CardAddedToHand'.");
            }
        }

        private void HandleReceiveCardSwapAnimationDetails(CardSwapAnimationDetailsDto details)
        {
            bool isGenericAnimatingCurrently = AnimationState?.IsCardActivationAnimating ?? false;
            string? currentGenericCardId = _lastActivatedCardForGenericAnimation?.Id;
            Logger.LogHandleReceiveCardSwapDetails(details.CardGiven.Name, details.CardReceived.Name, isGenericAnimatingCurrently);
            Logger.LogClientSignalRConnectionWarning($"[ChessPage] HandleReceiveCardSwapAnimationDetails: _lastActivatedCardForGenericAnimation ID: {currentGenericCardId}, IsCardActivationAnimating: {isGenericAnimatingCurrently}");
            if (AnimationState != null && details != null && GameCoreState?.CurrentPlayerInfo != null)
            {
                if (GameCoreState.CurrentPlayerInfo.Id == details.PlayerId)
                {
                    _pendingSwapAnimationDetails = details;
                    Logger.LogClientSignalRConnectionWarning("[ChessPage] HandleReceiveCardSwapAnimationDetails: Details für Kartentausch gespeichert (_pendingSwapAnimationDetails gesetzt). Warten auf HandleClientAnimationFinished, falls generische Animation für 'cardswap' lief/läuft.");
                }
            }
            else
            {
                Logger.LogClientCriticalServicesNullOnInit($"[ChessPage] HandleReceiveCardSwapAnimationDetails: Kritische Objekte sind null oder Spieler-ID stimmt nicht. animationStateIsNull: {AnimationState == null}, detailsIsNull: {details == null}, currentPlayerInfoIsNull: {GameCoreState?.CurrentPlayerInfo == null}, spielerIdPasst: {GameCoreState?.CurrentPlayerInfo?.Id == details?.PlayerId}");
            }
        }

        private void RegisterCardEventHandlers()
        {
            if (HubService == null) return;
            HubService.OnCardPlayed -= HandleCardPlayedNotificationClient;
            HubService.OnCardPlayed += HandleCardPlayedNotificationClient;
        }

        private void DeregisterCardEventHandlers()
        {
            if (HubService == null) return;
            HubService.OnCardPlayed -= HandleCardPlayedNotificationClient;
        }

        public async ValueTask DisposeAsync()
        {
            UnsubscribeFromStateChanges();
            // Annahme: HubService, ModalService, ModalState, AnimationState sind nach OnInitializedAsync nicht null.
            HubService.OnTurnChanged -= HandleHubTurnChanged;
            HubService.OnTimeUpdate -= HandleHubTimeUpdate;
            HubService.OnPlayerJoined -= HandlePlayerJoinedClient;
            HubService.OnPlayerLeft -= HandlePlayerLeftClient;
            HubService.OnPlayCardActivationAnimation -= HandlePlayCardActivationAnimation;
            HubService.OnUpdateHandContents -= HandleUpdateHandContents;
            HubService.OnReceiveInitialHand -= HandleReceiveInitialHand;
            HubService.OnCardAddedToHand -= HandleCardAddedToHand;
            HubService.OnPlayerEarnedCardDraw -= HandlePlayerEarnedCardDrawNotification;
            HubService.OnReceiveCardSwapAnimationDetails -= HandleReceiveCardSwapAnimationDetails;

            DeregisterCardEventHandlers();
            await HubService.DisposeAsync();
            ModalService.ShowCreateGameModalRequested -= OpenCreateGameModalHandler;
            ModalService.ShowJoinGameModalRequested -= OpenJoinGameModalHandler;

            ModalState.CloseCardInfoPanelModal();
            ModalState.ClosePieceSelectionModal();

            AnimationState.FinishCardActivationAnimation();
            AnimationState.FinishCardSwapAnimation();
            _lastActivatedCardForGenericAnimation = null;
            _pendingSwapAnimationDetails = null;

            GC.SuppressFinalize(this);
        }
    }
}