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
    // Diese Klasse ist der "Dirigent" für alle spielbezogenen Aktionen auf dem Client.
    // Sie koordiniert die Interaktion zwischen der UI, den State-Containern und dem Server.
    public class GameOrchestrationService
    {
        // Alle benötigten Dienste und State-Container werden per Dependency Injection bereitgestellt.
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

        // Konstruktor zur Initialisierung der Dienste.
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

        // Sendet eine Anfrage zur Erstellung eines neuen Spiels an den Server.
        public async Task<CreateGameResultDto?> CreateGameOnServerAsync(CreateGameParameters args)
        {
            if (string.IsNullOrWhiteSpace(args.Name))
            {
                _modalState.OpenErrorModal("Bitte gib einen Spielernamen ein.");
                return null;
            }

            try
            {
                // Erstellt das Data Transfer Object für die Server-Anfrage.
                var createGameDto = new CreateGameDto
                {
                    PlayerName = args.Name,
                    Color = args.Color,
                    InitialMinutes = args.TimeMinutes,
                    OpponentType = args.OpponentType.ToString(),
                    ComputerDifficulty = args.ComputerDifficulty.ToString()
                };
                // Ruft den Server auf und initialisiert den lokalen Spielzustand mit dem Ergebnis.
                var result = await _gameService.CreateGameAsync(createGameDto);
                _gameCoreState.InitializeNewGame(result, args);
                _modalState.UpdateCreateGameModalArgs(args.Name, args.Color, args.TimeMinutes);
                return result;
            }
            catch (Exception ex)
            {
                _modalState.OpenErrorModal($"Fehler beim Erstellen des Spiels: {ex.Message}");
                return null;
            }
        }

        // Versucht, eine benutzerfreundliche Fehlermeldung aus einer JSON-Antwort zu extrahieren.
        private static string ParseErrorMessageFromJson(string jsonContent)
        {
            try
            {
                var moveResult = JsonSerializer.Deserialize<MoveResultDto>(jsonContent, _jsonOptions);
                if (!string.IsNullOrWhiteSpace(moveResult?.ErrorMessage)) return moveResult.ErrorMessage;
            }
            catch (JsonException) { /* Ignorieren, wenn das Parsen fehlschlägt */ }

            try
            {
                var cardResult = JsonSerializer.Deserialize<ServerCardActivationResultDto>(jsonContent, _jsonOptions);
                if (!string.IsNullOrWhiteSpace(cardResult?.ErrorMessage)) return cardResult.ErrorMessage;
            }
            catch (JsonException) { /* Ignorieren */ }

            // Gibt den rohen Inhalt zurück, wenn kein bekanntes DTO geparst werden konnte.
            return jsonContent;
        }

        // Verbindet den Client mit dem SignalR Hub und registriert den Spieler für das Spiel.
        public async Task ConnectAndRegisterPlayerToHubAsync(Guid gameId, Guid playerId)
        {
            if (!_hubService.IsConnected)
            {
                // Baut die Hub-URL aus der Konfiguration zusammen.
                string? serverBaseUrl = _configuration.GetValue<string>("ServerBaseUrl") ?? ClientConstants.DefaultServerBaseUrl;
                await _hubService.StartAsync($"{serverBaseUrl.TrimEnd('/')}{ClientConstants.ChessHubRelativePath}");
            }
            if (_hubService.IsConnected)
            {
                // Registriert die Verbindung des Spielers beim Hub.
                await _hubService.RegisterPlayerWithHubAsync(gameId, playerId);
            }
            else
            {
                _logger.LogClientSignalRConnectionWarning("SignalR konnte nicht verbunden werden.");
            }
        }

        // Verarbeitet einen Klick auf ein Feld, während eine Kartenaktion aktiv ist.
        public async Task HandleSquareClickForCardAsync(string algebraicCoord)
        {
            if (!_cardState.IsCardActivationPending || _cardState.ActiveCardForBoardSelection == null || _gameCoreState.CurrentPlayerInfo == null) return;

            var activeCard = _cardState.ActiveCardForBoardSelection;
            var request = new ActivateCardRequestDto { CardInstanceId = _cardState.SelectedCardInstanceIdInHand ?? Guid.Empty, CardTypeId = activeCard.Id };

            // Behandelt mehrstufige Karteneffekte wie Teleport oder Positionstausch.
            if (activeCard.Id is CardConstants.Teleport or CardConstants.Positionstausch)
            {
                if (string.IsNullOrEmpty(_cardState.FirstSquareSelectedForTeleportOrSwap))
                {
                    // Erster Klick: Figur auswählen.
                    _cardState.SetFirstSquareForTeleportOrSwap(algebraicCoord);
                    var msg = activeCard.Id == CardConstants.Teleport ? $"Teleport: Figur auf {algebraicCoord} ausgewählt. Wähle nun ein leeres Zielfeld."
                        : $"Positionstausch: Erste Figur auf {algebraicCoord} ausgewählt. Wähle nun deine zweite Figur.";
                    await SetCardActionInfoBoxMessage(msg, true);
                    _highlightState.SetHighlights(algebraicCoord, null, false);
                }
                else
                {
                    // Zweiter Klick: Ziel auswählen und Aktion finalisieren.
                    request.FromSquare = _cardState.FirstSquareSelectedForTeleportOrSwap;
                    request.ToSquare = algebraicCoord;
                    await FinalizeCardActivationOnServerAsync(request, activeCard);
                }
            }
            // Behandelt die Auswahl des Zielfeldes für die "Wiedergeburt"-Karte.
            else if (activeCard.Id is CardConstants.Wiedergeburt && _cardState.IsAwaitingRebirthTargetSquareSelection)
            {
                request.PieceTypeToRevive = _cardState.PieceTypeSelectedForRebirth;
                request.TargetRevivalSquare = algebraicCoord;
                await FinalizeCardActivationOnServerAsync(request, activeCard);
            }
            // Behandelt die Auswahl des Bauern für die "Opfergabe"-Karte.
            else if (activeCard.Id is CardConstants.SacrificeEffect && _cardState.IsAwaitingSacrificePawnSelection)
            {
                request.FromSquare = algebraicCoord;
                await FinalizeCardActivationOnServerAsync(request, activeCard);
            }
        }

        // Verarbeitet die Auswahl einer Figur aus dem Wiedergeburts- oder Bauernumwandlungsmodal.
        public async Task HandlePieceTypeSelectedFromModalAsync(PieceType selectedType)
        {
            if (_modalState.ShowPawnPromotionModalSpecifically)
            {
                // Fall: Bauernumwandlung
                await ProcessPawnPromotionAsync(selectedType);
            }
            else if (_cardState.IsCardActivationPending && _cardState.ActiveCardForBoardSelection?.Id == CardConstants.Wiedergeburt)
            {
                // Fall: Wiedergeburt
                _modalState.ClosePieceSelectionModal();
                _cardState.SetAwaitingRebirthTargetSquareSelection(selectedType);

                // Prüft, ob gültige Startfelder für die Wiederbelebung frei sind.
                List<string> originalSquares = PieceHelperClient.GetOriginalStartSquares(selectedType, _gameCoreState.MyColor);
                List<string> validTargetSquares = originalSquares.Where(s => {
                    (int r, int c) = PositionHelper.ToIndices(s);
                    return _gameCoreState.BoardDto?.Squares[r][c] == null;
                }).ToList();

                if (validTargetSquares.Count == 0)
                {
                    // Kein Feld frei: Karte verfällt.
                    await _uiState.SetCurrentInfoMessageForBoxAsync($"Keine freien Ursprungsfelder für {selectedType} verfügbar. Karte verfällt.", true, 4000);
                    var request = new ActivateCardRequestDto { CardInstanceId = _cardState.SelectedCardInstanceIdInHand ?? Guid.Empty, CardTypeId = CardConstants.Wiedergeburt, PieceTypeToRevive = selectedType };
                    if (_cardState.ActiveCardForBoardSelection != null)
                    {
                        await FinalizeCardActivationOnServerAsync(request, _cardState.ActiveCardForBoardSelection);
                    }
                }
                else if (validTargetSquares.Count == 1)
                {
                    // Nur ein Feld frei: Aktion automatisch ausführen.
                    await _uiState.SetCurrentInfoMessageForBoxAsync($"Wiederbelebe {selectedType} auf {validTargetSquares[0]}...", false);
                    var request = new ActivateCardRequestDto
                    {
                        CardInstanceId = _cardState.SelectedCardInstanceIdInHand ?? Guid.Empty,
                        CardTypeId = CardConstants.Wiedergeburt,
                        PieceTypeToRevive = selectedType,
                        TargetRevivalSquare = validTargetSquares[0]
                    };
                    if (_cardState.ActiveCardForBoardSelection != null)
                    {
                        await FinalizeCardActivationOnServerAsync(request, _cardState.ActiveCardForBoardSelection);
                    }
                }
                else
                {
                    // Mehrere Felder frei: Benutzer muss wählen.
                    _highlightState.SetCardTargetSquaresForSelection(validTargetSquares);
                    await SetCardActionInfoBoxMessage($"Wähle ein leeres Ursprungsfeld für {selectedType}.", true);
                }
            }
        }

        // Tritt einem existierenden Spiel bei.
        public async Task<(bool Success, Guid GameId)> JoinExistingGameAsync(string name, string gameIdToJoin)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                _modalState.OpenErrorModal("Bitte gib einen Spielernamen ein.");
                return (false, Guid.Empty);
            }
            if (!Guid.TryParse(gameIdToJoin, out var parsedGuid))
            {
                _modalState.OpenErrorModal("Ungültiges Game-ID Format.");
                return (false, Guid.Empty);
            }

            _modalState.UpdateJoinGameModalArgs(name, gameIdToJoin);
            try
            {
                // Ruft den Server auf, um dem Spiel beizutreten.
                var result = await _gameService.JoinGameAsync(parsedGuid, name);
                _gameCoreState.InitializeJoinedGame(result, parsedGuid, result.Color);

                // Holt sich die Informationen des Gegners.
                var opponentInfo = await _gameService.GetOpponentInfoAsync(parsedGuid, result.PlayerId);
                if (opponentInfo != null)
                {
                    _gameCoreState.SetPlayerName(opponentInfo.OpponentColor, opponentInfo.OpponentName);
                }

                _modalState.CloseJoinGameModal();
                // Registriert den Spieler beim Hub.
                await ConnectAndRegisterPlayerToHubAsync(parsedGuid, result.PlayerId);
                return (true, _gameCoreState.GameId);
            }
            catch (Exception ex)
            {
                _modalState.OpenErrorModal($"Fehler beim Beitreten zum Spiel: {ex.Message}");
                return (false, Guid.Empty);
            }
        }

        // Verarbeitet einen vom Spieler initiierten Zug (Optimistic UI Logik).
        public async Task ProcessPlayerMoveAsync(MoveDto clientMove)
        {
            if (_gameCoreState.IsAwaitingMoveConfirmation) return;

            // Bauernumwandlung ist eine Ausnahme, da sie ein Modal erfordert und nicht optimistisch sein kann.
            if (IsPawnPromotion(clientMove.From, clientMove.To))
            {
                _modalState.OpenPawnPromotionModal(clientMove, _gameCoreState.MyColor);
                await _uiState.SetCurrentInfoMessageForBoxAsync("Wähle eine Figur für die Umwandlung.");
                return;
            }

            // Führt den Zug sofort im Client-State aus, bevor der Server antwortet.
            _gameCoreState.ApplyOptimisticMove(clientMove);

            try
            {
                // Sendet den Zug zur finalen Validierung an den Server.
                var moveToSend = new MoveDto(clientMove.From, clientMove.To, _gameCoreState.CurrentPlayerInfo!.Id, clientMove.PromotionTo);
                await _gameService.SendMoveAsync(_gameCoreState.GameId, moveToSend);
                // Bei Erfolg passiert hier nichts. Wir warten auf das 'OnTurnChanged'-Event vom Hub.
            }
            catch (HttpRequestException ex)
            {
                // ROLLBACK: Der Server hat den Zug abgelehnt.
                string friendlyMessage = ParseErrorMessageFromJson(ex.Message);
                _modalState.OpenErrorModal(friendlyMessage);
                _highlightState.ClearAllActionHighlights();

                // Macht den optimistischen Zug im Client rückgängig.
                _gameCoreState.RevertOptimisticMove();
            }
            catch (Exception ex)
            {
                // Allgemeiner Fehler, ebenfalls Rollback.
                _modalState.OpenErrorModal($"Ein unerwarteter Fehler ist aufgetreten: {ex.Message}");
                _highlightState.ClearAllActionHighlights();
                _gameCoreState.RevertOptimisticMove();
            }
        }

        // Verarbeitet die Auswahl einer Figur im Bauernumwandlungs-Modal.
        public async Task ProcessPawnPromotionAsync(PieceType promotionType)
        {
            if (_modalState.PendingPromotionMove == null || _gameCoreState.CurrentPlayerInfo == null)
            {
                _modalState.OpenErrorModal("Fehler bei der Bauernumwandlung: Zustand inkonsistent.");
                _modalState.ClosePawnPromotionModal();
                return;
            }

            // Modal schliessen und den Zug mit der gewählten Figur an den Server senden.
            _modalState.ClosePawnPromotionModal();
            MoveDto pendingMove = _modalState.PendingPromotionMove;
            _modalState.ClearPendingPromotionMove();
            MoveDto moveWithPromotion = new(pendingMove.From, pendingMove.To, _gameCoreState.CurrentPlayerInfo.Id, promotionType);
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

        // Startet den Prozess der Kartenaktivierung.
        public async Task ActivateCardAsync(CardDto cardToActivate)
        {
            // Führt eine Vorab-Prüfung durch, ob die Karte überhaupt aktivierbar ist.
            if (!IsCardActivatable(cardToActivate))
            {
                await SetCardActionInfoBoxMessage($"Karte '{cardToActivate.Name}' kann momentan nicht aktiviert werden.", false);
                _cardState.SetIsCardActivationPending(false);
                return;
            }

            _cardState.StartCardActivation(cardToActivate);

            // Unterscheidet zwischen Karten, die weitere Benutzerinteraktion erfordern, und solchen, die sofort wirken.
            switch (cardToActivate.Id)
            {
                case CardConstants.Teleport:
                case CardConstants.Positionstausch:
                case CardConstants.SacrificeEffect:
                case CardConstants.Wiedergeburt:
                    // Diese Fälle erfordern weitere Klicks auf dem Brett und werden von anderen Handlern verarbeitet.
                    await (cardToActivate.Id switch
                    {
                        CardConstants.Teleport => SetCardActionInfoBoxMessage("Teleport: Wähle eine deiner Figuren...", true),
                        CardConstants.Positionstausch => SetCardActionInfoBoxMessage("Positionstausch: Wähle deine erste Figur...", true),
                        CardConstants.SacrificeEffect => HandleSacrificeCardActivationAsync(cardToActivate),
                        CardConstants.Wiedergeburt => HandleRebirthCardActivationAsync(cardToActivate),
                        _ => Task.CompletedTask
                    });
                    break;
                default:
                    // Karten, die sofort wirken (z.B. Zeitkarten).
                    ActivateCardRequestDto requestDto = new ActivateCardRequestDto
                    {
                        CardInstanceId = cardToActivate.InstanceId,
                        CardTypeId = cardToActivate.Id
                    };
                    if (cardToActivate.Id == CardConstants.CardSwap)
                    {
                        // Wählt eine andere Karte aus der Hand als Tauschobjekt.
                        requestDto.CardInstanceIdToSwapFromHand = _cardState.PlayerHandCards?.FirstOrDefault(c => c.InstanceId != cardToActivate.InstanceId)?.InstanceId;
                    }
                    await SetCardActionInfoBoxMessage($"Aktiviere Karte '{cardToActivate.Name}'...", false);
                    _cardState.SetAwaitingTurnConfirmation(cardToActivate.Id != CardConstants.ExtraZug);
                    _cardState.DeselectActiveHandCard();
                    await FinalizeCardActivationOnServerAsync(requestDto, cardToActivate);
                    break;
            }
        }

        // Bereitet die Aktivierung der "Opfergabe"-Karte vor.
        private async Task HandleSacrificeCardActivationAsync(CardDto card)
        {
            _cardState.SetAwaitingSacrificePawnSelection(true);
            var pawnSquares = new List<string>();
            if (_gameCoreState.BoardDto != null)
            {
                // Findet alle eigenen Bauern auf dem Brett.
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
                // Hebt alle opferbaren Bauern hervor.
                _highlightState.SetCardTargetSquaresForSelection(pawnSquares);
                await SetCardActionInfoBoxMessage("Opfergabe: Wähle einen deiner Bauern.", true);
            }
            else
            {
                // Keine Bauern vorhanden, Karte verfällt.
                await SetCardActionInfoBoxMessage("Keine Bauern zum Opfern vorhanden.", false);
                await FinalizeCardActivationOnServerAsync(new ActivateCardRequestDto { CardInstanceId = card.InstanceId, CardTypeId = card.Id }, card);
            }
        }

        // Bereitet die Aktivierung der "Wiedergeburt"-Karte vor.
        private async Task HandleRebirthCardActivationAsync(CardDto card)
        {
            if (_gameCoreState.CurrentPlayerInfo == null || _gameCoreState.GameId == Guid.Empty) return;

            // Lädt die Liste der geschlagenen Figuren vom Server.
            await _cardState.LoadCapturedPiecesForRebirthAsync(_gameCoreState.GameId, _gameCoreState.CurrentPlayerInfo.Id, _gameService);
            var capturedPieceTypes = _cardState.CapturedPiecesForRebirth?.Select(p => p.Type).Distinct().ToList() ?? new List<PieceType>();

            if (capturedPieceTypes.Count > 0)
            {
                // Baut die Liste der Auswahlmöglichkeiten für das Modal auf.
                List<PieceSelectionChoiceInfo> choiceInfos = new();
                foreach (var pieceType in capturedPieceTypes)
                {
                    // Prüft, ob für diesen Figurentyp ein Startfeld frei ist.
                    List<string> originalSquares = PieceHelperClient.GetOriginalStartSquares(pieceType, _gameCoreState.MyColor);
                    bool canBeRevived = originalSquares.Any(s => {
                        (int r, int c) = PositionHelper.ToIndices(s);
                        return _gameCoreState.BoardDto?.Squares[r][c] == null;
                    });
                    string tooltip = canBeRevived ? $"{pieceType} kann wiederbelebt werden." : $"Alle Startfelder für {pieceType} sind besetzt.";
                    choiceInfos.Add(new PieceSelectionChoiceInfo(pieceType, canBeRevived, tooltip));
                }
                // Öffnet das Auswahlmodal.
                _modalState.OpenPieceSelectionModal("Figur wiederbeleben", "Wähle eine Figur:", choiceInfos, _gameCoreState.MyColor);
            }
            else
            {
                // Keine wiederbelebungsfähigen Figuren geschlagen, Karte verfällt.
                await SetCardActionInfoBoxMessage("Keine wiederbelebungsfähigen Figuren geschlagen.", false);
                await FinalizeCardActivationOnServerAsync(new ActivateCardRequestDto { CardInstanceId = card.InstanceId, CardTypeId = card.Id }, card);
            }
        }

        // Finalisiert die Kartenaktivierung durch Senden der Anfrage an den Server.
        public async Task FinalizeCardActivationOnServerAsync(ActivateCardRequestDto requestDto, CardDto activatedCardDefinition)
        {
            if (_gameCoreState.CurrentPlayerInfo == null) return;
            try
            {
                var result = await _gameService.ActivateCardAsync(_gameCoreState.GameId, _gameCoreState.CurrentPlayerInfo.Id, requestDto);

                // Sonderfall: Wenn der Karteneffekt eine Bauernumwandlung auslöst.
                if (result.PawnPromotionPendingAt != null && result.Success)
                {
                    _cardState.SetAwaitingTurnConfirmation(false);
                    var pos = result.PawnPromotionPendingAt;
                    var move = new MoveDto(PositionHelper.ToAlgebraic(pos.Row, pos.Column), PositionHelper.ToAlgebraic(pos.Row, pos.Column), _gameCoreState.CurrentPlayerInfo.Id);
                    _modalState.OpenPawnPromotionModal(move, _gameCoreState.MyColor);
                    await _uiState.SetCurrentInfoMessageForBoxAsync("Bauer umwandeln!");
                    _cardState.SetIsCardActivationPending(true); // Hält den Karten-Modus aktiv.
                }
                else
                {
                    // Normalfall: Verarbeitet das Ergebnis der Kartenaktivierung.
                    bool turnAlreadyProcessedBySignalR = result.Success && result.EndsPlayerTurn && _gameCoreState.CurrentTurnPlayer == _gameCoreState.MyColor;
                    string message = result.Success ? $"Karte '{activatedCardDefinition.Name}' erfolgreich aktiviert!" : result.ErrorMessage ?? "Kartenaktivierung fehlgeschlagen.";
                    _cardState.ResetCardActivationState(!result.Success, message);

                    // Wartet auf die Bestätigung vom Server, dass der Zug vorbei ist.
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
                // Behandelt Server-Fehler.
                string friendlyMessage = ParseErrorMessageFromJson(ex.Message);
                _logger.LogClientSignalRConnectionWarning($"Fehler bei Kartenaktivierung vom Server: {friendlyMessage}");
                _modalState.OpenErrorModal(friendlyMessage, closeOtherModals: false);
                _cardState.ResetCardActivationState(true, friendlyMessage);
            }
            catch (Exception ex)
            {
                // Behandelt unerwartete Client-Fehler.
                _logger.LogClientSignalRConnectionWarning($"Unerwarteter Fehler bei Kartenaktivierung: {ex.Message}");
                var errorMsg = "Ein unerwarteter Fehler ist aufgetreten.";
                _modalState.OpenErrorModal(errorMsg, closeOtherModals: false);
                _cardState.ResetCardActivationState(true, errorMsg);
            }
        }

        // Prüft, ob ein Zug eine Bauernumwandlung darstellt.
        private bool IsPawnPromotion(string fromAlgebraic, string toAlgebraic)
        {
            if (_gameCoreState.BoardDto == null) return false;
            (int fromRow, _) = PositionHelper.ToIndices(fromAlgebraic);
            (int toRow, _) = PositionHelper.ToIndices(toAlgebraic);
            PieceDto? piece = _gameCoreState.BoardDto.Squares[fromRow][PositionHelper.ToIndices(fromAlgebraic).Column];

            if (piece == null || !piece.Value.ToString().Contains("Pawn", StringComparison.OrdinalIgnoreCase)) return false;

            // Umwandlung für Weiss: von Reihe 1 auf 0
            if (_gameCoreState.MyColor == Player.White && fromRow == 1 && toRow == 0) return true;
            // Umwandlung für Schwarz: von Reihe 6 auf 7
            if (_gameCoreState.MyColor == Player.Black && fromRow == 6 && toRow == 7) return true;

            return false;
        }

        // Führt eine clientseitige Vorab-Prüfung durch, ob eine Karte aktiviert werden kann.
        private bool IsCardActivatable(CardDto? card)
        {
            if (card == null) return false;
            if (_modalState.ShowPieceSelectionModal) return false;
            if (_cardState.IsCardActivationPending && _cardState.SelectedCardInstanceIdInHand != card.InstanceId) return false;
            if (_gameCoreState.CurrentPlayerInfo == null || !_gameCoreState.OpponentJoined || _gameCoreState.MyColor != _gameCoreState.CurrentTurnPlayer || !string.IsNullOrEmpty(_gameCoreState.EndGameMessage)) return false;

            // Sonderregel für "Zeitdiebstahl".
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
            // Sonderregel für "Kartentausch".
            if (card.Id == CardConstants.CardSwap)
            {
                if (_cardState.PlayerHandCards == null || _cardState.PlayerHandCards.Count < 2)
                {
                    return false;
                }
            }
            return true;
        }

        // Setzt eine Nachricht in der Info-Box, oft mit einer "Abbrechen"-Aktion.
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