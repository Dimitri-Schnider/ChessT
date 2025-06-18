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

        #region Instance Fields

        private readonly IGameManager _gameManager;
        private readonly IChessLogger _logger;
        private readonly IConnectionMappingService _connectionMappingService;

        #endregion

        public ChessHub(IGameManager gameManager, IChessLogger logger, IConnectionMappingService connectionMappingService)
        {
            _gameManager = gameManager;
            _logger = logger;
            _connectionMappingService = connectionMappingService;
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

            var (playerId, gameId) = _connectionMappingService.RemoveMapping(Context.ConnectionId);

            if (playerId.HasValue)
            {
                _logger.LogHubPlayerMappingRemovedOnDisconnect(playerId.Value);
            }
            if (gameId.HasValue)
            {
                _gameManager.UnregisterPlayerHubConnection(gameId.Value, Context.ConnectionId);
                _logger.LogHubConnectionRemovedFromGameOnDisconnect(Context.ConnectionId, gameId.Value);
            }

            await base.OnDisconnectedAsync(exception);
        }

        #endregion

        #region Hub Methods

        // Registriert eine Verbindung für einen bestimmten Spieler in einem Spiel.
        public async Task RegisterConnection(Guid gameId, Guid playerId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, gameId.ToString());

            // Den neuen Service für das Hinzufügen verwenden
            _connectionMappingService.AddMapping(Context.ConnectionId, playerId, gameId);

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

                        // Den neuen Service verwenden, um die ConnectionId des Gegners zu finden
                        if (opponentId != Guid.Empty && _connectionMappingService.GetConnectionId(opponentId) is string opponentConnectionId)
                        {
                            List<CardDto> opponentHand = session.CardManager.GetPlayerHand(opponentId);
                            int opponentDrawPileCount = session.CardManager.GetDrawPileCount(opponentId);
                            var opponentInitialHandDto = new InitialHandDto(opponentHand, opponentDrawPileCount);

                            _logger.LogHubSendingInitialHand(opponentId, opponentConnectionId, gameId, opponentInitialHandDto.Hand.Count, opponentInitialHandDto.DrawPileCount);
                            await Clients.Client(opponentConnectionId).SendAsync("ReceiveInitialHand", opponentInitialHandDto);
                        }

                        _logger.LogStartGameCountdown(gameId);
                        await Clients.Group(gameId.ToString()).SendAsync("StartGameCountdown");

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

            // Den neuen Service für das Aufräumen verwenden
            var (playerId, gameId) = _connectionMappingService.RemoveMapping(Context.ConnectionId);

            if (playerId.HasValue && gameId.HasValue)
            {
                _gameManager.UnregisterPlayerHubConnection(gameId.Value, Context.ConnectionId);
                _logger.LogHubPlayerMappingRemovedOnDisconnect(playerId.Value);
            }
            _logger.LogHubClientRemovedFromGameGroup(Context.ConnectionId, gameIdString);
        }

        #endregion
    }
}