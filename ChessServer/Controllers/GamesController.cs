using Chess.Logging;
using ChessLogic;
using ChessNetwork.Configuration;
using ChessNetwork.DTOs;
using ChessServer.Hubs;
using ChessServer.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;

namespace ChessServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GamesController : ControllerBase
    {
        private readonly IGameManager _mgr;                 // Dienst zur Verwaltung aller Spielsitzungen.
        private readonly IChessLogger _logger;              // Dienst für das Logging von Spielereignissen.
        private readonly IHubContext<ChessHub> _hubContext; // Kontext für die Echtzeit-Kommunikation via SignalR.

        public GamesController(IGameManager mgr, IChessLogger logger, IHubContext<ChessHub> hubContext)
        {
            _mgr = mgr;
            _logger = logger;
            _hubContext = hubContext;
        }

        #region Public API Endpoints

        // POST: api/games/{gameId}/player/{playerId}/activatecard
        // Aktiviert eine Spezialkarte für einen Spieler.
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
                // Zuerst die Karten-Aktivierung vollständig auf dem Server validieren und ausführen.
                ServerCardActivationResultDto activationResultFull = await _mgr.ActivateCardEffect(gameId, playerId, dto);

                // Wenn die Aktivierung nicht erfolgreich war, sofort eine Fehlermeldung zurückgeben.
                // Es wird keine Animation gesendet.
                if (!activationResultFull.Success)
                {
                    _logger.LogCardActivationFailedController(gameId, playerId, dto.CardTypeId, activationResultFull.ErrorMessage ?? "Unbekannter Kartenaktivierungsfehler");
                    return BadRequest(activationResultFull);
                }

                // ERST JETZT: Da die Aktivierung erfolgreich war, senden wir das Signal für die Animation.
                Player playerDataColor = _mgr.GetPlayerColor(gameId, playerId);
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
                    // Fallback, sollte nicht passieren bei erfolgreicher Aktivierung.
                    _logger.LogGameNotFoundOnMove(gameId, playerId);
                }

                await _hubContext.Clients.Group(gameId.ToString()).SendAsync("PlayCardActivationAnimation", cardForAnimation ?? new CardDto { Id = dto.CardTypeId, Name = dto.CardTypeId, Description = "Lade...", ImageUrl = CardConstants.DefaultCardBackImageUrl, InstanceId = Guid.Empty }, playerId, playerDataColor);

                // Restliche Logik nach erfolgreicher Aktivierung (war bereits vorhanden)
                _logger.LogCardActivationSuccessController(gameId, playerId, dto.CardTypeId);
                if (dto.CardTypeId == CardConstants.CardSwap && activationResultFull.CardGivenByPlayerForSwap != null && activationResultFull.CardReceivedByPlayerForSwap != null)
                {
                    await SendCardSwapAnimationDetails(gameId, playerId, activationResultFull.CardGivenByPlayerForSwap, activationResultFull.CardReceivedByPlayerForSwap);
                    await SendHandUpdatesAfterCardSwap(gameId, playerId, _mgr.GetOpponentInfo(gameId, playerId));
                }

                var timeUpdate = _mgr.GetTimeUpdate(gameId);
                await _hubContext.Clients.Group(gameId.ToString()).SendAsync("OnTimeUpdate", timeUpdate);
                _logger.LogOnTimeUpdateSentAfterMove(gameId);

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

        // POST: api/games
        // Erstellt ein neues Spiel.
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

        // GET: api/games/{gameId}/player/{playerId}/capturedpieces
        // Ruft die geschlagenen Figuren eines Spielers ab.
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

        // GET: api/games/{gameId}/currentplayer
        // Ruft den Spieler ab, der aktuell am Zug ist.
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

        // GET: api/games/{gameId}/downloadhistory
        // Stellt die Spielhistorie als JSON-Datei zum Download bereit.
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

        // GET: api/games/{gameId}/info
        // Ruft grundlegende Informationen zu einem Spiel ab.
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

        // GET: api/games/{gameId}/player/{playerId}/opponentinfo
        // Ruft Informationen über den Gegner ab.
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

        // GET: api/games/{gameId}/time
        // Ruft die aktuellen Bedenkzeiten des Spiels ab.
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

        // POST: api/games/{gameId}/join
        // Ermöglicht einem Spieler, einem bestehenden Spiel beizutreten.
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

        // GET: api/games/{gameId}/legalmoves
        // Ruft alle legalen Züge für eine Figur an einer bestimmten Position ab.
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

        // POST: api/games/{gameId}/move
        // Verarbeitet einen vom Client gesendeten Zug.
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
                BoardDto? currentBoard = null; try { currentBoard = _mgr.GetState(gameId); } catch { }
                return NotFound(new MoveResultDto { IsValid = false, ErrorMessage = ex.Message, NewBoard = currentBoard ?? new BoardDto(System.Array.Empty<PieceDto?[]>()), Status = GameStatusDto.None });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogInvalidOperationOnMove(gameId, dto.PlayerId);
                BoardDto? currentBoard = null; try { currentBoard = _mgr.GetState(gameId); } catch { }
                return BadRequest(new MoveResultDto { IsValid = false, ErrorMessage = ex.Message, NewBoard = currentBoard ?? new BoardDto(System.Array.Empty<PieceDto?[]>()), Status = GameStatusDto.None });
            }
            catch (Exception ex)
            {
                _logger.LogMoveProcessingError(gameId, dto.From, dto.To, ex);
                BoardDto? currentBoard = null; try { currentBoard = _mgr.GetState(gameId); } catch { }
                return StatusCode(StatusCodes.Status500InternalServerError, new MoveResultDto { IsValid = false, ErrorMessage = "Ein interner Fehler ist beim Verarbeiten des Zugs aufgetreten.", NewBoard = currentBoard ?? new BoardDto(System.Array.Empty<PieceDto?[]>()), Status = GameStatusDto.None });
            }
        }

        // GET: api/games/{gameId}/status
        // Ruft den Spielstatus für einen bestimmten Spieler ab.
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

        #endregion

        #region Private Helper Methods

        // Findet die SignalR-Verbindungs-ID für eine gegebene Spieler-ID.
        private string? GetConnectionIdForPlayerViaHubMap(Guid playerId)
        {
            if (ChessHub.PlayerIdToConnectionMap.TryGetValue(playerId, out string? connId))
            {
                return connId;
            }
            _logger.LogControllerConnectionIdForPlayerNotFound(playerId);
            return null;
        }

        // Sendet die Animationsdetails für einen Kartentausch an beide beteiligten Spieler.
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

        // Sendet nach einem Kartentausch die aktualisierten Handkarten an beide Spieler.
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

        #endregion
    }
}