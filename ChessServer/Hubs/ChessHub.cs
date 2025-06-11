using Chess.Logging;
using ChessLogic;
using ChessNetwork.DTOs;
using ChessServer.Services;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChessServer.Hubs
{
    // SignalR-Hub für Echtzeit-Kommunikation während des Schachspiels.
    public class ChessHub : Hub
    {
        #region Static Fields

        // Bildet eine Verbindungs-ID auf eine Spieler-ID ab.
        public static readonly ConcurrentDictionary<string, Guid> connectionToPlayerIdMap = new();
        // Bildet eine Spieler-ID auf eine Verbindungs-ID ab (für schnelles Nachschlagen).
        public static readonly ConcurrentDictionary<Guid, string> PlayerIdToConnectionMap = new();
        // Bildet eine Verbindungs-ID auf eine Spiel-ID ab.
        private static readonly ConcurrentDictionary<string, Guid> _connectionToGameIdMap = new();

        #endregion

        #region Instance Fields

        private readonly IGameManager _gameManager;
        private readonly IChessLogger _logger;

        #endregion

        public ChessHub(IGameManager gameManager, IChessLogger logger)
        {
            _gameManager = gameManager;
            _logger = logger;
        }

        #region Overridden Methods

        // Wird aufgerufen, wenn ein neuer Client eine Verbindung zum Hub herstellt.
        public override async Task OnConnectedAsync()
        {
            _logger.LogHubClientConnected(Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        // Wird aufgerufen, wenn ein Client die Verbindung zum Hub trennt.
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogHubClientDisconnected(Context.ConnectionId, exception?.Message, exception);
            if (connectionToPlayerIdMap.TryRemove(Context.ConnectionId, out Guid playerId))
            {
                PlayerIdToConnectionMap.TryRemove(playerId, out _);
                _logger.LogHubPlayerMappingRemovedOnDisconnect(playerId);
            }
            if (_connectionToGameIdMap.TryRemove(Context.ConnectionId, out Guid gameId))
            {
                _gameManager.UnregisterPlayerHubConnection(gameId, Context.ConnectionId);
                _logger.LogHubConnectionRemovedFromGameOnDisconnect(Context.ConnectionId, gameId);
            }
            await base.OnDisconnectedAsync(exception);
        }

        #endregion

        #region Hub Methods

        // Registriert eine Verbindung für einen bestimmten Spieler in einem Spiel.
        public async Task RegisterConnection(Guid gameId, Guid playerId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, gameId.ToString());
            _connectionToGameIdMap[Context.ConnectionId] = gameId;
            connectionToPlayerIdMap[Context.ConnectionId] = playerId;
            PlayerIdToConnectionMap[playerId] = Context.ConnectionId;

            _gameManager.RegisterPlayerHubConnection(gameId, playerId, Context.ConnectionId);
            _logger.LogHubPlayerRegisteredToHub(playerId, Context.ConnectionId, gameId);

            string? playerName = _gameManager.GetPlayerName(gameId, playerId);
            int playerCount = 0;
            try
            {
                var gameInfoForCount = _gameManager.GetGameInfo(gameId);
                if (_gameManager.GetPlayerIdByColor(gameId, Player.White).HasValue) playerCount++;
                if (_gameManager.GetPlayerIdByColor(gameId, Player.Black).HasValue) playerCount++;
            }
            catch (KeyNotFoundException)
            {
                _logger.LogHubGameNotFoundForPlayerCount(gameId, Context.ConnectionId);
                playerCount = 1;
            }

            await Clients.Group(gameId.ToString()).SendAsync("PlayerJoined", playerName ?? "Ein Spieler", playerCount);
            _logger.LogHubPlayerJoinedNotificationSent(playerName ?? "Unbekannt", gameId.ToString(), playerCount);

            try
            {
                if (_gameManager is InMemoryGameManager manager && manager.GetSessionForDirectHubSend(gameId) is GameSession session)
                {
                    List<CardDto> hand = session.CardManager.GetPlayerHand(playerId);
                    int drawPileCount = session.CardManager.GetDrawPileCount(playerId);
                    var initialHandDto = new InitialHandDto(hand, drawPileCount);

                    _logger.LogHubSendingInitialHand(playerId, Context.ConnectionId, gameId, initialHandDto.Hand.Count, initialHandDto.DrawPileCount);
                    await Clients.Client(Context.ConnectionId).SendAsync("ReceiveInitialHand", initialHandDto);
                    var gameInfo = _gameManager.GetGameInfo(gameId);
                    if (gameInfo.HasOpponent && playerCount == 2)
                    {
                        Guid opponentId = Guid.Empty;
                        var playerWhiteId = _gameManager.GetPlayerIdByColor(gameId, Player.White);
                        var playerBlackId = _gameManager.GetPlayerIdByColor(gameId, Player.Black);
                        if (playerWhiteId.HasValue && playerWhiteId.Value == playerId && playerBlackId.HasValue)
                        {
                            opponentId = playerBlackId.Value;
                        }
                        else if (playerBlackId.HasValue && playerBlackId.Value == playerId && playerWhiteId.HasValue)
                        {
                            opponentId = playerWhiteId.Value;
                        }

                        if (opponentId != Guid.Empty && PlayerIdToConnectionMap.TryGetValue(opponentId, out string? opponentConnectionId))
                        {
                            List<CardDto> opponentHand = session.CardManager.GetPlayerHand(opponentId);
                            int opponentDrawPileCount = session.CardManager.GetDrawPileCount(opponentId);
                            var opponentInitialHandDto = new InitialHandDto(opponentHand, opponentDrawPileCount);

                            _logger.LogHubSendingInitialHand(opponentId, opponentConnectionId, gameId, opponentInitialHandDto.Hand.Count, opponentInitialHandDto.DrawPileCount);
                            await Clients.Client(opponentConnectionId).SendAsync("ReceiveInitialHand", opponentInitialHandDto);
                        }

                        // Countdown starten, wenn der zweite Spieler beitritt
                        _logger.LogStartGameCountdown(gameId);
                        await Clients.Group(gameId.ToString()).SendAsync("StartGameCountdown");

                        // NEU: Warte ~4 Sekunden, bevor der Server-Timer gestartet wird.
                        // Gibt dem Client genug Zeit für die Animation.
                        await Task.Delay(4000);
                        _gameManager.StartGame(gameId);
                    }
                }
                else
                {
                    _logger.LogHubFailedToSendInitialHandSessionNotFound(gameId, playerId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogHubErrorSendingInitialHand(playerId, gameId, ex);
            }
        }

        // Entfernt einen Client aus einer Spielgruppe.
        public async Task LeaveGame(string gameIdString)
        {
            _logger.LogHubClientLeavingGameGroup(Context.ConnectionId, gameIdString);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameIdString);

            Guid gameIdGuid = Guid.Empty;
            if (_connectionToGameIdMap.TryRemove(Context.ConnectionId, out var gid)) gameIdGuid = gid;
            if (connectionToPlayerIdMap.TryRemove(Context.ConnectionId, out Guid playerId))
            {
                PlayerIdToConnectionMap.TryRemove(playerId, out _);
                if (gameIdGuid != Guid.Empty) _gameManager.UnregisterPlayerHubConnection(gameIdGuid, Context.ConnectionId);
                _logger.LogHubPlayerMappingRemovedOnDisconnect(playerId);
            }
            _logger.LogHubClientRemovedFromGameGroup(Context.ConnectionId, gameIdString);
        }

        #endregion
    }
}