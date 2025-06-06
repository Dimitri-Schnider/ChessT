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
    // Implementiert die Spielverwaltungslogik und hält alle laufenden Spielsitzungen im Arbeitsspeicher.
    public partial class InMemoryGameManager : IGameManager
    {
        #region Private Fields

        private readonly ConcurrentDictionary<Guid, GameSession> _games = new(); // Thread-sicheres Dictionary zur Speicherung der Spielsitzungen.
        private readonly IHubContext<ChessHub> _hubContext;                       // SignalR-Hub-Kontext für Echtzeit-Kommunikation.
        private readonly IChessLogger _logger;                                    // Dienst für strukturiertes Logging von Spielereignissen.
        private readonly ILoggerFactory _loggerFactory;                           // Factory zur Erstellung von Loggern für untergeordnete Dienste.
        private readonly IHttpClientFactory _httpClientFactory;                   // Factory zur Erstellung von HttpClient-Instanzen für API-Aufrufe.

        #endregion

        #region Constructor

        public InMemoryGameManager(IHubContext<ChessHub> hubContext, IChessLogger logger, ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory)
        {
            _hubContext = hubContext;
            _logger = logger;
            _loggerFactory = loggerFactory;
            _httpClientFactory = httpClientFactory;
        }

        #endregion

        #region Game Creation & Joining

        // Erstellt ein neues Spiel und eine zugehörige Spielsitzung.
        public (Guid GameId, Guid PlayerId) CreateGame(string playerName, Player color, int initialMinutes, string opponentType = "Human", string computerDifficulty = "Medium")
        {
            if (string.IsNullOrWhiteSpace(playerName))
                throw new ArgumentException("PlayerName darf nicht leer sein.", nameof(playerName));

            var gameId = Guid.NewGuid();
            var msLoggerForGameSession = _loggerFactory.CreateLogger<GameSession>();
            var chessLoggerForGameSession = new ChessLogger<GameSession>(msLoggerForGameSession);

            var session = new GameSession(
                gameId,
                color,
                playerName,
                initialMinutes,
                _hubContext,
                chessLoggerForGameSession,
                _loggerFactory,
                _httpClientFactory,
                opponentType,
                computerDifficulty
            );

            // Der menschliche Ersteller tritt der Session bei.
            // Die `Join`-Methode der GameSession fügt den Ersteller hinzu und initialisiert ggf. den Computergegner.
            var (firstPlayerId, _) = session.Join(playerName, color);

            _games[gameId] = session;

            return (gameId, firstPlayerId);
        }

        // Ermöglicht einem Spieler, einem bestehenden Spiel beizutreten.
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

        #endregion

        #region Game Actions

        // Wendet einen Zug auf eine bestimmte Spielsitzung an.
        public MoveResultDto ApplyMove(Guid gameId, MoveDto move, Guid playerId)
        {
            if (!_games.TryGetValue(gameId, out var session))
                throw new KeyNotFoundException($"Spiel mit ID {gameId} nicht gefunden.");

            MoveResultDto moveResult = session.MakeMove(move, playerId);

            if (moveResult.IsValid && session.IsGameReallyOver())
            {
                _logger.LogMgrGameOverTimerStop(gameId);
            }
            return moveResult;
        }

        // Aktiviert den Effekt einer ausgewählten Karte.
        public async Task<ServerCardActivationResultDto> ActivateCardEffect(Guid gameId, Guid playerId, ActivateCardRequestDto cardActivationRequestDto)
        {
            if (!_games.TryGetValue(gameId, out var session))
            {
                _logger.LogMgrGameNotFoundForCardActivation(gameId, playerId, cardActivationRequestDto.CardTypeId);
                return new ServerCardActivationResultDto { Success = false, ErrorMessage = $"Spiel mit ID {gameId} nicht gefunden.", CardId = cardActivationRequestDto.CardTypeId };
            }
            return await session.ActivateCard(playerId, cardActivationRequestDto);
        }

        #endregion

        #region State & Info Getters

        // Ruft den aktuellen Brettzustand (Figurenpositionen) ab.
        public BoardDto GetState(Guid gameId)
        {
            if (!_games.TryGetValue(gameId, out var session))
                throw new KeyNotFoundException($"Spiel mit ID {gameId} nicht gefunden.");
            lock (session) { return session.ToBoardDto(); }
        }

        // Ruft alle legalen Züge für eine Figur auf einem bestimmten Feld ab.
        public IEnumerable<string> GetLegalMoves(Guid gameId, Guid playerId, string from)
        {
            if (!_games.TryGetValue(gameId, out var session))
                throw new KeyNotFoundException($"Spiel mit ID {gameId} nicht gefunden.");
            lock (session) { return session.GetLegalMoves(from); }
        }

        // Ruft grundlegende Informationen über ein Spiel ab.
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

        // Ruft den detaillierten Spielverlauf ab.
        public GameHistoryDto GetGameHistory(Guid gameId)
        {
            if (!_games.TryGetValue(gameId, out var session))
                throw new KeyNotFoundException($"Spiel mit ID {gameId} nicht gefunden.");
            lock (session) { return session.GetGameHistory(); }
        }

        // Ruft die aktuellen Bedenkzeiten beider Spieler ab.
        public TimeUpdateDto GetTimeUpdate(Guid gameId)
        {
            if (!_games.TryGetValue(gameId, out var session))
                throw new KeyNotFoundException($"Spiel mit ID {gameId} nicht gefunden.");
            lock (session)
            {
                return session.TimerService.GetCurrentTimeUpdateDto();
            }
        }

        // Ruft den Spielstatus (z.B. Schach, Matt) für einen bestimmten Spieler ab.
        public GameStatusDto GetGameStatus(Guid gameId, Guid playerId)
        {
            if (!_games.TryGetValue(gameId, out var session))
                throw new KeyNotFoundException($"Spiel mit ID {gameId} nicht gefunden.");
            lock (session) { return session.GetStatus(playerId); }
        }

        // Ruft den Spielstatus für den Gegner des zuletzt ziehenden Spielers ab.
        public GameStatusDto GetGameStatusForOpponentOf(Guid gameId, Guid lastPlayerId)
        {
            if (!_games.TryGetValue(gameId, out var session))
                throw new KeyNotFoundException($"Spiel mit ID {gameId} nicht gefunden.");
            lock (session) { return session.GetStatusForOpponentOf(lastPlayerId); }
        }

        #endregion

        #region Player & Card Getters

        // Ruft den Spieler ab, der aktuell am Zug ist.
        public Player GetCurrentTurnPlayer(Guid gameId)
        {
            if (!_games.TryGetValue(gameId, out var session))
                throw new KeyNotFoundException($"Spiel mit ID {gameId} nicht gefunden.");
            lock (session) { return session.GetCurrentTurnPlayerLogic(); }
        }

        // Ruft die ID eines Spielers anhand seiner Farbe ab.
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

        // Ruft die Farbe eines Spielers anhand seiner ID ab.
        public Player GetPlayerColor(Guid gameId, Guid playerId)
        {
            if (!_games.TryGetValue(gameId, out var session))
                throw new KeyNotFoundException($"Spiel mit ID {gameId} nicht gefunden.");
            lock (session) { return session.GetPlayerColor(playerId); }
        }

        // Ruft den Namen eines Spielers anhand seiner ID ab.
        public string? GetPlayerName(Guid gameId, Guid playerId)
        {
            if (!_games.TryGetValue(gameId, out var session))
                return null;
            lock (session) { return session.GetPlayerName(playerId); }
        }

        // Ruft Informationen über den Gegner des aktuellen Spielers ab.
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

        // Ruft alle geschlagenen Figuren für einen bestimmten Spieler ab.
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

        // Ruft die Handkarten eines Spielers ab.
        public List<CardDto> GetPlayerHand(Guid gameId, Guid playerId)
        {
            if (_games.TryGetValue(gameId, out var session))
            {
                lock (session)
                {
                    return session.GetPlayerHand(playerId);
                }
            }
            _logger.LogMgrGameNotFoundForCapturedPieces(gameId, playerId); // Wiederverwendeter Log, da der Kontext ähnlich ist (Spiel nicht gefunden)
            return new List<CardDto>();
        }

        // Ruft die Anzahl der Karten im Nachziehstapel eines Spielers ab.
        public int GetDrawPileCount(Guid gameId, Guid playerId)
        {
            if (!_games.TryGetValue(gameId, out var session))
                throw new KeyNotFoundException($"Spiel mit ID {gameId} nicht gefunden.");
            lock (session) { return session.GetDrawPileCount(playerId); }
        }

        #endregion

        #region Hub & Session Management

        // Registriert die SignalR-Verbindung eines Spielers mit einem Spiel.
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

        // Entfernt die Registrierung der SignalR-Verbindung eines Spielers.
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

        // Gibt eine direkte Referenz auf eine GameSession zurück (für interne Zwecke wie den Hub).
        public GameSession? GetSessionForDirectHubSend(Guid gameId)
        {
            _games.TryGetValue(gameId, out var session);
            return session;
        }

        #endregion
    }
}