using System;
using ChessNetwork.DTOs;

namespace ChessClient.Extensions
{
    // Stellt Erweiterungsmethoden für das PieceDto zur Verfügung.
    public static class PieceDtoExtensions
    {
        // Konvertiert ein PieceDto in den relativen Pfad seines Bildes im wwwroot-Ordner.
        public static string ToImagePath(this PieceDto piece)
        {
            // Ordnet jedem Enum-Wert den entsprechenden Bildpfad zu.
            return piece switch
            {
                PieceDto.WhiteKing => "img/pieces/white_king.png",
                PieceDto.WhiteQueen => "img/pieces/white_queen.png",
                PieceDto.WhiteRook => "img/pieces/white_rook.png",
                PieceDto.WhiteBishop => "img/pieces/white_bishop.png",
                PieceDto.WhiteKnight => "img/pieces/white_knight.png",
                PieceDto.WhitePawn => "img/pieces/white_pawn.png",

                PieceDto.BlackKing => "img/pieces/black_king.png",
                PieceDto.BlackQueen => "img/pieces/black_queen.png",
                PieceDto.BlackRook => "img/pieces/black_rook.png",
                PieceDto.BlackBishop => "img/pieces/black_bishop.png",
                PieceDto.BlackKnight => "img/pieces/black_knight.png",
                PieceDto.BlackPawn => "img/pieces/black_pawn.png",

                // Wirft eine Ausnahme, falls ein unbekannter oder ungültiger Wert übergeben wird.
                _ => throw new ArgumentOutOfRangeException(nameof(piece), piece, "Unbekannte Figur")
            };
        }
    }
}