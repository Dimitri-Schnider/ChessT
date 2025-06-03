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

namespace ChessServer.Services
{
    public partial class InMemoryGameManager : IGameManager
    {
        private readonly ConcurrentDictionary<Guid, GameSession> _games = new();
        private readonly IHubContext<ChessHub> _hubContext;
        private readonly IChessLogger _logger;
        private readonly ILoggerFactory _loggerFactory;


        public InMemoryGameManager(IHubContext<ChessHub> hubContext, IChessLogger logger, ILoggerFactory loggerFactory)
        {
            _hubContext = hubContext;
            _logger = logger;
            _loggerFactory = loggerFactory;
        }

        public (Guid GameId, Guid PlayerId) CreateGame(string playerName, Player color, int initialMinutes)
        {
            if (string.IsNullOrWhiteSpace(playerName))
                throw new ArgumentException("PlayerName darf nicht leer sein.", nameof(playerName));
            var gameId = Guid.NewGuid();

            // Korrekte Logger-Erstellung für GameSession
            var msLoggerForGameSession = _loggerFactory.CreateLogger<GameSession>(); // Standard MS Logger für die Kategorie GameSession
            var chessLoggerForGameSession = new Chess.Logging.ChessLogger<GameSession>(msLoggerForGameSession); // Unser IChessLogger Wrapper

            var session = new GameSession(gameId, color, playerName, initialMinutes, _hubContext, chessLoggerForGameSession, _loggerFactory);

            (Guid firstPlayerId, Player firstPlayerColor) = session.Join(playerName, color);

            _games[gameId] = session;
            _logger.LogMgrGameCreated(gameId, playerName, firstPlayerId, firstPlayerColor, initialMinutes);
            return (gameId, firstPlayerId);
        }

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

        public (Guid PlayerId, Player Color) JoinGame(Guid gameId, string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName))
                throw new ArgumentException("PlayerName darf nicht leer sein.", nameof(playerName));
            if (!_games.TryGetValue(gameId, out var session))
                throw new KeyNotFoundException($"Spiel mit ID {gameId} nicht gefunden.");
            Guid newPlayerId;
            Player playerColor;

            lock (session)
            {
                (newPlayerId, playerColor) = session.Join(playerName);
            }

            bool startGameTimerLogicNeeded = false;
            lock (session)
            {
                if (session.PlayerCount == 2 && !session.IsGameReallyOver())
                {
                    startGameTimerLogicNeeded = true;
                }
            }

            if (startGameTimerLogicNeeded)
            {
                _logger.LogMgrPlayerJoinedGameTimerStart(playerName, gameId, session.GetCurrentTurnPlayerLogic());
                session.StartGameTimer();
            }

            return (newPlayerId, playerColor);
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

        public MoveResultDto ApplyMove(Guid gameId, MoveDto move, Guid playerId)
        {
            if (!_games.TryGetValue(gameId, out var session))
                throw new KeyNotFoundException($"Spiel mit ID {gameId} nicht gefunden.");
            MoveResultDto moveResult;
            lock (session)
            {
                moveResult = session.MakeMove(move, playerId);
                if (moveResult.IsValid && session.IsGameReallyOver())
                {
                    _logger.LogMgrGameOverTimerStop(gameId);
                    session.NotifyTimerGameOver();
                }
            }
            return moveResult;
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
    }
}