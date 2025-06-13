using Chess.Logging;
using ChessClient.Models;
using ChessClient.State;
using ChessLogic;
using ChessNetwork.Configuration;
using ChessNetwork.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChessClient.Services
{
    public class HubSubscriptionService : IAsyncDisposable
    {
        private readonly ChessHubService _hubService;
        private readonly IGameCoreState _gameCoreState;
        private readonly ICardState _cardState;
        private readonly IUiState _uiState;
        private readonly IAnimationState _animationState;
        private readonly IHighlightState _highlightState;
        private readonly IModalState _modalState;
        private readonly IChessLogger _logger;

        public HubSubscriptionService(
            ChessHubService hubService,
            IGameCoreState gameCoreState,
            ICardState cardState,
            IUiState uiState,
            IAnimationState animationState,
            IHighlightState highlightState,
            IModalState modalState,
            IChessLogger logger)
        {
            _hubService = hubService;
            _gameCoreState = gameCoreState;
            _cardState = cardState;
            _uiState = uiState;
            _animationState = animationState;
            _highlightState = highlightState;
            _modalState = modalState;
            _logger = logger;
        }

        public void Initialize()
        {
            UnsubscribeFromHubEvents();
            SubscribeToHubEvents();
        }

        private void SubscribeToHubEvents()
        {
            _hubService.OnTurnChanged += HandleHubTurnChangedAsyncWrapper;
            _hubService.OnTimeUpdate += HandleHubTimeUpdate;
            _hubService.OnPlayerJoined += HandlePlayerJoinedClient;
            _hubService.OnPlayerLeft += HandlePlayerLeftClient;
            _hubService.OnPlayCardActivationAnimation += HandlePlayCardActivationAnimation;
            _hubService.OnReceiveInitialHand += HandleReceiveInitialHand;
            _hubService.OnCardAddedToHand += HandleCardAddedToHand;
            _hubService.OnUpdateHandContents += HandleUpdateHandContents;
            _hubService.OnPlayerEarnedCardDraw += HandlePlayerEarnedCardDrawNotification;
            _hubService.OnReceiveCardSwapAnimationDetails += HandleReceiveCardSwapAnimationDetails;
            _hubService.OnStartGameCountdown += HandleStartGameCountdown;
            _hubService.OnCardPlayed += HandleCardPlayedNotificationClient;
        }

        private void UnsubscribeFromHubEvents()
        {
            _hubService.OnTurnChanged -= HandleHubTurnChangedAsyncWrapper;
            _hubService.OnTimeUpdate -= HandleHubTimeUpdate;
            _hubService.OnPlayerJoined -= HandlePlayerJoinedClient;
            _hubService.OnPlayerLeft -= HandlePlayerLeftClient;
            _hubService.OnPlayCardActivationAnimation -= HandlePlayCardActivationAnimation;
            _hubService.OnReceiveInitialHand -= HandleReceiveInitialHand;
            _hubService.OnCardAddedToHand -= HandleCardAddedToHand;
            _hubService.OnUpdateHandContents -= HandleUpdateHandContents;
            _hubService.OnPlayerEarnedCardDraw -= HandlePlayerEarnedCardDrawNotification;
            _hubService.OnReceiveCardSwapAnimationDetails -= HandleReceiveCardSwapAnimationDetails;
            _hubService.OnStartGameCountdown -= HandleStartGameCountdown;
            _hubService.OnCardPlayed -= HandleCardPlayedNotificationClient;
        }

        private void HandleHubTurnChangedAsyncWrapper(BoardDto newBoard, Player nextPlayer, GameStatusDto statusForNextPlayer, string? lastMoveFromServerFrom, string? lastMoveFromServerTo, List<AffectedSquareInfo>? cardEffectSquaresFromServer)
        {
            // Führt die asynchrone Methode "fire-and-forget" aus.
            // Die Fehlerbehandlung findet innerhalb der Methode statt.
            _ = HandleHubTurnChangedAsync(newBoard, nextPlayer, statusForNextPlayer, lastMoveFromServerFrom, lastMoveFromServerTo, cardEffectSquaresFromServer);
        }

        private async Task HandleHubTurnChangedAsync(BoardDto newBoard, Player nextPlayer, GameStatusDto statusForNextPlayer, string? lastMoveFromServerFrom, string? lastMoveFromServerTo, List<AffectedSquareInfo>? cardEffectSquaresFromServer)
        {
            _logger.LogHandleHubTurnChangedClientInfo(nextPlayer, statusForNextPlayer, lastMoveFromServerFrom, lastMoveFromServerTo, cardEffectSquaresFromServer?.Count ?? 0);
            if (_cardState.IsAwaitingTurnConfirmation)
            {
                _cardState.SetAwaitingTurnConfirmation(false);
                _logger.LogAwaitingTurnConfirmationStatus(false, "[HubSubs] Server turn confirmation received.");
            }

            Player playerWhoseTurnItWas = _gameCoreState.CurrentTurnPlayer ??
            Player.None;

            _gameCoreState.UpdateBoard(newBoard);
            _gameCoreState.SetCurrentTurnPlayer(nextPlayer);

            bool highlightLogicWasHandledByExtraTurn = false;
            if (_gameCoreState.IsExtraTurnSequenceActive && _gameCoreState.CurrentPlayerInfo != null && _gameCoreState.MyColor == playerWhoseTurnItWas)
            {
                _gameCoreState.IncrementExtraTurnMovesMade();
                bool isThisTheThirdMoveOverallByMe = (_gameCoreState.ExtraTurnMovesMade == 2);
                _highlightState.SetHighlights(lastMoveFromServerFrom, lastMoveFromServerTo, true, isThisTheThirdMoveOverallByMe);
                highlightLogicWasHandledByExtraTurn = true;
                if (isThisTheThirdMoveOverallByMe || _gameCoreState.MyColor != nextPlayer)
                {
                    _gameCoreState.SetExtraTurnSequenceActive(false);
                }
            }

            if (!highlightLogicWasHandledByExtraTurn)
            {
                if (cardEffectSquaresFromServer != null && cardEffectSquaresFromServer.Count != 0)
                {
                    _highlightState.SetHighlightForCardEffect(
                        cardEffectSquaresFromServer.Select(eff => (eff.Square, eff.Type)).ToList()
                    );
                }
                else
                {
                    _highlightState.SetHighlights(lastMoveFromServerFrom, lastMoveFromServerTo, false);
                }
                _gameCoreState.SetExtraTurnSequenceActive(false);
            }

            bool isModalInteractionPendingForRebirth = _modalState.ShowPieceSelectionModal && _cardState.ActiveCardForBoardSelection?.Id == CardConstants.Wiedergeburt;
            if (_cardState.IsCardActivationPending && !isModalInteractionPendingForRebirth)
            {
                _cardState.ResetCardActivationState(true, "Zug gewechselt, Kartenauswahl abgebrochen.");
            }

            _cardState.DeselectActiveHandCard();
            _gameCoreState.SetOpponentJoined(true);
            ProcessEndGameStatus(nextPlayer, statusForNextPlayer);

            await ProcessGameStatusAsync(statusForNextPlayer, _gameCoreState.MyColor == nextPlayer);
        }

        private void HandleCardPlayedNotificationClient(Guid playingPlayerId, CardDto playedCardFullDefinition)
        {
            if (_gameCoreState.CurrentPlayerInfo == null) return;
            var playedCardInfo = new PlayedCardInfo { CardDefinition = playedCardFullDefinition, PlayerId = playingPlayerId, Timestamp = DateTime.UtcNow };
            if (playingPlayerId == _gameCoreState.CurrentPlayerInfo.Id)
            {
                _cardState.HandleCardPlayedByMe(playedCardFullDefinition.InstanceId, playedCardFullDefinition.Id);
                _cardState.AddToMyPlayedHistory(playedCardInfo);
                _ = _uiState.SetCurrentInfoMessageForBoxAsync($"Du hast Karte '{playedCardFullDefinition.Name}' gespielt!", true, 3000);
                if (playedCardFullDefinition.Id == CardConstants.ExtraZug)
                {
                    _gameCoreState.SetExtraTurnSequenceActive(true);
                    _highlightState.SetHighlights(null, null, false);
                }
            }
            else
            {
                _cardState.AddToOpponentPlayedHistory(playedCardInfo);
                _ = _uiState.SetCurrentInfoMessageForBoxAsync($"Gegner hat Karte '{playedCardFullDefinition.Name}' gespielt!", true, 3000);
            }

            if (_modalState.ShowCardInfoPanelModal && _modalState.CardForInfoPanelModal?.InstanceId == playedCardFullDefinition.InstanceId)
            {
                _modalState.CloseCardInfoPanelModal();
            }

            if (_cardState.IsCardActivationPending && (_cardState.ActiveCardForBoardSelection?.InstanceId == playedCardFullDefinition.InstanceId || _cardState.SelectedCardInstanceIdInHand == playedCardFullDefinition.InstanceId))
            {
                _cardState.ResetCardActivationState(false, "Kartenaktion abgeschlossen.");
            }
        }

        private async void HandleStartGameCountdown()
        {
            _uiState.ShowCountdown("3");
            await Task.Delay(1000);
            _uiState.ShowCountdown("2");
            await Task.Delay(1000);
            _uiState.ShowCountdown("1");
            await Task.Delay(1000);
            _uiState.ShowCountdown("Spiel beginnt!");

            _cardState.RevealCards();
            _gameCoreState.SetGameRunning(true);
            _logger.LogCardsRevealed(_gameCoreState.GameId);

            await Task.Delay(800);
            _uiState.HideCountdown();
        }

        private void HandleReceiveInitialHand(InitialHandDto initialHandDto) => _cardState.SetInitialHand(initialHandDto);
        private void HandleUpdateHandContents(InitialHandDto newHandInfo)
        {
            _cardState.UpdateHandAndDrawPile(newHandInfo);
            _ = _uiState.SetCurrentInfoMessageForBoxAsync("Deine Handkarten wurden aktualisiert.", true, 3000);
        }

        private void HandleCardAddedToHand(CardDto drawnCard, int newDrawPileCount)
        {
            _cardState.AddReceivedCardToHand(drawnCard, newDrawPileCount);
            if (!drawnCard.Name.Contains(CardConstants.NoMoreCardsName) && !drawnCard.Name.Contains(CardConstants.ReplacementCardName))
            {
                _ = _uiState.SetCurrentInfoMessageForBoxAsync($"Neue Karte '{drawnCard.Name}' erhalten!", true, 3000);
            }
            else
            {
                _ = _uiState.SetCurrentInfoMessageForBoxAsync(drawnCard.Description, true, 5000);
            }
        }

        private void HandlePlayerEarnedCardDrawNotification(Guid playerIdToDraw)
        {
            if (_gameCoreState.CurrentPlayerInfo != null && playerIdToDraw == _gameCoreState.CurrentPlayerInfo.Id)
            {
                _logger.LogClientSignalRConnectionWarning($"[HubSubs] Info: Server hat einen Kartenzug für mich veranlasst (PlayerId: {playerIdToDraw}).");
            }
        }

        private void HandleReceiveCardSwapAnimationDetails(CardSwapAnimationDetailsDto details)
        {
            if (_gameCoreState.CurrentPlayerInfo != null && details.PlayerId == _gameCoreState.CurrentPlayerInfo.Id)
            {
                // Prüfen, ob die generische Animation bereits fertig ist und auf diese Details wartet.
                if (_animationState.IsGenericAnimationFinishedForSwap)
                {
                    // Ja, sie hat gewartet.
                    // Starte die Tausch-Animation direkt.
                    _animationState.StartCardSwapAnimation(details.CardGiven, details.CardReceived);
                    _animationState.SetGenericAnimationFinishedForSwap(false); // Flag zurücksetzen
                }
                else
                {
                    // Nein, die generische Animation läuft noch.
                    // Speichere die Details für später.
                    _animationState.SetPendingSwapAnimationDetails(details);
                }
                _logger.LogHandleReceiveCardSwapDetails(details.CardGiven.Name, details.CardReceived.Name, _animationState.IsCardActivationAnimating);
            }
        }

        private void HandlePlayCardActivationAnimation(CardDto cardForAnimation, Guid playerIdActivating, Player playerColorActivating)
        {
            _logger.LogHandlePlayCardActivationAnimation(cardForAnimation.Id, playerIdActivating, playerColorActivating);
            if (_gameCoreState.CurrentPlayerInfo != null)
            {
                _animationState.SetLastAnimatedCard(cardForAnimation);
                _animationState.StartCardActivationAnimation(cardForAnimation, _gameCoreState.MyColor == playerColorActivating);
                _ = _uiState.SetCurrentInfoMessageForBoxAsync($"Karte '{cardForAnimation.Name}' wird aktiviert...");
                _logger.LogGenericCardAnimationStartedForCard(cardForAnimation.Name);
            }
        }

        private async void HandlePlayerJoinedClient(string playerName, int playerCount)
        {
            // Der Name wird nur als Gegnername gesetzt, wenn der empfangene
            // Name NICHT der Name des eigenen Spielers ist. Dies funktioniert für beide Clients.
            if (_gameCoreState.CurrentPlayerInfo != null && _gameCoreState.CurrentPlayerInfo.Name != playerName)
            {
                if (_gameCoreState.MyColor != Player.None)
                {
                    Player opponentColor = _gameCoreState.MyColor.Opponent();
                    _gameCoreState.SetPlayerName(opponentColor, playerName);
                }
            }

            if (playerCount == 2)
            {
                _modalState.CloseInviteLinkModal();
                _gameCoreState.SetOpponentJoined(true);
                await _uiState.SetCurrentInfoMessageForBoxAsync($"Spieler '{playerName}' ist beigetreten. Das Spiel kann beginnen!");
            }
            else
            {
                await _uiState.SetCurrentInfoMessageForBoxAsync($"Spieler '{playerName}' ist beigetreten. Warte auf weiteren Spieler...");
            }
        }

        private async void HandlePlayerLeftClient(string playerName, int playerCount)
        {
            if (playerCount < 2)
            {
                _gameCoreState.SetOpponentJoined(false);
                await _uiState.SetCurrentInfoMessageForBoxAsync($"Spieler '{playerName}' hat das Spiel verlassen.");
                if (string.IsNullOrEmpty(_gameCoreState.EndGameMessage))
                {
                    _gameCoreState.SetEndGameMessage($"Spieler '{playerName}' hat aufgegeben. Du gewinnst!");
                }
            }
        }

        private void HandleHubTimeUpdate(TimeUpdateDto timeUpdateDto)
        {
            if (!_gameCoreState.IsGameRunning) return;
            _gameCoreState.UpdateDisplayedTimes(timeUpdateDto.WhiteTime, timeUpdateDto.BlackTime, timeUpdateDto.PlayerWhoseTurnItIs);
            if (timeUpdateDto.PlayerWhoseTurnItIs.HasValue)
            {
                _gameCoreState.SetCurrentTurnPlayer(timeUpdateDto.PlayerWhoseTurnItIs.Value);
            }
        }

        private void ProcessEndGameStatus(Player nextPlayer, GameStatusDto status)
        {
            if (string.IsNullOrEmpty(_gameCoreState.EndGameMessage))
            {
                string message = status switch
                {
                    GameStatusDto.Checkmate => _gameCoreState.MyColor == nextPlayer ? "Schachmatt! Du hast verloren." : "Schachmatt! Du hast gewonnen!",
                    GameStatusDto.TimeOut => _gameCoreState.MyColor == nextPlayer.Opponent() ? "Zeit abgelaufen! Du hast verloren." : "Zeit abgelaufen! Du hast gewonnen!",
                    GameStatusDto.Stalemate => "Patt! Unentschieden.",
                    GameStatusDto.Draw50MoveRule or GameStatusDto.DrawInsufficientMaterial or GameStatusDto.DrawThreefoldRepetition => "Unentschieden!",
                    _ => string.Empty
                };
                if (!string.IsNullOrEmpty(message))
                {
                    _gameCoreState.SetEndGameMessage(message);
                }
            }
        }

        private async Task ProcessGameStatusAsync(GameStatusDto status, bool isMyCheckStatus)
        {
            if (isMyCheckStatus)
            {
                if (status == GameStatusDto.Check)
                {
                    await _uiState.SetCurrentInfoMessageForBoxAsync("Du stehst im Schach!");
                }
                else if (_uiState.CurrentInfoMessageForBox == "Du stehst im Schach!")
                {
                    _uiState.ClearCurrentInfoMessageForBox();
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            UnsubscribeFromHubEvents();
            await Task.CompletedTask;
            GC.SuppressFinalize(this);
        }
    }
}