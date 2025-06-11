using ChessLogic;
using ChessNetwork.DTOs;
using System;
using System.Collections.Generic;

namespace ChessServer.Services
{
    public interface IPlayerManager
    {
        int PlayerCount { get; }
        bool HasOpponent { get; }
        Guid FirstPlayerId { get; }
        Player FirstPlayerColor { get; }
        string OpponentType { get; }
        Guid? ComputerPlayerId { get; }

        (Guid PlayerId, Player Color) Join(string playerName, Player? preferredColor = null);
        Player GetPlayerColor(Guid playerId);
        Guid? GetPlayerIdByColor(Player color);
        string? GetPlayerName(Guid playerId);
        OpponentInfoDto? GetOpponentDetails(Guid currentPlayerId);
    }
}