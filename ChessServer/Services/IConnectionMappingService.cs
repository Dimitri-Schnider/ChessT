using System;

namespace ChessServer.Services
{
    public interface IConnectionMappingService
    {
        // Fügt eine neue Zuordnung für eine Verbindung hinzu.
        void AddMapping(string connectionId, Guid playerId, Guid gameId);

        // Entfernt alle Zuordnungen für eine bestimmte Verbindungs-ID.
        (Guid? PlayerId, Guid? GameId) RemoveMapping(string connectionId);

        // Ruft die Verbindungs-ID für eine gegebene Spieler-ID ab.
        string? GetConnectionId(Guid playerId);

        // Ruft die Spieler-ID für eine gegebene Verbindungs-ID ab.
        Guid? GetPlayerId(string connectionId);

        // Ruft die Spiel-ID für eine gegebene Verbindungs-ID ab.
        Guid? GetGameId(string connectionId);
    }
}