// File: [SolutionDir]\ChessServer\Services\InMemoryGameManager.cs
using Chess.Logging;
using ChessLogic;
using ChessNetwork.DTOs;
using ChessServer.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

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
            var playerManager = new PlayerManager(gameId, opponentType, computerDifficulty, new ChessLogger<PlayerManager>(_loggerFactory.CreateLogger<PlayerManager>()));
            var session = new GameSession(gameId, playerManager, initialMinutes, _hubContext, new ChessLogger<GameSession>(_loggerFactory.CreateLogger<GameSession>()), _loggerFactory, _httpClientFactory);

            var (firstPlayerId, _) = session.Join(playerName, color);
            _games[gameId] = session;

            _logger.LogMgrGameCreated(gameId, playerName, firstPlayerId, color, initialMinutes);
            return (gameId, firstPlayerId);
        }

        public (Guid PlayerId, Player Color) JoinGame(Guid gameId, string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName))
                throw new ArgumentException("PlayerName darf nicht leer sein.", nameof(playerName));
            var session = GetSession(gameId);
            var (newPlayerId, playerColor) = session.Join(playerName, null);
            return (newPlayerId, playerColor);
        }

        public void StartGame(Guid gameId)
        {
            GetSession(gameId).StartTheGameAndTimer();
        }

        public MoveResultDto ApplyMove(Guid gameId, MoveDto move, Guid playerId)
        {
            var session = GetSession(gameId);
            MoveResultDto moveResult = session.MakeMove(move, playerId);
            if (moveResult.IsValid && session.IsGameReallyOver())
            {
                _logger.LogMgrGameOverTimerStop(gameId);
            }
            return moveResult;
        }

        public async Task<ServerCardActivationResultDto> ActivateCardEffect(Guid gameId, Guid playerId, ActivateCardRequestDto cardActivationRequestDto)
        {
            var session = GetSession(gameId);
            return await session.ActivateCard(playerId, cardActivationRequestDto);
        }

        public BoardDto GetState(Guid gameId) => GetSession(gameId).ToBoardDto();
        public IEnumerable<string> GetLegalMoves(Guid gameId, Guid playerId, string from) => GetSession(gameId).GetLegalMoves(from);
        public GameInfoDto GetGameInfo(Guid gameId)
        {
            var session = GetSession(gameId);
            return new GameInfoDto(session.FirstPlayerId, session.FirstPlayerColor, session.HasOpponent);
        }
        public GameHistoryDto GetGameHistory(Guid gameId) => GetSession(gameId).GetGameHistory();
        public TimeUpdateDto GetTimeUpdate(Guid gameId) => GetSession(gameId).TimerService.GetCurrentTimeUpdateDto();
        public GameStatusDto GetGameStatus(Guid gameId, Guid playerId) => GetSession(gameId).GetStatus(playerId);
        public Player GetCurrentTurnPlayer(Guid gameId) => GetSession(gameId).CurrentGameState.CurrentPlayer;
        public Guid? GetPlayerIdByColor(Guid gameId, Player color) => GetSession(gameId).GetPlayerIdByColor(color);
        public Player GetPlayerColor(Guid gameId, Guid playerId) => GetSession(gameId).GetPlayerColor(playerId);
        public string? GetPlayerName(Guid gameId, Guid playerId) => GetSession(gameId).GetPlayerName(playerId);
        public OpponentInfoDto? GetOpponentInfo(Guid gameId, Guid currentPlayerId) => GetSession(gameId).GetOpponentDetails(currentPlayerId);

        public async Task<IEnumerable<CapturedPieceTypeDto>> GetCapturedPieces(Guid gameId, Guid playerId)
        {
            var session = GetSession(gameId);
            return await Task.FromResult(session.CardManager.GetCapturedPieceTypesOfPlayer(session.GetPlayerColor(playerId)));
        }

        public List<CardDto> GetPlayerHand(Guid gameId, Guid playerId) => GetSession(gameId).CardManager.GetPlayerHand(playerId);
        public int GetDrawPileCount(Guid gameId, Guid playerId) => GetSession(gameId).CardManager.GetDrawPileCount(playerId);

        public void RegisterPlayerHubConnection(Guid gameId, Guid playerId, string connectionId)
        {
            if (_games.ContainsKey(gameId)) _logger.LogMgrPlayerHubConnectionRegistered(playerId, gameId, connectionId);
            else _logger.LogMgrGameNotFoundForRegisterPlayerHub(gameId);
        }

        public void UnregisterPlayerHubConnection(Guid gameId, string connectionId)
        {
            if (_games.ContainsKey(gameId)) _logger.LogMgrPlayerHubConnectionUnregistered(connectionId, gameId);
            else _logger.LogMgrGameNotFoundForRegisterPlayerHub(gameId);
        }

        public GameSession? GetSessionForDirectHubSend(Guid gameId)
        {
            _games.TryGetValue(gameId, out var session);
            return session;
        }

        private GameSession GetSession(Guid gameId)
        {
            if (!_games.TryGetValue(gameId, out var session))
                throw new KeyNotFoundException($"Spiel mit ID {gameId} nicht gefunden.");
            return session;
        }

        public GameStatusDto GetGameStatusForOpponentOf(Guid gameId, Guid lastPlayerId)
        {
            var session = GetSession(gameId);
            return session.GetStatusForOpponentOf(lastPlayerId);
        }
    }
}