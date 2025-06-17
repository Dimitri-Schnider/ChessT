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
    // Dieser Dienst ist verantwortlich für das Abonnieren und Behandeln von Echtzeit-Nachrichten vom SignalR Hub.
    // Er agiert als Brücke zwischen dem Hub und den clientseitigen State-Containern.
    public class HubSubscriptionService : IAsyncDisposable
    {
        // Alle benötigten Dienste und State-Container.
        private readonly ChessHubService _hubService;
        private readonly IGameCoreState _gameCoreState;
        private readonly ICardState _cardState;
        private readonly IUiState _uiState;
        private readonly IAnimationState _animationState;
        private readonly IHighlightState _highlightState;
        private readonly IModalState _modalState;
        private readonly IChessLogger _logger;

        // Konstruktor zur Initialisierung der Dienste.
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

        // Initialisiert den Dienst, indem die Hub-Events abonniert werden.
        public void Initialize()
        {
            UnsubscribeFromHubEvents(); // Stellt sicher, dass keine alten Abonnements bestehen.
            SubscribeToHubEvents();
        }

        // Abonniert alle relevanten Events vom ChessHubService.
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

        // Entfernt alle Abonnements, um Memory Leaks zu vermeiden.
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

        // Wrapper-Methode, um die asynchrone Behandlung des OnTurnChanged-Events sicher aufzurufen.
        private void HandleHubTurnChangedAsyncWrapper(BoardDto newBoard, Player nextPlayer, GameStatusDto statusForNextPlayer, string? lastMoveFromServerFrom, string? lastMoveFromServerTo, List<AffectedSquareInfo>? cardEffectSquaresFromServer)
        {
            // Führt die asynchrone Methode "fire-and-forget" aus. Die Fehlerbehandlung findet innerhalb der Methode statt.
            _ = HandleHubTurnChangedAsync(newBoard, nextPlayer, statusForNextPlayer, lastMoveFromServerFrom, lastMoveFromServerTo, cardEffectSquaresFromServer);
        }

        // Die zentrale Methode zur Verarbeitung von Spielzugs-Updates vom Server.
        private async Task HandleHubTurnChangedAsync(BoardDto newBoard, Player nextPlayer, GameStatusDto statusForNextPlayer, string? lastMoveFromServerFrom, string? lastMoveFromServerTo, List<AffectedSquareInfo>? cardEffectSquaresFromServer)
        {
            _logger.LogHandleHubTurnChangedClientInfo(nextPlayer, statusForNextPlayer, lastMoveFromServerFrom, lastMoveFromServerTo, cardEffectSquaresFromServer?.Count ?? 0);

            // Behandelt die Bestätigung eines optimistischen Zugs.
            if (_gameCoreState.IsAwaitingMoveConfirmation)
            {
                // Der Zustand wird mit der Wahrheit des Servers überschrieben.
                _gameCoreState.ConfirmOptimisticMove(newBoard);
            }
            else
            {
                // Normales Update des Bretts, wenn kein optimistischer Zug anstand.
                _gameCoreState.UpdateBoard(newBoard);
            }

            // Setzt den Wartezustand nach einer Kartenaktion zurück.
            if (_cardState.IsAwaitingTurnConfirmation)
            {
                _cardState.SetAwaitingTurnConfirmation(false);
            }

            Player playerWhoseTurnItWas = _gameCoreState.CurrentTurnPlayer ?? Player.None;
            _gameCoreState.SetCurrentTurnPlayer(nextPlayer);

            // Spezielle Logik zur Handhabung der Highlights während eines "Extrazuges".
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

            // Standard-Highlight-Logik für normale Züge oder Karteneffekte.
            if (!highlightLogicWasHandledByExtraTurn)
            {
                if (cardEffectSquaresFromServer != null && cardEffectSquaresFromServer.Count != 0)
                {
                    _highlightState.SetHighlightForCardEffect(cardEffectSquaresFromServer.Select(eff => (eff.Square, eff.Type)).ToList());
                }
                else
                {
                    _highlightState.SetHighlights(lastMoveFromServerFrom, lastMoveFromServerTo, false);
                }
                _gameCoreState.SetExtraTurnSequenceActive(false);
            }

            // Bricht eine eventuell laufende Kartenauswahl ab, wenn der Zug wechselt.
            bool isModalInteractionPendingForRebirth = _modalState.ShowPieceSelectionModal && _cardState.ActiveCardForBoardSelection?.Id == CardConstants.Wiedergeburt;
            if (_cardState.IsCardActivationPending && !isModalInteractionPendingForRebirth)
            {
                _cardState.ResetCardActivationState(true, "Zug gewechselt, Kartenauswahl abgebrochen.");
            }

            _cardState.DeselectActiveHandCard();
            _gameCoreState.SetOpponentJoined(true);

            // Prüft auf Spielende-Bedingungen und aktualisiert die UI-Nachrichten.
            ProcessEndGameStatus(nextPlayer, statusForNextPlayer);
            await ProcessGameStatusAsync(statusForNextPlayer, _gameCoreState.MyColor == nextPlayer);
        }

        // Verarbeitet die Benachrichtigung, dass eine Karte gespielt wurde.
        private void HandleCardPlayedNotificationClient(Guid playingPlayerId, CardDto playedCardFullDefinition)
        {
            if (_gameCoreState.CurrentPlayerInfo == null) return;
            var playedCardInfo = new PlayedCardInfo { CardDefinition = playedCardFullDefinition, PlayerId = playingPlayerId, Timestamp = DateTime.UtcNow };

            // Unterscheidet, ob ich oder der Gegner die Karte gespielt hat.
            if (playingPlayerId == _gameCoreState.CurrentPlayerInfo.Id)
            {
                _cardState.HandleCardPlayedByMe(playedCardFullDefinition.InstanceId, playedCardFullDefinition.Id);
                _cardState.AddToMyPlayedHistory(playedCardInfo);
                _ = _uiState.SetCurrentInfoMessageForBoxAsync($"Du hast Karte '{playedCardFullDefinition.Name}' gespielt!", true, 3000);
                if (playedCardFullDefinition.Id == CardConstants.ExtraZug)
                {
                    _gameCoreState.SetExtraTurnSequenceActive(true);
                }
            }
            else
            {
                _cardState.AddToOpponentPlayedHistory(playedCardInfo);
                _ = _uiState.SetCurrentInfoMessageForBoxAsync($"Gegner hat Karte '{playedCardFullDefinition.Name}' gespielt!", true, 3000);
            }
        }

        // Startet den visuellen Countdown vor Spielbeginn.
        private async void HandleStartGameCountdown()
        {
            _uiState.ShowCountdown("3");
            await Task.Delay(1000);
            _uiState.ShowCountdown("2");
            await Task.Delay(1000);
            _uiState.ShowCountdown("1");
            await Task.Delay(1000);
            _uiState.ShowCountdown("Spiel beginnt!");

            // Deckt die Karten auf und markiert das Spiel als laufend.
            _cardState.RevealCards();
            _gameCoreState.SetGameRunning(true);
            _logger.LogCardsRevealed(_gameCoreState.GameId);

            await Task.Delay(800);
            _uiState.HideCountdown();
        }

        // Setzt die initiale Hand und den Nachziehstapel zu Beginn des Spiels.
        private void HandleReceiveInitialHand(InitialHandDto initialHandDto) => _cardState.SetInitialHand(initialHandDto);

        // Aktualisiert die Handkarten, z.B. nach einem Kartentausch.
        private void HandleUpdateHandContents(InitialHandDto newHandInfo)
        {
            _cardState.UpdateHandAndDrawPile(newHandInfo);
            _ = _uiState.SetCurrentInfoMessageForBoxAsync("Deine Handkarten wurden aktualisiert.", true, 3000);
        }

        // Fügt eine neu gezogene Karte der Hand hinzu.
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

        // Wird vom Server aufgerufen, um den Client zu informieren, dass ein Kartenzug stattgefunden hat (primär für Logging).
        private void HandlePlayerEarnedCardDrawNotification(Guid playerIdToDraw)
        {
            if (_gameCoreState.CurrentPlayerInfo != null && playerIdToDraw == _gameCoreState.CurrentPlayerInfo.Id)
            {
                _logger.LogClientSignalRConnectionWarning($"[HubSubs] Info: Server hat einen Kartenzug für mich veranlasst (PlayerId: {playerIdToDraw}).");
            }
        }

        // Empfängt die Details für die Kartentausch-Animation.
        private void HandleReceiveCardSwapAnimationDetails(CardSwapAnimationDetailsDto details)
        {
            if (_gameCoreState.CurrentPlayerInfo != null && details.PlayerId == _gameCoreState.CurrentPlayerInfo.Id)
            {
                // Prüft, ob die generische Animation bereits beendet ist und auf diese Details wartet.
                if (_animationState.IsGenericAnimationFinishedForSwap)
                {
                    _animationState.StartCardSwapAnimation(details.CardGiven, details.CardReceived);
                    _animationState.SetGenericAnimationFinishedForSwap(false);
                }
                else
                {
                    // Speichert die Details, da die generische Animation noch läuft.
                    _animationState.SetPendingSwapAnimationDetails(details);
                }
            }
        }

        // Startet die generische Kartenaktivierungs-Animation.
        private void HandlePlayCardActivationAnimation(CardDto cardForAnimation, Guid playerIdActivating, Player playerColorActivating)
        {
            if (_gameCoreState.CurrentPlayerInfo != null)
            {
                _animationState.StartCardActivationAnimation(cardForAnimation, _gameCoreState.MyColor == playerColorActivating);
                _ = _uiState.SetCurrentInfoMessageForBoxAsync($"Karte '{cardForAnimation.Name}' wird aktiviert...");
            }
        }

        // Aktualisiert die Spielerinformationen, wenn ein Gegner beitritt.
        private async void HandlePlayerJoinedClient(string playerName, int playerCount)
        {
            if (_gameCoreState.CurrentPlayerInfo != null && _gameCoreState.CurrentPlayerInfo.Name != playerName)
            {
                Player opponentColor = _gameCoreState.MyColor.Opponent();
                _gameCoreState.SetPlayerName(opponentColor, playerName);
            }

            if (playerCount == 2)
            {
                _modalState.CloseInviteLinkModal();
                _gameCoreState.SetOpponentJoined(true);
                await _uiState.SetCurrentInfoMessageForBoxAsync($"Spieler '{playerName}' ist beigetreten. Das Spiel kann beginnen!");
            }
        }

        // Behandelt den Fall, dass ein Spieler das Spiel verlässt.
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

        // Aktualisiert die Timer-Anzeigen.
        private void HandleHubTimeUpdate(TimeUpdateDto timeUpdateDto)
        {
            if (!_gameCoreState.IsGameRunning) return;
            _gameCoreState.UpdateDisplayedTimes(timeUpdateDto.WhiteTime, timeUpdateDto.BlackTime, timeUpdateDto.PlayerWhoseTurnItIs);
            if (timeUpdateDto.PlayerWhoseTurnItIs.HasValue)
            {
                _gameCoreState.SetCurrentTurnPlayer(timeUpdateDto.PlayerWhoseTurnItIs.Value);
            }
        }

        // Verarbeitet den Spielende-Status und setzt die Endspiel-Nachricht.
        private void ProcessEndGameStatus(Player nextPlayer, GameStatusDto status)
        {
            if (string.IsNullOrEmpty(_gameCoreState.EndGameMessage))
            {
                string message = status switch
                {
                    GameStatusDto.Checkmate => _gameCoreState.MyColor == nextPlayer ? "Schachmatt! Du hast verloren." : "Schachmatt! Du hast gewonnen!",
                    GameStatusDto.TimeOut => _gameCoreState.MyColor == nextPlayer ? "Zeit abgelaufen! Du hast verloren." : "Zeit abgelaufen! Du hast gewonnen!",
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

        // Verarbeitet den Schach-Status und zeigt eine entsprechende Nachricht an.
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

        // Gibt die abonnierten Hub-Events frei.
        public async ValueTask DisposeAsync()
        {
            UnsubscribeFromHubEvents();
            await Task.CompletedTask;
            GC.SuppressFinalize(this);
        }
    }
}