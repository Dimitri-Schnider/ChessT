// File: [SolutionDir]\ChessClient\Services\GameOrchestrationService.cs
using Chess.Logging;
using ChessClient.Configuration;
using ChessClient.Extensions;
using ChessClient.Models;
using ChessClient.State;
using ChessClient.Utils;
using ChessLogic;
using ChessNetwork;
using ChessNetwork.Configuration;
using ChessNetwork.DTOs;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace ChessClient.Services
{
    public class GameOrchestrationService
    {
        private readonly IGameSession _gameService;
        private readonly IGameCoreState _gameCoreState;
        private readonly IUiState _uiState;
        private readonly IModalState _modalState;
        private readonly IHighlightState _highlightState;
        private readonly ICardState _cardState;
        private readonly IChessLogger _logger;
        private readonly ChessHubService _hubService;
        private readonly IConfiguration _configuration;

        public GameOrchestrationService(
            IGameSession gameService,
            IGameCoreState gameCoreState,
            IUiState uiState,
            IModalState modalState,
            IHighlightState highlightState,
            ICardState cardState,
            IChessLogger logger,
            ChessHubService hubService,
            IConfiguration configuration)
        {
            _gameService = gameService;
            _gameCoreState = gameCoreState;
            _uiState = uiState;
            _modalState = modalState;
            _highlightState = highlightState;
            _cardState = cardState;
            _logger = logger;
            _hubService = hubService;
            _configuration = configuration;
        }

        // Die Logik wurde in zwei Methoden aufgeteilt. Diese ist jetzt privat.
        private async Task<(bool Success, Guid GameId)> CreateNewGameAsync(CreateGameParameters args)
        {
            _gameCoreState.SetGameSpecificDataInitialized(false);
            if (string.IsNullOrWhiteSpace(args.Name))
            {
                _uiState.SetErrorMessage("Bitte gib einen Spielernamen ein.");
                return (false, Guid.Empty);
            }
            _uiState.ClearErrorMessage();
            _uiState.ClearCurrentInfoMessageForBox();
            _highlightState.ClearAllActionHighlights();
            try
            {
                var createGameDtoForServer = new CreateGameDto
                {
                    PlayerName = args.Name,
                    Color = args.Color,
                    InitialMinutes = args.TimeMinutes,
                    OpponentType = args.OpponentType.ToString(),
                    ComputerDifficulty = args.ComputerDifficulty.ToString()
                };
                var result = await _gameService.CreateGameAsync(createGameDtoForServer);
                _gameCoreState.InitializeNewGame(result, args.Name, result.Color, args.TimeMinutes, args.OpponentType.ToString());
                _modalState.UpdateCreateGameModalArgs(args.Name, args.Color, args.TimeMinutes);
                _modalState.CloseCreateGameModal();

                await ConnectAndRegisterToHubAsync(result.GameId, result.PlayerId);

                return (true, _gameCoreState.GameId);
            }
            catch (Exception ex)
            {
                _uiState.SetErrorMessage($"Fehler beim Erstellen des Spiels: {ex.Message}");
                return (false, Guid.Empty);
            }
        }

        // Methode nur für die API-Erstellung
        public async Task<CreateGameResultDto?> CreateGameOnServerAsync(CreateGameParameters args)
        {
            _gameCoreState.SetGameSpecificDataInitialized(false);
            if (string.IsNullOrWhiteSpace(args.Name))
            {
                _uiState.SetErrorMessage("Bitte gib einen Spielernamen ein.");
                return null;
            }
            _uiState.ClearErrorMessage();
            _uiState.ClearCurrentInfoMessageForBox();
            _highlightState.ClearAllActionHighlights();
            try
            {
                var createGameDtoForServer = new CreateGameDto
                {
                    PlayerName = args.Name,
                    Color = args.Color,
                    InitialMinutes = args.TimeMinutes,
                    OpponentType = args.OpponentType.ToString(),
                    ComputerDifficulty = args.ComputerDifficulty.ToString()
                };
                var result = await _gameService.CreateGameAsync(createGameDtoForServer);
                _gameCoreState.InitializeNewGame(result, args.Name, result.Color, args.TimeMinutes, args.OpponentType.ToString());
                _modalState.UpdateCreateGameModalArgs(args.Name, args.Color, args.TimeMinutes);

                return result;
            }
            catch (Exception ex)
            {
                _uiState.SetErrorMessage($"Fehler beim Erstellen des Spiels: {ex.Message}");
                return null;
            }
        }

        // Methode nur für die Hub-Registrierung
        public async Task ConnectAndRegisterPlayerToHubAsync(Guid gameId, Guid playerId)
        {
            await ConnectAndRegisterToHubAsync(gameId, playerId);
        }

        public async Task<(bool Success, Guid GameId)> JoinExistingGameAsync(string name, string gameIdToJoin)
        {
            _gameCoreState.SetGameSpecificDataInitialized(false);
            if (string.IsNullOrWhiteSpace(name))
            {
                _uiState.SetErrorMessage("Bitte gib einen Spielernamen ein.");
                return (false, Guid.Empty);
            }
            if (!Guid.TryParse(gameIdToJoin, out var parsedGuidForJoin))
            {
                _uiState.SetErrorMessage("Ungültiges Game-ID Format.");
                return (false, Guid.Empty);
            }

            _uiState.ClearErrorMessage();
            _uiState.ClearCurrentInfoMessageForBox();
            _highlightState.ClearAllActionHighlights();
            _modalState.UpdateJoinGameModalArgs(name, gameIdToJoin);
            try
            {
                var result = await _gameService.JoinGameAsync(parsedGuidForJoin, name);
                _gameCoreState.InitializeJoinedGame(result, parsedGuidForJoin, result.Color);
                _gameCoreState.SetOpponentJoined(true);
                _gameCoreState.SetIsPvCGame(false);
                _modalState.CloseJoinGameModal();

                await ConnectAndRegisterToHubAsync(parsedGuidForJoin, result.PlayerId);

                return (true, _gameCoreState.GameId);
            }
            catch (Exception ex)
            {
                _uiState.SetErrorMessage($"Fehler beim Beitreten zum Spiel: {ex.Message}");
                _gameCoreState.SetOpponentJoined(false);
                return (false, Guid.Empty);
            }
        }


        private async Task ConnectAndRegisterToHubAsync(Guid gameId, Guid playerId)
        {
            if (!_hubService.IsConnected)
            {
                string? serverBaseUrl = _configuration.GetValue<string>("ServerBaseUrl") ?? ClientConstants.DefaultServerBaseUrl;
                await _hubService.StartAsync($"{serverBaseUrl.TrimEnd('/')}{ClientConstants.ChessHubRelativePath}");
            }
            if (_hubService.IsConnected)
            {
                await _hubService.RegisterPlayerWithHubAsync(gameId, playerId);
            }
            else
            {
                _logger.LogClientSignalRConnectionWarning("SignalR konnte nicht verbunden werden.");
            }
        }

        public async Task ProcessPlayerMoveAsync(MoveDto clientMove)
        {
            if (_uiState.IsCountdownVisible || !_cardState.AreCardsRevealed || _modalState.ShowPieceSelectionModal || _modalState.ShowCardInfoPanelModal || _cardState.IsCardActivationPending || _gameCoreState.CurrentPlayerInfo == null || _gameCoreState.GameId == Guid.Empty || !string.IsNullOrEmpty(_gameCoreState.EndGameMessage) || !_gameCoreState.OpponentJoined || _gameCoreState.MyColor != _gameCoreState.CurrentTurnPlayer)
            {
                return;
            }

            if (IsPawnPromotion(clientMove.From, clientMove.To))
            {
                _modalState.OpenPawnPromotionModal(clientMove, _gameCoreState.MyColor);
                await _uiState.SetCurrentInfoMessageForBoxAsync("Wähle eine Figur für die Umwandlung.");
                return;
            }

            try
            {
                var moveToSend = new MoveDto(clientMove.From, clientMove.To, _gameCoreState.CurrentPlayerInfo.Id, clientMove.PromotionTo);
                var serverResult = await _gameService.SendMoveAsync(_gameCoreState.GameId, moveToSend);
                if (!serverResult.IsValid)
                {
                    _uiState.SetErrorMessage(serverResult.ErrorMessage ?? "Unbekannter Fehler beim Zug vom Server.");
                    _highlightState.ClearAllActionHighlights();
                }
                else
                {
                    _uiState.ClearErrorMessage();
                }
            }
            catch (Exception ex)
            {
                _uiState.SetErrorMessage($"Fehler beim Senden des Zugs: {ex.Message}");
                _highlightState.ClearAllActionHighlights();
            }
        }

        public async Task ProcessPawnPromotionAsync(PieceType promotionType)
        {
            if (_modalState.PendingPromotionMove == null || _gameCoreState.CurrentPlayerInfo == null)
            {
                _uiState.SetErrorMessage("Fehler bei der Bauernumwandlung: Zustand inkonsistent.");
                _modalState.ClosePawnPromotionModal();
                return;
            }

            _modalState.ClosePawnPromotionModal();
            MoveDto pendingMove = _modalState.PendingPromotionMove;
            _modalState.ClearPendingPromotionMove();

            MoveDto moveWithPromotion = new(pendingMove.From, pendingMove.To, _gameCoreState.CurrentPlayerInfo.Id, promotionType);
            _logger.LogClientSignalRConnectionWarning($"[GOS] Attempting Pawn Promotion. From: {moveWithPromotion.From}, To: {moveWithPromotion.To}, Promotion: {moveWithPromotion.PromotionTo}");
            await _uiState.SetCurrentInfoMessageForBoxAsync($"Figur wird zu {promotionType} umgewandelt...");

            try
            {
                MoveResultDto result = await _gameService.SendMoveAsync(_gameCoreState.GameId, moveWithPromotion);
                if (!result.IsValid)
                {
                    _uiState.SetErrorMessage(result.ErrorMessage ?? "Unbekannter Fehler bei der Bauernumwandlung vom Server.");
                }
            }
            catch (Exception ex)
            {
                _uiState.SetErrorMessage($"Fehler beim Senden des Umwandlungszugs: {ex.Message}");
            }
        }

        public async Task ActivateCardAsync(CardDto cardToActivate)
        {
            if (!IsCardActivatable(cardToActivate))
            {
                await SetCardActionInfoBoxMessage($"Karte '{cardToActivate.Name}' kann momentan nicht aktiviert werden.", false);
                _cardState.SetIsCardActivationPending(false);
                return;
            }

            _cardState.StartCardActivation(cardToActivate);

            switch (cardToActivate.Id)
            {
                case CardConstants.Teleport:
                    await SetCardActionInfoBoxMessage("Teleport: Wähle eine deiner Figuren auf dem Brett aus.", true);
                    break;
                case CardConstants.Positionstausch:
                    await SetCardActionInfoBoxMessage("Positionstausch: Wähle deine erste Figur auf dem Brett aus.", true);
                    break;
                case CardConstants.SacrificeEffect:
                    await HandleSacrificeCardActivationAsync(cardToActivate);
                    break;
                case CardConstants.Wiedergeburt:
                    await HandleRebirthCardActivationAsync(cardToActivate);
                    break;
                default:
                    ActivateCardRequestDto requestDto = new ActivateCardRequestDto
                    {
                        CardInstanceId = cardToActivate.InstanceId,
                        CardTypeId = cardToActivate.Id
                    };
                    if (cardToActivate.Id == CardConstants.CardSwap)
                    {
                        requestDto.CardInstanceIdToSwapFromHand = _cardState.PlayerHandCards?.FirstOrDefault(c => c.InstanceId != cardToActivate.InstanceId)?.InstanceId;
                    }
                    await SetCardActionInfoBoxMessage($"Aktiviere Karte '{cardToActivate.Name}'...", false);
                    _cardState.SetAwaitingTurnConfirmation(cardToActivate.Id != CardConstants.ExtraZug);
                    _cardState.DeselectActiveHandCard();
                    await FinalizeCardActivationOnServerAsync(requestDto, cardToActivate);
                    break;
            }
        }

        private async Task HandleSacrificeCardActivationAsync(CardDto card)
        {
            _cardState.SetAwaitingSacrificePawnSelection(true);
            var pawnSquares = new List<string>();
            if (_gameCoreState.BoardDto != null)
            {
                for (int r = 0; r < 8; r++)
                    for (int c = 0; c < 8; c++)
                    {
                        var p = _gameCoreState.BoardDto.Squares[r][c];
                        if (p.HasValue && p.Value.IsOfPlayerColor(_gameCoreState.MyColor) && p.Value.ToString().Contains("Pawn"))
                        {
                            pawnSquares.Add(PositionHelper.ToAlgebraic(r, c));
                        }
                    }
            }
            if (pawnSquares.Count > 0)
            {
                _highlightState.SetCardTargetSquaresForSelection(pawnSquares);
                await SetCardActionInfoBoxMessage("Opfergabe: Wähle einen deiner Bauern.", true);
            }
            else
            {
                await SetCardActionInfoBoxMessage("Keine Bauern zum Opfern vorhanden.", false);
                await FinalizeCardActivationOnServerAsync(new ActivateCardRequestDto { CardInstanceId = card.InstanceId, CardTypeId = card.Id }, card);
            }
        }

        private async Task HandleRebirthCardActivationAsync(CardDto card)
        {
            if (_gameCoreState.CurrentPlayerInfo == null) return;

            await _cardState.LoadCapturedPiecesForRebirthAsync(_gameCoreState.GameId, _gameCoreState.CurrentPlayerInfo.Id, _gameService);
            var choices = _cardState.CapturedPiecesForRebirth?.Select(p => p.Type).Distinct().ToList() ?? new List<PieceType>();
            if (choices.Count > 0)
            {
                var choiceInfos = choices.Select(pt => new PieceSelectionChoiceInfo(pt, true)).ToList(); // Simplified
                _modalState.OpenPieceSelectionModal("Figur wiederbeleben", "Wähle eine Figur:", choiceInfos, _gameCoreState.MyColor);
            }
            else
            {
                await SetCardActionInfoBoxMessage("Keine wiederbelebungsfähigen Figuren geschlagen.", false);
                await FinalizeCardActivationOnServerAsync(new ActivateCardRequestDto { CardInstanceId = card.InstanceId, CardTypeId = card.Id }, card);
            }
        }

        public async Task FinalizeCardActivationOnServerAsync(ActivateCardRequestDto requestDto, CardDto activatedCardDefinition)
        {
            if (_gameCoreState.CurrentPlayerInfo == null) return;
            var result = await _gameService.ActivateCardAsync(_gameCoreState.GameId, _gameCoreState.CurrentPlayerInfo.Id, requestDto);

            if (result.PawnPromotionPendingAt != null && result.Success)
            {
                _cardState.SetAwaitingTurnConfirmation(false);
                var pos = result.PawnPromotionPendingAt;
                var move = new MoveDto(PositionHelper.ToAlgebraic(pos.Row, pos.Column), PositionHelper.ToAlgebraic(pos.Row, pos.Column), _gameCoreState.CurrentPlayerInfo.Id);
                _modalState.OpenPawnPromotionModal(move, _gameCoreState.MyColor);
                await _uiState.SetCurrentInfoMessageForBoxAsync("Bauer umwandeln!");
                _cardState.SetIsCardActivationPending(true);
            }
            else
            {
                // *** BEGINN DER KORREKTUR ***
                // Prüfen, ob der Zug bereits durch SignalR verarbeitet wurde.
                // Dies ist der Fall, wenn die Karte den Zug beenden sollte, aber wir laut State schon wieder dran sind.
                bool turnAlreadyProcessedBySignalR = result.Success && result.EndsPlayerTurn && _gameCoreState.CurrentTurnPlayer == _gameCoreState.MyColor;

                // Den allgemeinen "Kartenaktivierung läuft"-Status zurücksetzen.
                _cardState.ResetCardActivationState(!result.Success, result.Success ? $"Karte '{activatedCardDefinition.Name}' erfolgreich aktiviert!" : _uiState.ErrorMessage);

                // JETZT das "Warten auf Zug"-Flag setzen, aber NUR, wenn der Zug nicht bereits verarbeitet wurde.
                if (turnAlreadyProcessedBySignalR)
                {
                    // SignalR war schneller. Der Zug ist schon da. Nicht auf Bestätigung warten.
                    _cardState.SetAwaitingTurnConfirmation(false);
                }
                else
                {
                    // Das Flag basierend auf dem HTTP-Ergebnis setzen. SignalR wird es später löschen.
                    _cardState.SetAwaitingTurnConfirmation(result.Success && result.EndsPlayerTurn);
                }
                // *** ENDE DER KORREKTUR ***
            }
        }

        private bool IsPawnPromotion(string fromAlgebraic, string toAlgebraic)
        {
            if (_gameCoreState.BoardDto == null) return false;
            (int fromRow, _) = PositionHelper.ToIndices(fromAlgebraic);
            (int toRow, _) = PositionHelper.ToIndices(toAlgebraic);
            PieceDto? piece = _gameCoreState.BoardDto.Squares[fromRow][PositionHelper.ToIndices(fromAlgebraic).Column];
            if (piece == null || !piece.Value.ToString().Contains("Pawn", StringComparison.OrdinalIgnoreCase)) return false;
            if (_gameCoreState.MyColor == Player.White && fromRow == 1 && toRow == 0) return true;
            if (_gameCoreState.MyColor == Player.Black && fromRow == 6 && toRow == 7) return true;
            return false;
        }

        private bool IsCardActivatable(CardDto? card)
        {
            if (card == null) return false;
            if (_modalState.ShowPieceSelectionModal) return false;
            if (_cardState.IsCardActivationPending && _cardState.SelectedCardInstanceIdInHand != card.InstanceId) return false;
            if (_gameCoreState.CurrentPlayerInfo == null || !_gameCoreState.OpponentJoined || _gameCoreState.MyColor != _gameCoreState.CurrentTurnPlayer || !string.IsNullOrEmpty(_gameCoreState.EndGameMessage)) return false;

            if (card.Id == CardConstants.SubtractTime)
            {
                Player opponentColor = _gameCoreState.MyColor.Opponent();
                string opponentTimeDisplay = opponentColor == Player.White ? _gameCoreState.WhiteTimeDisplay : _gameCoreState.BlackTimeDisplay;
                if (TimeSpan.TryParseExact(opponentTimeDisplay, @"mm\:ss", CultureInfo.InvariantCulture, out TimeSpan opponentTime))
                {
                    if (opponentTime < TimeSpan.FromMinutes(3)) return false;
                }
                else return false;
            }

            if (card.Id == CardConstants.CardSwap)
            {
                if (_cardState.PlayerHandCards == null || _cardState.PlayerHandCards.Count < 2)
                {
                    return false;
                }
            }
            return true;
        }

        private async Task SetCardActionInfoBoxMessage(string message, bool showCancelButton)
        {
            await _uiState.SetCurrentInfoMessageForBoxAsync(message,
                autoClear: !showCancelButton,
                durationMs: showCancelButton ? 0 : 4000,
                showActionButton: showCancelButton,
                actionButtonText: "Auswahl abbrechen",
                onActionButtonClicked: new Microsoft.AspNetCore.Components.EventCallback(null, () => _cardState.ResetCardActivationState(true, "Kartenaktion abgebrochen."))
            );
        }
    }
}