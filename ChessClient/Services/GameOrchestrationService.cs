// File: [SolutionDir]/ChessClient/Services/GameOrchestrationService.cs
using System;
using System.Globalization;
using System.Threading.Tasks;
using ChessClient.State;
using ChessLogic;
using ChessNetwork;
using ChessNetwork.DTOs;
using ChessClient.Utils;
using ChessClient.Models;
using ChessNetwork.Configuration;

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

        public GameOrchestrationService(
            IGameSession gameService,
            IGameCoreState gameCoreState,
            IUiState uiState,
            IModalState modalState,
            IHighlightState highlightState,
            ICardState cardState)
        {
            _gameService = gameService;
            _gameCoreState = gameCoreState;
            _uiState = uiState;
            _modalState = modalState;
            _highlightState = highlightState;
            _cardState = cardState;
        }

        private bool IsPawnPromotion(string fromAlgebraic, string toAlgebraic)
        {
            if (_gameCoreState.BoardDto == null) return false;
            (int fromRow, int fromCol) = PositionHelper.ToIndices(fromAlgebraic);
            (int toRow, _) = PositionHelper.ToIndices(toAlgebraic);

            PieceDto? piece = _gameCoreState.BoardDto.Squares[fromRow][fromCol];
            if (piece == null) return false;
            bool isPawn = piece.Value.ToString().Contains("Pawn", StringComparison.OrdinalIgnoreCase);
            if (!isPawn) return false;
            if (_gameCoreState.MyColor == Player.White && fromRow == 1 && toRow == 0) return true;
            if (_gameCoreState.MyColor == Player.Black && fromRow == 6 && toRow == 7) return true;

            return false;
        }

        public async Task<PlayerMoveProcessingResult> ProcessPlayerMoveAsync(MoveDto clientMove)
        {
            _uiState.ClearErrorMessage();
            if (IsPawnPromotion(clientMove.From, clientMove.To))
            {
                return new PlayerMoveProcessingResult(PlayerMoveOutcome.PawnPromotionPending, clientMove);
            }

            try
            {
                if (_gameCoreState.CurrentPlayerInfo == null || _gameCoreState.GameId == Guid.Empty)
                {
                    return new PlayerMoveProcessingResult(PlayerMoveOutcome.Error, ErrorMessage: "Spieler oder Spiel-ID nicht initialisiert.");
                }

                var moveToSend = new MoveDto(clientMove.From, clientMove.To, _gameCoreState.CurrentPlayerInfo.Id, clientMove.PromotionTo);
                var serverResult = await _gameService.SendMoveAsync(_gameCoreState.GameId, moveToSend);

                if (!serverResult.IsValid)
                {
                    _uiState.SetErrorMessage(serverResult.ErrorMessage ?? "Unbekannter Fehler beim Zug vom Server.");
                    _highlightState.ClearAllActionHighlights();
                    return new PlayerMoveProcessingResult(PlayerMoveOutcome.InvalidMove, ErrorMessage: serverResult.ErrorMessage);
                }
                else
                {
                    _uiState.ClearErrorMessage();
                    return new PlayerMoveProcessingResult(PlayerMoveOutcome.Success);
                }
            }
            catch (Exception ex)
            {
                string errorMsg = string.Format(CultureInfo.CurrentCulture, "Fehler beim Senden des Zugs: {0}", ex.Message);
                _uiState.SetErrorMessage(errorMsg);
                _highlightState.ClearAllActionHighlights();
                return new PlayerMoveProcessingResult(PlayerMoveOutcome.Error, ErrorMessage: errorMsg);
            }
        }

        public async Task<CardActivationFinalizationResult> FinalizeCardActivationAsync(ActivateCardRequestDto requestDto, CardDto activatedCard)
        {
            _cardState.SetIsCardActivationPending(true);
            await _uiState.SetCurrentInfoMessageForBoxAsync(string.Format(CultureInfo.CurrentCulture, "Aktiviere Karte '{0}'...", activatedCard.Name));
            _uiState.ClearErrorMessage();

            try
            {
                if (_gameCoreState.CurrentPlayerInfo == null)
                {
                    string noPlayerDataError = "Spielerdaten nicht verfügbar für Kartenaktivierung.";
                    _uiState.SetErrorMessage(noPlayerDataError);
                    _cardState.SetIsCardActivationPending(false);
                    // Hier auch die neuen Felder initialisieren
                    return new CardActivationFinalizationResult(CardActivationOutcome.Error, noPlayerDataError, true, null);
                }

                ServerCardActivationResultDto serverResult = await _gameService.ActivateCardAsync(_gameCoreState.GameId, _gameCoreState.CurrentPlayerInfo.Id, requestDto);
                if (!serverResult.Success)
                {
                    string errorMsg = serverResult.ErrorMessage ?? $"Fehler bei Kartenaktivierung '{activatedCard.Name}'.";
                    _uiState.SetErrorMessage(errorMsg);
                    await _uiState.SetCurrentInfoMessageForBoxAsync(string.Format(CultureInfo.CurrentCulture, "Aktivierung von '{0}' fehlgeschlagen.", activatedCard.Name));
                    _cardState.SetIsCardActivationPending(false);
                    // Hier auch die neuen Felder initialisieren
                    return new CardActivationFinalizationResult(CardActivationOutcome.Error, errorMsg, serverResult.EndsPlayerTurn, serverResult.PawnPromotionPendingAt);
                }

                _cardState.SetIsCardActivationPending(false);
                // Mappe die neuen Felder vom serverResult auf CardActivationFinalizationResult
                return new CardActivationFinalizationResult(
                    CardActivationOutcome.Success,
                    null, // Kein ErrorMessage bei Erfolg
                    serverResult.EndsPlayerTurn,
                    serverResult.PawnPromotionPendingAt
                );
            }
            catch (HttpRequestException httpEx)
            {
                _uiState.SetErrorMessage(httpEx.Message);
                await _uiState.SetCurrentInfoMessageForBoxAsync(string.Format(CultureInfo.CurrentCulture, "Aktivierung von '{0}' fehlgeschlagen.", activatedCard.Name));
                _cardState.SetIsCardActivationPending(false);
                return new CardActivationFinalizationResult(CardActivationOutcome.Error, httpEx.Message, true, null);
            }
            catch (Exception ex)
            {
                string errorMsg = string.Format(CultureInfo.CurrentCulture, "Fehler bei Kartenaktivierung '{0}': {1}", activatedCard.Name, ex.Message);
                _uiState.SetErrorMessage(errorMsg);
                await _uiState.SetCurrentInfoMessageForBoxAsync(string.Format(CultureInfo.CurrentCulture, "Aktivierung von '{0}' fehlgeschlagen.", activatedCard.Name));
                _cardState.SetIsCardActivationPending(false);
                return new CardActivationFinalizationResult(CardActivationOutcome.Error, errorMsg, true, null);
            }
        }

        public async Task<(bool Success, Guid GameId)> CreateNewGameAsync(string name, Player color, int timeMinutes)
        {
            _gameCoreState.SetGameSpecificDataInitialized(false);
            if (string.IsNullOrWhiteSpace(name))
            {
                _uiState.SetErrorMessage("Bitte gib einen Spielernamen ein.");
                return (false, Guid.Empty);
            }
            _uiState.ClearErrorMessage();
            _uiState.ClearCurrentInfoMessageForBox();
            _highlightState.ClearAllActionHighlights();
            try
            {
                var result = await _gameService.CreateGameAsync(name, color, timeMinutes);
                _gameCoreState.InitializeNewGame(result, name, result.Color, timeMinutes);
                _modalState.UpdateCreateGameModalArgs(name, color, timeMinutes);

                return (true, _gameCoreState.GameId);
            }
            catch (Exception ex)
            {
                _uiState.SetErrorMessage(string.Format(CultureInfo.CurrentCulture, "Fehler beim Erstellen des Spiels: {0}", ex.Message));
                return (false, Guid.Empty);
            }
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

                return (true, _gameCoreState.GameId);
            }
            catch (Exception ex)
            {
                _uiState.SetErrorMessage(string.Format(CultureInfo.CurrentCulture, "Fehler beim Beitreten zum Spiel: {0}", ex.Message));
                _gameCoreState.SetOpponentJoined(false);
                return (false, Guid.Empty);
            }
        }
    }
}