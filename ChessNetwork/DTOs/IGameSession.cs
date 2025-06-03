using System;
using System.Threading.Tasks;
using ChessLogic;
using ChessNetwork.DTOs;
namespace ChessNetwork
{
    public interface IGameSession
    {
        Task<CreateGameResultDto> CreateGameAsync(string playerName, Player color, int initialMinutes);
        Task<JoinGameResultDto> JoinGameAsync(Guid gameId, string playerName);
        Task<BoardDto> GetBoardAsync(Guid gameId);
        Task<MoveResultDto> SendMoveAsync(Guid gameId, MoveDto move);
        Task<GameInfoDto> GetGameInfoAsync(Guid gameId);
        Task<IEnumerable<string>> GetLegalMovesAsync(Guid gameId, string from, Guid playerId);
        Task<GameStatusDto> GetGameStatusAsync(Guid gameId, Guid playerId);
        Task<Player> GetCurrentTurnPlayerAsync(Guid gameId);
        Task<TimeUpdateDto> GetTimeUpdateAsync(Guid gameId);
        Task<ServerCardActivationResultDto> ActivateCardAsync(Guid gameId, Guid playerId, ActivateCardRequestDto cardActivationRequest);
        Task<IEnumerable<CapturedPieceTypeDto>> GetCapturedPiecesAsync(Guid gameId, Guid playerId);
        Task<OpponentInfoDto?> GetOpponentInfoAsync(Guid gameId, Guid playerId);
    }
}