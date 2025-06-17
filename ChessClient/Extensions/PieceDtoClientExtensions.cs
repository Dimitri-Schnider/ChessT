using ChessNetwork.DTOs;
using ChessLogic;

namespace ChessClient.Extensions
{
    // Enthält Erweiterungsmethoden für das PieceDto, um clientseitige Logik zu vereinfachen.
    public static class PieceDtoClientExtensions
    {
        // Prüft, ob eine Figur zur angegebenen Spielerfarbe gehört.
        public static bool IsOfPlayerColor(this PieceDto pieceDto, Player colorToCheck)
        {
            // Konvertiert den Enum-Wert in einen String für eine einfache Überprüfung.
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

        // Konvertiert ein `PieceDto` aus dem Netzwerk in ein `Piece`-Logikobjekt.
        public static Piece? ToPiece(this PieceDto? pieceDto)
        {
            // Wenn kein DTO vorhanden ist, gibt es auch kein Logikobjekt.
            if (!pieceDto.HasValue)
            {
                return null;
            }

            // Ordnet jedem DTO-Wert das entsprechende konkrete Piece-Objekt zu.
            return pieceDto.Value switch
            {
                PieceDto.WhiteKing => new King(Player.White),
                PieceDto.WhiteQueen => new Queen(Player.White),
                PieceDto.WhiteRook => new Rook(Player.White),
                PieceDto.WhiteBishop => new Bishop(Player.White),
                PieceDto.WhiteKnight => new Knight(Player.White),
                PieceDto.WhitePawn => new Pawn(Player.White),
                PieceDto.BlackKing => new King(Player.Black),
                PieceDto.BlackQueen => new Queen(Player.Black),
                PieceDto.BlackRook => new Rook(Player.Black),
                PieceDto.BlackBishop => new Bishop(Player.Black),
                PieceDto.BlackKnight => new Knight(Player.Black),
                PieceDto.BlackPawn => new Pawn(Player.Black),
                _ => null,
            };
        }
    }
}