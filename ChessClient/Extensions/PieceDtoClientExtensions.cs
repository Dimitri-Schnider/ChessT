using ChessNetwork.DTOs;
using ChessLogic;

namespace ChessClient.Extensions
{
    public static class PieceDtoClientExtensions
    {
        public static bool IsOfPlayerColor(this PieceDto pieceDto, Player colorToCheck)
        {
            string pieceName = pieceDto.ToString();
            if (colorToCheck == Player.White && pieceName.StartsWith("White", StringComparison.Ordinal))
            {
                return true;
            }
            if (colorToCheck == Player.Black && pieceName.StartsWith("Black", StringComparison.Ordinal))
            {
                return true;
            }
            return false;
        }
    }
}