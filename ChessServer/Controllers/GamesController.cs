// File: [SolutionDir]\ChessServer\Controllers\GamesController.cs
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using ChessNetwork.DTOs;
using ChessServer.Services;
using ChessLogic;
using Microsoft.AspNetCore.SignalR;
using ChessServer.Hubs;
using System.Text.Json;
using System.Threading.Tasks;
using ChessNetwork.Configuration;
using System.Globalization;
using System.Linq;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Chess.Logging;

namespace ChessServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GamesController : ControllerBase
    {
        private readonly IGameManager _mgr;
        private readonly IChessLogger _logger;
        private readonly IHubContext<ChessHub> _hubContext;
        public GamesController(IGameManager mgr, IChessLogger logger, IHubContext<ChessHub> hubContext)
        {
            _mgr = mgr;
            _logger = logger;
            _hubContext = hubContext;
        }

        [HttpPost]
        [ProducesResponseType(typeof(CreateGameResultDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<CreateGameResultDto> Create([FromBody] CreateGameDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var (gameId, playerId) = _mgr.CreateGame(
                    dto.PlayerName,
                    dto.Color,
                    dto.InitialMinutes,
                    dto.OpponentType,
                    dto.ComputerDifficulty
                );
                BoardDto boardDto = _mgr.GetState(gameId);
                Player assignedColor = _mgr.GetPlayerColor(gameId, playerId);

                var result = new CreateGameResultDto
                {
                    GameId = gameId,
                    PlayerId = playerId,
                    Color = assignedColor,
                    Board = boardDto
                };
                if (dto.OpponentType == "Computer")
                {
                    _logger.LogPvCGameCreated(gameId, dto.PlayerName, dto.Color, dto.ComputerDifficulty);
                }
                else
                {
                    _logger.LogGameCreated(gameId, dto.PlayerName, dto.InitialMinutes);
                }
                return CreatedAtAction(nameof(GetInfo), new { gameId = result.GameId }, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogGameHistoryGenericError(Guid.Empty, ex);
                return StatusCode(StatusCodes.Status500InternalServerError, "Ein interner Fehler ist beim Erstellen des Spiels aufgetreten.");
            }
        }

        [HttpPost("{gameId}/join")]
        [ProducesResponseType(typeof(JoinGameResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<JoinGameResultDto> Join(Guid gameId, [FromBody] JoinDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var (playerId, assignedColor) = _mgr.JoinGame(gameId, dto.PlayerName);
                BoardDto boardDto = _mgr.GetState(gameId);

                var result = new JoinGameResultDto
                {
                    PlayerId = playerId,
                    Name = dto.PlayerName,
                    Color = assignedColor,
                    Board = boardDto
                };
                _logger.LogPlayerJoinedGame(dto.PlayerName, gameId);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogGameNotFoundOnJoinAttempt(gameId);
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogInvalidOperationOnJoinGame(gameId, ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogGameHistoryGenericError(gameId, ex);
                return StatusCode(StatusCodes.Status500InternalServerError, "Ein interner Fehler ist beim Beitreten zum Spiel aufgetreten.");
            }
        }

        [HttpPost("{gameId}/move")]
        [ProducesResponseType(typeof(MoveResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MoveResultDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(MoveResultDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(MoveResultDto), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MoveResultDto>> Move(Guid gameId, [FromBody] MoveDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new MoveResultDto { IsValid = false, ErrorMessage = "Die Anfrage ist ungültig.", NewBoard = new BoardDto(System.Array.Empty<PieceDto?[]>()), Status = GameStatusDto.None });
            }
            try
            {
                MoveResultDto moveResult = _mgr.ApplyMove(gameId, dto, dto.PlayerId);
                if (!moveResult.IsValid)
                {
                    return BadRequest(moveResult);
                }

                _logger.LogApplyMoveInfo(gameId, dto.PlayerId, moveResult.IsValid);
                Player nextPlayerTurn = _mgr.GetCurrentTurnPlayer(gameId);
                Guid? nextPlayerId = _mgr.GetPlayerIdByColor(gameId, nextPlayerTurn);
                GameStatusDto statusForNextPlayer;
                if (nextPlayerId.HasValue)
                {
                    statusForNextPlayer = _mgr.GetGameStatus(gameId, nextPlayerId.Value);
                }
                else
                {
                    statusForNextPlayer = GameStatusDto.None;
                    _logger.LogControllerCouldNotDeterminePlayerIdForStatus(gameId, nextPlayerTurn);
                }

                string? cardEffectSquareCountLog = moveResult.CardEffectSquares != null ? moveResult.CardEffectSquares.Count.ToString(CultureInfo.InvariantCulture) : "0";
                _logger.LogSignalRUpdateInfo(gameId, nextPlayerTurn, statusForNextPlayer, moveResult.LastMoveFrom, moveResult.LastMoveTo, cardEffectSquareCountLog);
                await _hubContext.Clients.Group(gameId.ToString()).SendAsync("OnTurnChanged",
                    moveResult.NewBoard,
                    nextPlayerTurn,
                    statusForNextPlayer,
                    moveResult.LastMoveFrom,
                    moveResult.LastMoveTo,
                    moveResult.CardEffectSquares);
                _logger.LogOnTurnChangedSentToHub(gameId);
                var timeUpdate = _mgr.GetTimeUpdate(gameId);
                await _hubContext.Clients.Group(gameId.ToString()).SendAsync("OnTimeUpdate", timeUpdate);
                _logger.LogOnTimeUpdateSentAfterMove(gameId);
                if (moveResult.PlayerIdToSignalCardDraw.HasValue && moveResult.NewlyDrawnCard != null)
                {
                    Guid playerIdDrew = moveResult.PlayerIdToSignalCardDraw.Value;
                    string? targetConnectionId = GetConnectionIdForPlayerViaHubMap(playerIdDrew);

                    if (!string.IsNullOrEmpty(targetConnectionId))
                    {
                        int drawPileCount = _mgr.GetDrawPileCount(gameId, playerIdDrew);
                        await _hubContext.Clients.Client(targetConnectionId)
                                                 .SendAsync("CardAddedToHand", moveResult.NewlyDrawnCard, drawPileCount);
                        _logger.LogControllerMoveSentCardToHand(moveResult.NewlyDrawnCard.Name, targetConnectionId, playerIdDrew);
                    }
                    else if (moveResult.NewlyDrawnCard.Name.Contains(CardConstants.NoMoreCardsName))
                    {
                        _logger.LogControllerConnectionIdNotFoundNoMoreCards("Move", playerIdDrew);
                    }
                    else
                    {
                        await _hubContext.Clients.Group(gameId.ToString())
                                       .SendAsync("OnPlayerEarnedCardDraw", playerIdDrew);
                        _logger.LogControllerConnectionIdNotFoundGeneric("Move", playerIdDrew);
                    }
                }

                return Ok(moveResult);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogGameNotFoundOnMove(gameId, dto.PlayerId);
                BoardDto? currentBoard = null; try { currentBoard = _mgr.GetState(gameId); } catch { /* ignored */ }
                return NotFound(new MoveResultDto { IsValid = false, ErrorMessage = ex.Message, NewBoard = currentBoard ?? new BoardDto(System.Array.Empty<PieceDto?[]>()), Status = GameStatusDto.None });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogInvalidOperationOnMove(gameId, dto.PlayerId);
                BoardDto? currentBoard = null; try { currentBoard = _mgr.GetState(gameId); } catch { /* ignored */ }
                return BadRequest(new MoveResultDto { IsValid = false, ErrorMessage = ex.Message, NewBoard = currentBoard ?? new BoardDto(System.Array.Empty<PieceDto?[]>()), Status = GameStatusDto.None });
            }
            catch (Exception ex)
            {
                _logger.LogMoveProcessingError(gameId, dto.From, dto.To, ex);
                BoardDto? currentBoard = null; try { currentBoard = _mgr.GetState(gameId); } catch { /* ignored */ }
                return StatusCode(StatusCodes.Status500InternalServerError, new MoveResultDto { IsValid = false, ErrorMessage = "Ein interner Fehler ist beim Verarbeiten des Zugs aufgetreten.", NewBoard = currentBoard ?? new BoardDto(System.Array.Empty<PieceDto?[]>()), Status = GameStatusDto.None });
            }
        }

        [HttpPost("{gameId}/player/{playerId}/activatecard")]
        [ProducesResponseType(typeof(ServerCardActivationResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServerCardActivationResultDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ServerCardActivationResultDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ServerCardActivationResultDto), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ServerCardActivationResultDto>> ActivateCard(Guid gameId, Guid playerId, [FromBody] ActivateCardRequestDto dto)
        {
            _logger.LogCardActivationAttempt(gameId, playerId, dto.CardTypeId);
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServerCardActivationResultDto { Success = false, ErrorMessage = "Die Anfrage ist ungültig.", CardId = dto.CardTypeId });
            }

            try
            {
                Player playerDataColor;
                try
                {
                    playerDataColor = _mgr.GetPlayerColor(gameId, playerId);
                }
                catch (KeyNotFoundException)
                {
                    _logger.LogCardActivationFailedController(gameId, playerId, dto.CardTypeId, "Spieler oder Spiel nicht gefunden, um Farbe für Animation zu bestimmen.");
                    return NotFound(new ServerCardActivationResultDto { Success = false, ErrorMessage = $"Spiel oder Spieler für Kartenaktivierung nicht gefunden.", CardId = dto.CardTypeId });
                }
                catch (InvalidOperationException)
                {
                    _logger.LogCardActivationFailedController(gameId, playerId, dto.CardTypeId, "Spielerfarbe konnte nicht ermittelt werden.");
                    return StatusCode(StatusCodes.Status500InternalServerError, new ServerCardActivationResultDto { Success = false, ErrorMessage = "Interner Fehler: Spielerfarbe konnte nicht ermittelt werden.", CardId = dto.CardTypeId });
                }


                CardDto? cardForAnimation = null;
                if (_mgr is InMemoryGameManager concreteMgr)
                {
                    var sessionForCardDef = concreteMgr.GetSessionForDirectHubSend(gameId);
                    if (sessionForCardDef != null)
                    {
                        cardForAnimation = sessionForCardDef.GetCardDefinitionForAnimation(dto.CardTypeId);
                    }
                }

                if (cardForAnimation == null)
                {
                    _logger.LogGameNotFoundOnMove(gameId, playerId);
                }

                await _hubContext.Clients.Group(gameId.ToString()).SendAsync("PlayCardActivationAnimation", cardForAnimation ?? new CardDto { Id = dto.CardTypeId, Name = dto.CardTypeId, Description = "Lade...", ImageUrl = CardConstants.DefaultCardBackImageUrl, InstanceId = Guid.Empty }, playerId, playerDataColor);
                ServerCardActivationResultDto activationResultFull = await _mgr.ActivateCardEffect(gameId, playerId, dto);
                if (!activationResultFull.Success)
                {
                    _logger.LogCardActivationFailedController(gameId, playerId, dto.CardTypeId, activationResultFull.ErrorMessage ?? "Unbekannter Kartenaktivierungsfehler");
                    if (dto.CardTypeId == CardConstants.CardSwap &&
                        activationResultFull.ErrorMessage != null &&
                        (activationResultFull.ErrorMessage.Contains("Gegner hat keine Handkarten") ||
                         activationResultFull.ErrorMessage.Contains("Keine eigene Karte zum Tauschen")))
                    {
                        // Logik für diesen speziellen Fehlschlag, falls nötig
                    }
                    return BadRequest(activationResultFull);
                }

                _logger.LogCardActivationSuccessController(gameId, playerId, dto.CardTypeId);
                if (dto.CardTypeId == CardConstants.CardSwap && activationResultFull.CardGivenByPlayerForSwap != null && activationResultFull.CardReceivedByPlayerForSwap != null)
                {
                    await SendCardSwapAnimationDetails(gameId, playerId, activationResultFull.CardGivenByPlayerForSwap, activationResultFull.CardReceivedByPlayerForSwap);
                    await SendHandUpdatesAfterCardSwap(gameId, playerId, _mgr.GetOpponentInfo(gameId, playerId));
                }

                // Der folgende Block wurde entfernt, da die OnTurnChanged-Benachrichtigung
                // nun von der GameSession nach der Kartenaktivierung gesendet wird.
                /*
                var currentBoardDto = _mgr.GetState(gameId);
                var currentPlayerAfterCard = _mgr.GetCurrentTurnPlayer(gameId);
                GameStatusDto statusForNextPlayerSignalR;
                Guid? idOfPlayerNowAtTurn = _mgr.GetPlayerIdByColor(gameId, currentPlayerAfterCard);
                if (idOfPlayerNowAtTurn.HasValue)
                {
                    statusForNextPlayerSignalR = _mgr.GetGameStatus(gameId, idOfPlayerNowAtTurn.Value);
                }
                else
                {
                    statusForNextPlayerSignalR = GameStatusDto.None;
                    _logger.LogControllerCouldNotDeterminePlayerIdForStatus(gameId, currentPlayerAfterCard);
                }

                string? cardEffectSquareCountLogCard = activationResultFull.AffectedSquaresByCard != null ? activationResultFull.AffectedSquaresByCard.Count.ToString(CultureInfo.InvariantCulture) : "0";
                _logger.LogSignalRUpdateInfo(gameId, currentPlayerAfterCard, statusForNextPlayerSignalR, null, null, cardEffectSquareCountLogCard);

                List<AffectedSquareInfo>? affectedSquaresForSignalR = activationResultFull.AffectedSquaresByCard;
                await _hubContext.Clients.Group(gameId.ToString()).SendAsync("OnTurnChanged", currentBoardDto, currentPlayerAfterCard, statusForNextPlayerSignalR, null, null, affectedSquaresForSignalR);
                _logger.LogOnTurnChangedSentToHub(gameId);
                */

                var timeUpdate = _mgr.GetTimeUpdate(gameId);
                await _hubContext.Clients.Group(gameId.ToString()).SendAsync("OnTimeUpdate", timeUpdate);
                _logger.LogOnTimeUpdateSentAfterMove(gameId); // Dieser Log könnte präzisiert werden, da es kein "Move" im klassischen Sinn war.
                                                              // Aber für den Moment belassen wir es so, da die Zeit aktualisiert wurde.
                if (activationResultFull.PlayerIdToSignalCardDraw.HasValue && activationResultFull.NewlyDrawnCard != null)
                {
                    Guid playerIdDrew = activationResultFull.PlayerIdToSignalCardDraw.Value;
                    string? targetConnectionId = GetConnectionIdForPlayerViaHubMap(playerIdDrew);
                    if (!string.IsNullOrEmpty(targetConnectionId))
                    {
                        int drawPileCount = _mgr.GetDrawPileCount(gameId, playerIdDrew);
                        await _hubContext.Clients.Client(targetConnectionId)
                                                 .SendAsync("CardAddedToHand", activationResultFull.NewlyDrawnCard, drawPileCount);
                        _logger.LogControllerActivateCardSentCardToHand(activationResultFull.NewlyDrawnCard.Name, targetConnectionId, playerIdDrew);
                    }
                    else if (activationResultFull.NewlyDrawnCard.Name.Contains(CardConstants.NoMoreCardsName))
                    {
                        _logger.LogControllerConnectionIdNotFoundNoMoreCards("ActivateCard", playerIdDrew);
                    }
                    else
                    {
                        await _hubContext.Clients.Group(gameId.ToString())
                                       .SendAsync("OnPlayerEarnedCardDraw", playerIdDrew);
                        _logger.LogControllerConnectionIdNotFoundGeneric("ActivateCard", playerIdDrew);
                    }
                }

                return Ok(activationResultFull);
            }
            catch (KeyNotFoundException)
            {
                _logger.LogCardActivationFailedController(gameId, playerId, dto.CardTypeId, "Spiel nicht gefunden");
                return NotFound(new ServerCardActivationResultDto { Success = false, ErrorMessage = $"Spiel mit ID {gameId} nicht gefunden.", CardId = dto.CardTypeId });
            }
            catch (InvalidOperationException ioEx)
            {
                _logger.LogCardActivationFailedController(gameId, playerId, dto.CardTypeId, $"Ungültige Operation: {ioEx.Message}");
                return BadRequest(new ServerCardActivationResultDto { Success = false, ErrorMessage = ioEx.Message, CardId = dto.CardTypeId });
            }
            catch (Exception)
            {
                _logger.LogCardActivationFailedController(gameId, playerId, dto.CardTypeId, "Allgemeiner Fehler");
                return StatusCode(StatusCodes.Status500InternalServerError, new ServerCardActivationResultDto { Success = false, ErrorMessage = "Interner Serverfehler bei der Kartenaktivierung.", CardId = dto.CardTypeId });
            }
        }


        private async Task SendCardSwapAnimationDetails(Guid gameId, Guid activatingPlayerId, CardDto cardGivenByActivatingPlayer, CardDto cardReceivedByActivatingPlayer)
        {
            string? playerConnectionId = GetConnectionIdForPlayerViaHubMap(activatingPlayerId);
            if (!string.IsNullOrEmpty(playerConnectionId))
            {
                await _hubContext.Clients.Client(playerConnectionId)
                    .SendAsync("ReceiveCardSwapAnimationDetails", new CardSwapAnimationDetailsDto(activatingPlayerId, cardGivenByActivatingPlayer, cardReceivedByActivatingPlayer));
            }

            OpponentInfoDto? opponentInfo = _mgr.GetOpponentInfo(gameId, activatingPlayerId);
            if (opponentInfo != null)
            {
                string? opponentConnectionId = GetConnectionIdForPlayerViaHubMap(opponentInfo.OpponentId);
                if (!string.IsNullOrEmpty(opponentConnectionId))
                {
                    await _hubContext.Clients.Client(opponentConnectionId)
                        .SendAsync("ReceiveCardSwapAnimationDetails", new CardSwapAnimationDetailsDto(opponentInfo.OpponentId, cardReceivedByActivatingPlayer, cardGivenByActivatingPlayer));
                }
            }
        }

        private async Task SendHandUpdatesAfterCardSwap(Guid gameId, Guid playerId, OpponentInfoDto? opponentInfo)
        {
            string? playerConnectionId = GetConnectionIdForPlayerViaHubMap(playerId);
            if (!string.IsNullOrEmpty(playerConnectionId))
            {
                var playerHand = _mgr.GetPlayerHand(gameId, playerId);
                int playerDrawPile = _mgr.GetDrawPileCount(gameId, playerId);
                await _hubContext.Clients.Client(playerConnectionId).SendAsync("UpdateHandContents", new InitialHandDto(playerHand, playerDrawPile));
                _logger.LogControllerMoveSentCardToHand("HandUpdate", playerConnectionId, playerId);
            }

            OpponentInfoDto? actualOpponentInfo = opponentInfo ?? _mgr.GetOpponentInfo(gameId, playerId);
            if (actualOpponentInfo != null)
            {
                string? opponentConnectionId = GetConnectionIdForPlayerViaHubMap(actualOpponentInfo.OpponentId);
                if (!string.IsNullOrEmpty(opponentConnectionId))
                {
                    var opponentHand = _mgr.GetPlayerHand(gameId, actualOpponentInfo.OpponentId);
                    int opponentDrawPile = _mgr.GetDrawPileCount(gameId, actualOpponentInfo.OpponentId);
                    await _hubContext.Clients.Client(opponentConnectionId).SendAsync("UpdateHandContents", new InitialHandDto(opponentHand, opponentDrawPile));
                    _logger.LogControllerMoveSentCardToHand("HandUpdateOpponent", opponentConnectionId, actualOpponentInfo.OpponentId);
                }
            }
        }
        private string? GetConnectionIdForPlayerViaHubMap(Guid playerId)
        {
            if (ChessHub.PlayerIdToConnectionMap.TryGetValue(playerId, out string? connId))
            {
                return connId;
            }
            _logger.LogControllerConnectionIdForPlayerNotFound(playerId);
            return null;
        }

        [HttpGet("{gameId}/player/{playerId}/capturedpieces")]
        [ProducesResponseType(typeof(IEnumerable<CapturedPieceTypeDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<CapturedPieceTypeDto>>> GetCapturedPieces(Guid gameId, Guid playerId)
        {
            _logger.LogGettingCapturedPieces(gameId, playerId);
            try
            {
                var capturedPieces = await _mgr.GetCapturedPieces(gameId, playerId);
                return Ok(capturedPieces);
            }
            catch (KeyNotFoundException)
            {
                _logger.LogGameNotFoundCapturedPieces(gameId, playerId);
                return NotFound($"Spiel mit ID {gameId} oder Spieler mit ID {playerId} nicht gefunden.");
            }
            catch (Exception ex)
            {
                _logger.LogErrorGettingCapturedPieces(gameId, playerId, ex);
                return StatusCode(StatusCodes.Status500InternalServerError, "Interner Serverfehler beim Abrufen der geschlagenen Figuren.");
            }
        }

        [HttpGet("{gameId}/player/{playerId}/opponentinfo")]
        [ProducesResponseType(typeof(OpponentInfoDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public ActionResult<OpponentInfoDto> GetOpponentInfo(Guid gameId, Guid playerId)
        {
            try
            {
                var opponentInfo = _mgr.GetOpponentInfo(gameId, playerId);
                if (opponentInfo == null)
                {
                    return NotFound($"Keine Gegnerinformationen für Spiel {gameId} und Spieler {playerId} gefunden.");
                }
                return Ok(opponentInfo);
            }
            catch (KeyNotFoundException)
            {
                _logger.LogControllerGameNotFound(gameId, nameof(GetOpponentInfo));
                return NotFound($"Spiel mit ID {gameId} nicht gefunden.");
            }
            catch (Exception ex)
            {
                _logger.LogControllerErrorGettingOpponentInfo(gameId, playerId, ex);
                return StatusCode(StatusCodes.Status500InternalServerError, "Interner Serverfehler beim Abrufen der Gegnerinformationen.");
            }
        }

        [HttpGet("{gameId}/info")]
        [ProducesResponseType(typeof(GameInfoDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public ActionResult<GameInfoDto> GetInfo(Guid gameId)
        {
            try
            {
                var info = _mgr.GetGameInfo(gameId);
                return Ok(info);
            }
            catch (KeyNotFoundException)
            {
                _logger.LogControllerGameNotFound(gameId, nameof(GetInfo));
                return NotFound($"Spiel mit ID {gameId} nicht gefunden.");
            }
        }

        [HttpGet("{gameId}/legalmoves")]
        [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public ActionResult<IEnumerable<string>> LegalMoves(Guid gameId, [FromQuery] string from, [FromQuery] Guid playerId)
        {
            try
            {
                var moves = _mgr.GetLegalMoves(gameId, playerId, from);
                return Ok(moves);
            }
            catch (KeyNotFoundException)
            {
                _logger.LogControllerGameNotFound(gameId, nameof(LegalMoves));
                return NotFound($"Spiel mit ID {gameId} nicht gefunden oder Spieler/Feld ungültig.");
            }
            catch (InvalidOperationException ioEx)
            {
                _logger.LogControllerErrorGettingLegalMoves(gameId, playerId, from, ioEx);
                return BadRequest(ioEx.Message);
            }
        }

        [HttpGet("{gameId}/status")]
        [ProducesResponseType(typeof(GameStatusDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public ActionResult<GameStatusDto> Status(Guid gameId, [FromQuery] Guid playerId)
        {
            try { return Ok(_mgr.GetGameStatus(gameId, playerId)); }
            catch (KeyNotFoundException)
            {
                _logger.LogControllerGameNotFound(gameId, nameof(Status));
                return NotFound($"Spiel mit ID {gameId} oder Spieler-ID {playerId} nicht gefunden.");
            }
        }

        [HttpGet("{gameId}/currentplayer")]
        [ProducesResponseType(typeof(Player), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public ActionResult<Player> GetCurrentPlayerRoute(Guid gameId)
        {
            try { return Ok(_mgr.GetCurrentTurnPlayer(gameId)); }
            catch (KeyNotFoundException)
            {
                _logger.LogControllerGameNotFound(gameId, nameof(GetCurrentPlayerRoute));
                return NotFound($"Spiel mit ID {gameId} nicht gefunden.");
            }
        }

        [HttpGet("{gameId}/time")]
        [ProducesResponseType(typeof(TimeUpdateDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public ActionResult<TimeUpdateDto> GetTime(Guid gameId)
        {
            try { return Ok(_mgr.GetTimeUpdate(gameId)); }
            catch (KeyNotFoundException)
            {
                _logger.LogGameNotFoundOnTimeRequest(gameId);
                return NotFound($"Spiel mit ID {gameId} nicht gefunden.");
            }
            catch (Exception ex)
            {
                _logger.LogErrorGettingTime(gameId, ex);
                return StatusCode(StatusCodes.Status500InternalServerError, "Interner Serverfehler beim Abrufen der Zeit.");
            }
        }

        [HttpGet("{gameId}/downloadhistory")]
        [ProducesResponseType(typeof(GameHistoryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public ActionResult GetGameHistory(Guid gameId)
        {
            try
            {
                var gameHistory = _mgr.GetGameHistory(gameId);
                if (gameHistory == null)
                {
                    _logger.LogGameHistoryNullFromManager(gameId);
                    return NotFound($"Spielverlauf für Spiel-ID {gameId} nicht gefunden.");
                }
                Response.Headers.Append("Content-Disposition", $"attachment; filename=\"chess_game_{gameId}.json\"");
                var serializerOptions = new JsonSerializerOptions { WriteIndented = true };
                return new JsonResult(gameHistory, serializerOptions);
            }
            catch (KeyNotFoundException)
            {
                _logger.LogGameHistoryKeyNotFound(gameId);
                return NotFound($"Spiel mit ID {gameId} nicht gefunden.");
            }
            catch (Exception ex)
            {
                _logger.LogGameHistoryGenericError(gameId, ex);
                return StatusCode(StatusCodes.Status500InternalServerError, "Interner Serverfehler beim Abrufen des Spielverlaufs.");
            }
        }
    }
}