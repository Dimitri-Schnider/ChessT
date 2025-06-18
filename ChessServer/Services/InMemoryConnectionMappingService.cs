using System;
using System.Collections.Concurrent;

namespace ChessServer.Services
{
    public class InMemoryConnectionMappingService : IConnectionMappingService
    {
        private readonly ConcurrentDictionary<string, Guid> _connectionToPlayerId = new();
        private readonly ConcurrentDictionary<Guid, string> _playerIdToConnection = new();
        private readonly ConcurrentDictionary<string, Guid> _connectionToGameId = new();

        public void AddMapping(string connectionId, Guid playerId, Guid gameId)
        {
            _connectionToPlayerId[connectionId] = playerId;
            _playerIdToConnection[playerId] = connectionId;
            _connectionToGameId[connectionId] = gameId;
        }

        public (Guid? PlayerId, Guid? GameId) RemoveMapping(string connectionId)
        {
            _connectionToGameId.TryRemove(connectionId, out Guid gameId);

            if (_connectionToPlayerId.TryRemove(connectionId, out Guid playerId))
            {
                _playerIdToConnection.TryRemove(playerId, out _);
                return (playerId, gameId);
            }

            return (null, gameId);
        }

        public string? GetConnectionId(Guid playerId)
        {
            return _playerIdToConnection.GetValueOrDefault(playerId);
        }

        public Guid? GetPlayerId(string connectionId)
        {
            return _connectionToPlayerId.GetValueOrDefault(connectionId);
        }

        public Guid? GetGameId(string connectionId)
        {
            return _connectionToGameId.GetValueOrDefault(connectionId);
        }
    }
}