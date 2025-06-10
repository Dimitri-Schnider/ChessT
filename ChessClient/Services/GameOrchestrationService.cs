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
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
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
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

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



        // Methode nur für die API-Erstellung
        public async Task<CreateGameResultDto?> CreateGameOnServerAsync(CreateGameParameters args)
        {
            if (string.IsNullOrWhiteSpace(args.Name))
            {
                // KORREKTUR: Verwende das Modal für Fehlermeldungen.
                _modalState.OpenErrorModal("Bitte gib einen Spielernamen ein.");
                return null;
            }

            try
            {
                var createGameDto = new CreateGameDto
                {
                    PlayerName = args.Name,
                    Color = args.Color,
                    InitialMinutes = args.TimeMinutes,
                    OpponentType = args.OpponentType.ToString(),
                    ComputerDifficulty = args.ComputerDifficulty.ToString()
                };
                var result = await _gameService.CreateGameAsync(createGameDto);
                _gameCoreState.InitializeNewGame(result, args.Name, result.Color, args.TimeMinutes, args.OpponentType.ToString());
                _modalState.UpdateCreateGameModalArgs(args.Name, args.Color, args.TimeMinutes);
                return result;
            }
            catch (Exception ex)
            {
                // KORREKTUR: Verwende das Modal für Fehlermeldungen.
                _modalState.OpenErrorModal($"Fehler beim Erstellen des Spiels: {ex.Message}");
                return null;
            }
        }


        // Versucht, eine spezifische Fehlermeldung aus einer JSON-Antwort zu extrahieren.
        private static string ParseErrorMessageFromJson(string jsonContent)
        {
            try
            {
                var moveResult = JsonSerializer.Deserialize<MoveResultDto>(jsonContent, _jsonOptions);
                if (!string.IsNullOrWhiteSpace(moveResult?.ErrorMessage)) return moveResult.ErrorMessage;
            }
            catch (JsonException) { /* Ignorieren */ }

            try
            {
                var cardResult = JsonSerializer.Deserialize<ServerCardActivationResultDto>(jsonContent, _jsonOptions);
                if (!string.IsNullOrWhiteSpace(cardResult?.ErrorMessage)) return cardResult.ErrorMessage;
            }
            catch (JsonException) { /* Ignorieren */ }

            return jsonContent;
        }

        // Methode nur für die Hub-Registrierung
        public async Task ConnectAndRegisterPlayerToHubAsync(Guid gameId, Guid playerId)
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

        // NEU: Implementierung der fehlenden Methode
        public async Task HandleSquareClickForCardAsync(string algebraicCoord)
        {
            if (!_cardState.IsCardActivationPending || _cardState.ActiveCardForBoardSelection == null || _gameCoreState.CurrentPlayerInfo == null) return;

            var activeCard = _cardState.ActiveCardForBoardSelection;
            var request = new ActivateCardRequestDto { CardInstanceId = _cardState.SelectedCardInstanceIdInHand ?? Guid.Empty, CardTypeId = activeCard.Id };

            if (activeCard.Id is CardConstants.Teleport or CardConstants.Positionstausch)
            {
                if (string.IsNullOrEmpty(_cardState.FirstSquareSelectedForTeleportOrSwap))
                {
                    _cardState.SetFirstSquareForTeleportOrSwap(algebraicCoord);
                    var msg = activeCard.Id == CardConstants.Teleport ? $"Teleport: Figur auf {algebraicCoord} ausgewählt. Wähle nun ein leeres Zielfeld." : $"Positionstausch: Erste Figur auf {algebraicCoord} ausgewählt. Wähle nun deine zweite Figur.";
                    await SetCardActionInfoBoxMessage(msg, true);
                    _highlightState.SetHighlights(algebraicCoord, null, false);
                }
                else
                {
                    request.FromSquare = _cardState.FirstSquareSelectedForTeleportOrSwap;
                    request.ToSquare = algebraicCoord;
                    await FinalizeCardActivationOnServerAsync(request, activeCard);
                }
            }
            else if (activeCard.Id is CardConstants.Wiedergeburt && _cardState.IsAwaitingRebirthTargetSquareSelection)
            {
                request.PieceTypeToRevive = _cardState.PieceTypeSelectedForRebirth;
                request.TargetRevivalSquare = algebraicCoord;
                await FinalizeCardActivationOnServerAsync(request, activeCard);
            }
            else if (activeCard.Id is CardConstants.SacrificeEffect && _cardState.IsAwaitingSacrificePawnSelection)
            {
                request.FromSquare = algebraicCoord;
                await FinalizeCardActivationOnServerAsync(request, activeCard);
            }
        }

        // NEU: Implementierung der fehlenden Methode
        public async Task HandlePieceTypeSelectedFromModalAsync(PieceType selectedType)
        {
            if (_modalState.ShowPawnPromotionModalSpecifically)
            {
                await ProcessPawnPromotionAsync(selectedType);
            }
            else if (_cardState.IsCardActivationPending && _cardState.ActiveCardForBoardSelection?.Id == CardConstants.Wiedergeburt)
            {
                _modalState.ClosePieceSelectionModal();
                _cardState.SetAwaitingRebirthTargetSquareSelection(selectedType);

                List<string> originalSquares = PieceHelperClient.GetOriginalStartSquares(selectedType, _gameCoreState.MyColor);
                List<string> validTargetSquares = originalSquares.Where(s => {
                    (int r, int c) = PositionHelper.ToIndices(s);
                    return _gameCoreState.BoardDto?.Squares[r][c] == null;
                }).ToList();

                if (validTargetSquares.Count == 0)
                {
                    await _uiState.SetCurrentInfoMessageForBoxAsync($"Keine freien Ursprungsfelder für {selectedType} verfügbar. Karte verfällt.", true, 4000);
                    var request = new ActivateCardRequestDto { CardInstanceId = _cardState.SelectedCardInstanceIdInHand ?? Guid.Empty, CardTypeId = CardConstants.Wiedergeburt, PieceTypeToRevive = selectedType };
                    if (_cardState.ActiveCardForBoardSelection != null)
                    {
                        await FinalizeCardActivationOnServerAsync(request, _cardState.ActiveCardForBoardSelection);
                    }
                }
                else if (validTargetSquares.Count == 1)
                {
                    await _uiState.SetCurrentInfoMessageForBoxAsync($"Wiederbelebe {selectedType} auf {validTargetSquares[0]}...", false);
                    var request = new ActivateCardRequestDto { CardInstanceId = _cardState.SelectedCardInstanceIdInHand ?? Guid.Empty, CardTypeId = CardConstants.Wiedergeburt, PieceTypeToRevive = selectedType, TargetRevivalSquare = validTargetSquares[0] };
                    if (_cardState.ActiveCardForBoardSelection != null)
                    {
                        await FinalizeCardActivationOnServerAsync(request, _cardState.ActiveCardForBoardSelection);
                    }
                }
                else
                {
                    _highlightState.SetCardTargetSquaresForSelection(validTargetSquares);
                    await SetCardActionInfoBoxMessage($"Wähle ein leeres Ursprungsfeld für {selectedType}.", true);
                }
            }
        }

        public async Task<(bool Success, Guid GameId)> JoinExistingGameAsync(string name, string gameIdToJoin)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                // KORREKTUR: Verwende das Modal für Fehlermeldungen.
                _modalState.OpenErrorModal("Bitte gib einen Spielernamen ein.");
                return (false, Guid.Empty);
            }
            if (!Guid.TryParse(gameIdToJoin, out var parsedGuid))
            {
                // KORREKTUR: Verwende das Modal für Fehlermeldungen.
                _modalState.OpenErrorModal("Ungültiges Game-ID Format.");
                return (false, Guid.Empty);
            }

            _modalState.UpdateJoinGameModalArgs(name, gameIdToJoin);
            try
            {
                var result = await _gameService.JoinGameAsync(parsedGuid, name);
                _gameCoreState.InitializeJoinedGame(result, parsedGuid, result.Color);
                _gameCoreState.SetOpponentJoined(true);
                _gameCoreState.SetIsPvCGame(false);
                _modalState.CloseJoinGameModal();
                await ConnectAndRegisterPlayerToHubAsync(parsedGuid, result.PlayerId);
                return (true, _gameCoreState.GameId);
            }
            catch (Exception ex)
            {
                // KORREKTUR: Verwende das Modal für Fehlermeldungen.
                _modalState.OpenErrorModal($"Fehler beim Beitreten zum Spiel: {ex.Message}");
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
            if (IsPawnPromotion(clientMove.From, clientMove.To))
            {
                _modalState.OpenPawnPromotionModal(clientMove, _gameCoreState.MyColor);
                await _uiState.SetCurrentInfoMessageForBoxAsync("Wähle eine Figur für die Umwandlung.");
                return;
            }

            try
            {
                var moveToSend = new MoveDto(clientMove.From, clientMove.To, _gameCoreState.CurrentPlayerInfo!.Id, clientMove.PromotionTo);
                await _gameService.SendMoveAsync(_gameCoreState.GameId, moveToSend);
            }
            catch (HttpRequestException ex)
            {
                string friendlyMessage = ParseErrorMessageFromJson(ex.Message);
                _modalState.OpenErrorModal(friendlyMessage);
                _highlightState.ClearAllActionHighlights();
            }
            catch (Exception ex)
            {
                _modalState.OpenErrorModal($"Ein unerwarteter Fehler ist aufgetreten: {ex.Message}");
                _highlightState.ClearAllActionHighlights();
            }
        }


        public async Task ProcessPawnPromotionAsync(PieceType promotionType)
        {
            if (_modalState.PendingPromotionMove == null || _gameCoreState.CurrentPlayerInfo == null)
            {
                _modalState.OpenErrorModal("Fehler bei der Bauernumwandlung: Zustand inkonsistent.");
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
                await _gameService.SendMoveAsync(_gameCoreState.GameId, moveWithPromotion);
            }
            catch (Exception ex)
            {
                _modalState.OpenErrorModal($"Fehler beim Senden des Umwandlungszugs: {ex.Message}");
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
            if (_gameCoreState.CurrentPlayerInfo == null || _gameCoreState.GameId == Guid.Empty) return;

            await _cardState.LoadCapturedPiecesForRebirthAsync(_gameCoreState.GameId, _gameCoreState.CurrentPlayerInfo.Id, _gameService);
            var capturedPieceTypes = _cardState.CapturedPiecesForRebirth?.Select(p => p.Type).Distinct().ToList() ?? new List<PieceType>();

            if (capturedPieceTypes.Count > 0)
            {
                List<PieceSelectionChoiceInfo> choiceInfos = new();
                foreach (var pieceType in capturedPieceTypes)
                {
                    List<string> originalSquares = PieceHelperClient.GetOriginalStartSquares(pieceType, _gameCoreState.MyColor);
                    bool canBeRevived = false;
                    if (_gameCoreState.BoardDto?.Squares != null)
                    {
                        foreach (string squareString in originalSquares)
                        {
                            (int row, int col) = PositionHelper.ToIndices(squareString);
                            if (row >= 0 && row < 8 && col >= 0 && col < 8 && _gameCoreState.BoardDto.Squares[row][col] == null)
                            {
                                canBeRevived = true;
                                break;
                            }
                        }
                    }
                    string tooltip = canBeRevived ? $"{pieceType} kann wiederbelebt werden." : $"Alle Startfelder für {pieceType} sind besetzt.";
                    choiceInfos.Add(new PieceSelectionChoiceInfo(pieceType, canBeRevived, tooltip));
                }

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

            try
            {
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
                    bool turnAlreadyProcessedBySignalR = result.Success && result.EndsPlayerTurn && _gameCoreState.CurrentTurnPlayer == _gameCoreState.MyColor;

                    // KORREKTUR: Der Fehlertext kommt jetzt direkt vom `result`, wenn die Aktivierung serverseitig fehlschlägt.
                    string message = result.Success ? $"Karte '{activatedCardDefinition.Name}' erfolgreich aktiviert!" : result.ErrorMessage ?? "Kartenaktivierung fehlgeschlagen.";
                    _cardState.ResetCardActivationState(!result.Success, message);

                    if (turnAlreadyProcessedBySignalR)
                    {
                        _cardState.SetAwaitingTurnConfirmation(false);
                    }
                    else
                    {
                        _cardState.SetAwaitingTurnConfirmation(result.Success && result.EndsPlayerTurn);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                string friendlyMessage = ParseErrorMessageFromJson(ex.Message);
                _logger.LogClientSignalRConnectionWarning($"Fehler bei Kartenaktivierung vom Server: {friendlyMessage}");
                _modalState.OpenErrorModal(friendlyMessage);
                _cardState.ResetCardActivationState(true, friendlyMessage);
            }
            catch (Exception ex)
            {
                _logger.LogClientSignalRConnectionWarning($"Unerwarteter Fehler bei Kartenaktivierung: {ex.Message}");
                var errorMsg = "Ein unerwarteter Fehler ist aufgetreten.";
                _modalState.OpenErrorModal(errorMsg);
                _cardState.ResetCardActivationState(true, errorMsg);
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