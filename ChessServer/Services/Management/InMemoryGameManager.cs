using Chess.Logging;
using ChessLogic;
using ChessNetwork.DTOs;
using ChessServer.Hubs;
using ChessServer.Services.ComputerPlayer;
using ChessServer.Services.Connectivity;
using ChessServer.Services.Session;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace ChessServer.Services.Management
{
    // Implementiert die IGameManager-Schnittstelle und verwaltet alle Spielsitzungen im Arbeitsspeicher.
    // Diese Klasse agiert als zentraler Einstiegspunkt für den `GamesController`, um auf einzelne Spiele zuzugreifen.
    public class InMemoryGameManager : IGameManager
    {
        // Ein thread-sicheres Dictionary, das Spiel-IDs auf die entsprechenden GameSession-Objekte abbildet.
        private readonly ConcurrentDictionary<Guid, GameSession> _games = new();
        private readonly IHubContext<ChessHub> _hubContext;
        private readonly IChessLogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IComputerMoveProvider _computerMoveProvider;
        private readonly IConnectionMappingService _connectionMappingService;
        private readonly IMoveExecutionService _moveExecutionService;

        // Konstruktor: Initialisiert die Manager-Klasse mit den erforderlichen Diensten.
        public InMemoryGameManager(IHubContext<ChessHub> hubContext, IChessLogger logger, 
            ILoggerFactory loggerFactory, IComputerMoveProvider computerMoveProvider, 
            IConnectionMappingService connectionMappingService, IMoveExecutionService moveExecutionService)
        {
            _hubContext = hubContext;
            _logger = logger;
            _loggerFactory = loggerFactory;
            _computerMoveProvider = computerMoveProvider;
            _connectionMappingService = connectionMappingService;
            _moveExecutionService = moveExecutionService;
        }

        // Erstellt ein neues Spiel, initialisiert eine neue GameSession und fügt sie dem Dictionary hinzu.
        public (Guid GameId, Guid PlayerId) CreateGame(string playerName, Player color, int initialMinutes, OpponentType opponentType = OpponentType.Human, ComputerDifficulty computerDifficulty = ComputerDifficulty.Medium)
        {
            if (string.IsNullOrWhiteSpace(playerName))
                throw new ArgumentException("PlayerName darf nicht leer sein.", nameof(playerName));
            var gameId = Guid.NewGuid();
            var playerManager = new PlayerManager(gameId, opponentType, computerDifficulty, new ChessLogger<PlayerManager>(_loggerFactory.CreateLogger<PlayerManager>()));
            var session = new GameSession(gameId, playerManager, initialMinutes, _hubContext, new ChessLogger<GameSession>(_loggerFactory.CreateLogger<GameSession>()), _loggerFactory, _computerMoveProvider, _connectionMappingService, _moveExecutionService);

            var (firstPlayerId, _) = session.Join(playerName, color);
            _games[gameId] = session;

            _logger.LogMgrGameCreated(gameId, playerName, firstPlayerId, color, initialMinutes);

            // Startet ein Spiel gegen den Computer sofort.
            if (opponentType == OpponentType.Computer)
            {
                StartGame(gameId);
            }

            return (gameId, firstPlayerId);
        }

        // Ermöglicht einem Spieler, einem bestehenden Spiel beizutreten, indem der Aufruf an die entsprechende GameSession delegiert wird.
        public (Guid PlayerId, Player Color) JoinGame(Guid gameId, string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName))
                throw new ArgumentException("PlayerName darf nicht leer sein.", nameof(playerName));
            var session = GetSession(gameId);
            var (newPlayerId, playerColor) = session.Join(playerName, null);
            return (newPlayerId, playerColor);
        }

        // Startet ein Spiel.
        public void StartGame(Guid gameId)
        {
            GetSession(gameId).StartTheGameAndTimer();
        }

        // Wendet einen Zug an, indem der Aufruf an die GameSession delegiert wird.
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

        // Aktiviert einen Karteneffekt.
        public async Task<ServerCardActivationResultDto> ActivateCardEffect(Guid gameId, Guid playerId, ActivateCardRequestDto cardActivationRequestDto)
        {
            var session = GetSession(gameId);
            return await session.ActivateCard(playerId, cardActivationRequestDto);
        }

        // Die folgenden Methoden sind einfache Wrapper, die die Anfragen an die richtige GameSession weiterleiten.
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

        // Registriert die SignalR-Verbindung eines Spielers (nur für Logging-Zwecke hier).
        public void RegisterPlayerHubConnection(Guid gameId, Guid playerId, string connectionId)
        {
            if (_games.ContainsKey(gameId)) _logger.LogMgrPlayerHubConnectionRegistered(playerId, gameId, connectionId);
            else _logger.LogMgrGameNotFoundForRegisterPlayerHub(gameId);
        }

        // Deregistriert die SignalR-Verbindung eines Spielers.
        public void UnregisterPlayerHubConnection(Guid gameId, string connectionId)
        {
            if (_games.ContainsKey(gameId)) _logger.LogMgrPlayerHubConnectionUnregistered(connectionId, gameId);
            else _logger.LogMgrGameNotFoundForRegisterPlayerHub(gameId);
        }

        // Gibt eine GameSession-Instanz direkt zurück, z.B. für den ChessHub.
        public GameSession? GetSessionForDirectHubSend(Guid gameId)
        {
            _games.TryGetValue(gameId, out var session);
            return session;
        }

        // Private Hilfsmethode, um eine GameSession sicher aus dem Dictionary abzurufen.
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