using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChessLogic;
using ChessNetwork.DTOs;

namespace ChessServer.Services
{
    public interface IGameManager
    {
        (Guid GameId, Guid PlayerId) CreateGame(string playerName, Player color, int initialMinutes, string opponentType = "Human", string computerDifficulty = "Medium"); 
        (Guid PlayerId, Player Color) JoinGame(Guid gameId, string playerName);
        BoardDto GetState(Guid gameId);
        MoveResultDto ApplyMove(Guid gameId, MoveDto move, Guid playerId);
        GameInfoDto GetGameInfo(Guid gameId);
        IEnumerable<string> GetLegalMoves(Guid gameId, Guid playerId, string from);
        GameStatusDto GetGameStatus(Guid gameId, Guid playerId);
        Player GetCurrentTurnPlayer(Guid gameId);
        GameStatusDto GetGameStatusForOpponentOf(Guid gameId, Guid lastPlayerId);
        TimeUpdateDto GetTimeUpdate(Guid gameId);
        GameHistoryDto GetGameHistory(Guid gameId);
        Task<ServerCardActivationResultDto> ActivateCardEffect(Guid gameId, Guid playerId, ActivateCardRequestDto cardActivationRequestDto);
        Task<IEnumerable<CapturedPieceTypeDto>> GetCapturedPieces(Guid gameId, Guid playerId);
        Guid? GetPlayerIdByColor(Guid gameId, Player color);
        OpponentInfoDto? GetOpponentInfo(Guid gameId, Guid currentPlayerId);
        int GetDrawPileCount(Guid gameId, Guid playerId);
        Player GetPlayerColor(Guid gameId, Guid playerId);
        string? GetPlayerName(Guid gameId, Guid playerId);
        void RegisterPlayerHubConnection(Guid gameId, Guid playerId, string connectionId);
        void UnregisterPlayerHubConnection(Guid gameId, string connectionId);
        List<CardDto> GetPlayerHand(Guid gameId, Guid playerId);
    }
}