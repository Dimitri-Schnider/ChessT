// File: [SolutionDir]/ChessServer/Hubs/ChessHub.cs
using Microsoft.AspNetCore.SignalR;
using ChessServer.Services;
using ChessNetwork.DTOs;
using System.Threading.Tasks;
using ChessLogic;
using System;
using System.Collections.Concurrent;
using Chess.Logging;

namespace ChessServer.Hubs
{
    public class ChessHub : Hub
    {
        public static readonly ConcurrentDictionary<string, Guid> connectionToPlayerIdMap = new();
        public static readonly ConcurrentDictionary<Guid, string> PlayerIdToConnectionMap = new();
        private static readonly ConcurrentDictionary<string, Guid> _connectionToGameIdMap = new();

        private readonly IGameManager _gameManager;
        private readonly IChessLogger _logger;

        public ChessHub(IGameManager gameManager, IChessLogger logger)
        {
            _gameManager = gameManager;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogHubClientConnected(Context.ConnectionId); // Neuer Log-Aufruf
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogHubClientDisconnected(Context.ConnectionId, exception?.Message, exception); // Neuer Log-Aufruf
            if (connectionToPlayerIdMap.TryRemove(Context.ConnectionId, out Guid playerId))
            {
                PlayerIdToConnectionMap.TryRemove(playerId, out _);
                _logger.LogHubPlayerMappingRemovedOnDisconnect(playerId); // Neuer Log-Aufruf
            }
            if (_connectionToGameIdMap.TryRemove(Context.ConnectionId, out Guid gameId))
            {
                _gameManager.UnregisterPlayerHubConnection(gameId, Context.ConnectionId);
                _logger.LogHubConnectionRemovedFromGameOnDisconnect(Context.ConnectionId, gameId); // Neuer Log-Aufruf
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task RegisterConnection(Guid gameId, Guid playerId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, gameId.ToString());
            _connectionToGameIdMap[Context.ConnectionId] = gameId;
            connectionToPlayerIdMap[Context.ConnectionId] = playerId;
            PlayerIdToConnectionMap[playerId] = Context.ConnectionId;

            _gameManager.RegisterPlayerHubConnection(gameId, playerId, Context.ConnectionId);

            _logger.LogHubPlayerRegisteredToHub(playerId, Context.ConnectionId, gameId); // Neuer Log-Aufruf

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
                _logger.LogHubGameNotFoundForPlayerCount(gameId, Context.ConnectionId); // Neuer Log-Aufruf
                playerCount = 1;
            }


            await Clients.Group(gameId.ToString()).SendAsync("PlayerJoined", playerName ?? "Ein Spieler", playerCount);
            _logger.LogHubPlayerJoinedNotificationSent(playerName ?? "Unbekannt", gameId.ToString(), playerCount); // Neuer Log-Aufruf

            try
            {
                if (_gameManager is InMemoryGameManager manager && manager.GetSessionForDirectHubSend(gameId) is GameSession session)
                {
                    List<CardDto> hand = session.GetPlayerHand(playerId);
                    int drawPileCount = session.GetDrawPileCount(playerId);
                    var initialHandDto = new InitialHandDto(hand, drawPileCount);

                    _logger.LogHubSendingInitialHand(playerId, Context.ConnectionId, gameId, initialHandDto.Hand.Count, initialHandDto.DrawPileCount); // Neuer Log-Aufruf
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
                            List<CardDto> opponentHand = session.GetPlayerHand(opponentId);
                            int opponentDrawPileCount = session.GetDrawPileCount(opponentId);
                            var opponentInitialHandDto = new InitialHandDto(opponentHand, opponentDrawPileCount);
                            _logger.LogHubSendingInitialHand(opponentId, opponentConnectionId, gameId, opponentInitialHandDto.Hand.Count, opponentInitialHandDto.DrawPileCount); // Neuer Log-Aufruf
                            await Clients.Client(opponentConnectionId).SendAsync("ReceiveInitialHand", opponentInitialHandDto);
                        }
                    }
                }
                else
                {
                    _logger.LogHubFailedToSendInitialHandSessionNotFound(gameId, playerId); // Neuer Log-Aufruf
                }
            }
            catch (Exception ex)
            {
                _logger.LogHubErrorSendingInitialHand(playerId, gameId, ex); // Neuer Log-Aufruf
            }
        }

        [Obsolete("Verwenden Sie RegisterConnection, nachdem der Spieler seine PlayerId erhalten hat.")]
        public async Task JoinGame(string gameIdString, string playerName)
        {
            if (!Guid.TryParse(gameIdString, out var gameIdGuid))
            {
                _logger.LogHubJoinGameInvalidGameIdFormat(gameIdString, Context.ConnectionId); // Neuer Log-Aufruf
                return;
            }
            _logger.LogHubClientJoiningGameGroup(Context.ConnectionId, gameIdString); // Neuer Log-Aufruf
            await Groups.AddToGroupAsync(Context.ConnectionId, gameIdString);
            _connectionToGameIdMap[Context.ConnectionId] = gameIdGuid;
            _logger.LogHubClientAddedToGameGroup(Context.ConnectionId, gameIdString); // Neuer Log-Aufruf

            _logger.LogHubPlayerActuallyJoinedGame(playerName, gameIdString); // Neuer Log-Aufruf

            int playerCount = 0;
            try
            {
                if (_gameManager.GetPlayerIdByColor(gameIdGuid, Player.White).HasValue) playerCount++;
                if (_gameManager.GetPlayerIdByColor(gameIdGuid, Player.Black).HasValue) playerCount++;
                var gameInfo = _gameManager.GetGameInfo(gameIdGuid);
                if (gameInfo.HasOpponent)
                {
                    playerCount = 2;
                }
                else if (_gameManager.GetPlayerIdByColor(gameIdGuid, Player.White).HasValue || _gameManager.GetPlayerIdByColor(gameIdGuid, Player.Black).HasValue)
                {
                    playerCount = 1;
                }
            }
            catch (KeyNotFoundException)
            {
                _logger.LogHubGameNotFoundForPlayerCount(gameIdGuid, Context.ConnectionId); // Neuer Log-Aufruf
                playerCount = 1;
            }

            await Clients.Group(gameIdString).SendAsync("PlayerJoined", playerName, playerCount);
            _logger.LogHubPlayerJoinedNotificationSent(playerName, gameIdString, playerCount); // Neuer Log-Aufruf
        }

        public async Task LeaveGame(string gameIdString)
        {
            _logger.LogHubClientLeavingGameGroup(Context.ConnectionId, gameIdString); // Neuer Log-Aufruf
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameIdString);

            Guid gameIdGuid = Guid.Empty;
            if (_connectionToGameIdMap.TryRemove(Context.ConnectionId, out var gid)) gameIdGuid = gid;
            if (connectionToPlayerIdMap.TryRemove(Context.ConnectionId, out Guid playerId))
            {
                PlayerIdToConnectionMap.TryRemove(playerId, out _);
                if (gameIdGuid != Guid.Empty) _gameManager.UnregisterPlayerHubConnection(gameIdGuid, Context.ConnectionId);
                _logger.LogHubPlayerMappingRemovedOnDisconnect(playerId); // Neuer Log-Aufruf
            }
            _logger.LogHubClientRemovedFromGameGroup(Context.ConnectionId, gameIdString); // Neuer Log-Aufruf
        }
    }
}
