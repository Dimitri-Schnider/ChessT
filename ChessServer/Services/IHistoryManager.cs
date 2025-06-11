using ChessLogic;
using ChessNetwork.DTOs;
using System;

namespace ChessServer.Services
{
    public interface IHistoryManager
    {
        void AddMove(PlayedMoveDto move);
        void AddPlayedCard(PlayedCardDto card);
        void UpdateOnGameOver(Result result);
        GameHistoryDto GetGameHistory(IPlayerManager playerManager);
        int GetMoveCount();
    }
}