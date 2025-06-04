// File: [SolutionDir]\ChessServer\Services\InMemoryGameManager.cs
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChessLogic;
using ChessNetwork.DTOs;
using Microsoft.AspNetCore.SignalR;
using ChessServer.Hubs;
using Chess.Logging;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace ChessServer.Services
{
    public partial class InMemoryGameManager : IGameManager
    {
        private readonly ConcurrentDictionary<Guid, GameSession> _games = new();
        private readonly IHubContext<ChessHub> _hubContext;
        private readonly IChessLogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IHttpClientFactory _httpClientFactory;

        public InMemoryGameManager(IHubContext<ChessHub> hubContext, IChessLogger logger, ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory)
        {
            _hubContext = hubContext;
            _logger = logger;
            _loggerFactory = loggerFactory;
            _httpClientFactory = httpClientFactory;
        }

        public (Guid GameId, Guid PlayerId) CreateGame(string playerName, Player color, int initialMinutes, string opponentType = "Human", string computerDifficulty = "Medium")
        {
            if (string.IsNullOrWhiteSpace(playerName))
                throw new ArgumentException("PlayerName darf nicht leer sein.", nameof(playerName));
            var gameId = Guid.NewGuid();

            var msLoggerForGameSession = _loggerFactory.CreateLogger<GameSession>();
            var chessLoggerForGameSession = new Chess.Logging.ChessLogger<GameSession>(msLoggerForGameSession);

            var session = new GameSession(
                gameId,
                color,
                playerName, // creatorName wird hier übergeben, auch wenn Join es nochmal nimmt
                initialMinutes,
                _hubContext,
                chessLoggerForGameSession,
                _loggerFactory,
                _httpClientFactory,
                opponentType,
                computerDifficulty
            );
            // Der menschliche Ersteller tritt der Session bei.
            // Die `Join`-Methode der GameSession fügt den Ersteller hinzu
            // und initialisiert ggf. den Computergegner.
            var (firstPlayerId, _) = session.Join(playerName, color); // Das `color` hier ist die Präferenz des Erstellers.

            _games[gameId] = session;

            // Log wird im GamesController gemacht.
            return (gameId, firstPlayerId);
        }

        public (Guid PlayerId, Player Color) JoinGame(Guid gameId, string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName))
                throw new ArgumentException("PlayerName darf nicht leer sein.", nameof(playerName));
            if (!_games.TryGetValue(gameId, out var session))
                throw new KeyNotFoundException($"Spiel mit ID {gameId} nicht gefunden.");

            // Der zweite Spieler (Mensch) tritt bei.
            // Das preferredColor Argument ist null, da der zweite Spieler die andere Farbe bekommt.
            var (newPlayerId, playerColor) = session.Join(playerName, null);

            return (newPlayerId, playerColor);
        }

        public MoveResultDto ApplyMove(Guid gameId, MoveDto move, Guid playerId)
        {
            if (!_games.TryGetValue(gameId, out var session))
                throw new KeyNotFoundException($"Spiel mit ID {gameId} nicht gefunden.");

            // KORREKTUR: session.MakeMove ist eine Methode der GameSession-Instanz
            MoveResultDto moveResult = session.MakeMove(move, playerId);

            if (moveResult.IsValid && session.IsGameReallyOver())
            {
                _logger.LogMgrGameOverTimerStop(gameId);
            }
            return moveResult;
        }

        // ... (Rest der Klasse InMemoryGameManager, GetSessionForDirectHubSend nur einmal definieren) ...
        public List<CardDto> GetPlayerHand(Guid gameId, Guid playerId)
        {
            if (_games.TryGetValue(gameId, out var session))
            {
                lock (session)
                {
                    return session.GetPlayerHand(playerId);
                }
            }
            _logger.LogMgrGameNotFoundForCapturedPieces(gameId, playerId);
            return new List<CardDto>();
        }
        public GameHistoryDto GetGameHistory(Guid gameId)
        {
            if (!_games.TryGetValue(gameId, out var session))
                throw new KeyNotFoundException($"Spiel mit ID {gameId} nicht gefunden.");
            lock (session) { return session.GetGameHistory(); }
        }

        public BoardDto GetState(Guid gameId)
        {
            if (!_games.TryGetValue(gameId, out var session))
                throw new KeyNotFoundException($"Spiel mit ID {gameId} nicht gefunden.");
            lock (session) { return session.ToBoardDto(); }
        }


        public TimeUpdateDto GetTimeUpdate(Guid gameId)
        {
            if (!_games.TryGetValue(gameId, out var session))
                throw new KeyNotFoundException($"Spiel mit ID {gameId} nicht gefunden.");
            lock (session)
            {
                return session.TimerService.GetCurrentTimeUpdateDto();
            }
        }

        public GameInfoDto GetGameInfo(Guid gameId)
        {
            if (!_games.TryGetValue(gameId, out var session))
                throw new KeyNotFoundException($"Spiel mit ID {gameId} nicht gefunden.");
            lock (session)
            {
                return new GameInfoDto(
                    CreatorId: session.FirstPlayerId,
                    CreatorColor: session.FirstPlayerColor,
                    HasOpponent: session.HasOpponent
                );
            }
        }

        public IEnumerable<string> GetLegalMoves(Guid gameId, Guid playerId, string from)
        {
            if (!_games.TryGetValue(gameId, out var session))
                throw new KeyNotFoundException($"Spiel mit ID {gameId} nicht gefunden.");
            lock (session) { return session.GetLegalMoves(from); }
        }

        public GameStatusDto GetGameStatus(Guid gameId, Guid playerId)
        {
            if (!_games.TryGetValue(gameId, out var session))
                throw new KeyNotFoundException($"Spiel mit ID {gameId} nicht gefunden.");
            lock (session) { return session.GetStatus(playerId); }
        }

        public Player GetCurrentTurnPlayer(Guid gameId)
        {
            if (!_games.TryGetValue(gameId, out var session))
                throw new KeyNotFoundException($"Spiel mit ID {gameId} nicht gefunden.");
            lock (session) { return session.GetCurrentTurnPlayerLogic(); }
        }

        public GameStatusDto GetGameStatusForOpponentOf(Guid gameId, Guid lastPlayerId)
        {
            if (!_games.TryGetValue(gameId, out var session))
                throw new KeyNotFoundException($"Spiel mit ID {gameId} nicht gefunden.");
            lock (session) { return session.GetStatusForOpponentOf(lastPlayerId); }
        }

        public Guid? GetPlayerIdByColor(Guid gameId, Player color)
        {
            if (_games.TryGetValue(gameId, out var session))
            {
                lock (session)
                {
                    return session.GetPlayerIdByColor(color);
                }
            }
            _logger.LogMgrGameNotFoundForGetPlayerIdByColor(gameId);
            return null;
        }

        public Player GetPlayerColor(Guid gameId, Guid playerId)
        {
            if (!_games.TryGetValue(gameId, out var session))
                throw new KeyNotFoundException($"Spiel mit ID {gameId} nicht gefunden.");
            lock (session) { return session.GetPlayerColor(playerId); }
        }

        public string? GetPlayerName(Guid gameId, Guid playerId)
        {
            if (!_games.TryGetValue(gameId, out var session))
                return null;
            lock (session) { return session.GetPlayerName(playerId); }
        }


        public OpponentInfoDto? GetOpponentInfo(Guid gameId, Guid currentPlayerId)
        {
            if (_games.TryGetValue(gameId, out var session))
            {
                lock (session)
                {
                    return session.GetOpponentDetails(currentPlayerId);
                }
            }
            _logger.LogMgrGameNotFoundForGetOpponentInfo(gameId, currentPlayerId);
            return null;
        }

        public async Task<ServerCardActivationResultDto> ActivateCardEffect(Guid gameId, Guid playerId, ActivateCardRequestDto cardActivationRequestDto)
        {
            if (!_games.TryGetValue(gameId, out var session))
            {
                _logger.LogMgrGameNotFoundForCardActivation(gameId, playerId, cardActivationRequestDto.CardTypeId);
                return new ServerCardActivationResultDto { Success = false, ErrorMessage = $"Spiel mit ID {gameId} nicht gefunden.", CardId = cardActivationRequestDto.CardTypeId };
            }
            return await session.ActivateCard(playerId, cardActivationRequestDto);
        }

        public Task<IEnumerable<CapturedPieceTypeDto>> GetCapturedPieces(Guid gameId, Guid playerId)
        {
            if (!_games.TryGetValue(gameId, out var session))
            {
                _logger.LogMgrGameNotFoundForCapturedPieces(gameId, playerId);
                throw new KeyNotFoundException($"Spiel mit ID {gameId} nicht gefunden.");
            }
            Player playerColor;
            lock (session)
            {
                playerColor = session.GetPlayerColor(playerId);
            }
            return Task.FromResult(session.GetCapturedPieceTypesOfPlayer(playerColor));
        }

        public int GetDrawPileCount(Guid gameId, Guid playerId)
        {
            if (!_games.TryGetValue(gameId, out var session))
                throw new KeyNotFoundException($"Spiel mit ID {gameId} nicht gefunden.");
            lock (session) { return session.GetDrawPileCount(playerId); }
        }

        public void RegisterPlayerHubConnection(Guid gameId, Guid playerId, string connectionId)
        {
            if (_games.TryGetValue(gameId, out var session))
            {
                lock (session)
                {
                    _logger.LogMgrPlayerHubConnectionRegistered(playerId, gameId, connectionId);
                }
            }
            else
            {
                _logger.LogMgrGameNotFoundForRegisterPlayerHub(gameId);
            }
        }

        public void UnregisterPlayerHubConnection(Guid gameId, string connectionId)
        {
            if (_games.TryGetValue(gameId, out var session))
            {
                lock (session)
                {
                    _logger.LogMgrPlayerHubConnectionUnregistered(connectionId, gameId);
                }
            }
            else
            {
                _logger.LogMgrGameNotFoundForRegisterPlayerHub(gameId);
            }
        }
        // Stelle sicher, dass diese Methode nur EINMAL definiert ist.
        public GameSession? GetSessionForDirectHubSend(Guid gameId)
        {
            _games.TryGetValue(gameId, out var session);
            return session;
        }
    }
}