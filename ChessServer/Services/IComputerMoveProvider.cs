using ChessNetwork.DTOs;
using System.Threading.Tasks;

namespace ChessServer.Services
{
    public interface IComputerMoveProvider
    {
        // Ruft den nächsten Zug für einen Computergegner von einer externen Quelle ab.
        Task<string?> GetNextMoveAsync(Guid gameId, string fen, int depth);
    }
}