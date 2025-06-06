using ChessLogic;
using ChessNetwork.DTOs;
using System;

namespace ChessServer.Services
{
    public static class PieceExtensions
    {
        // Konvertiert ein Piece-Objekt in ein PieceDto für die Netzwerkübertragung.
        public static PieceDto ToDto(this Piece piece)
        {
            return (piece.Color, piece.Type) switch
            {
                (Player.White, PieceType.King) => PieceDto.WhiteKing,
                (Player.White, PieceType.Queen) => PieceDto.WhiteQueen,
                (Player.White, PieceType.Rook) => PieceDto.WhiteRook,
                (Player.White, PieceType.Bishop) => PieceDto.WhiteBishop,
                (Player.White, PieceType.Knight) => PieceDto.WhiteKnight,
                (Player.White, PieceType.Pawn) => PieceDto.WhitePawn,

                (Player.Black, PieceType.King) => PieceDto.BlackKing,
                (Player.Black, PieceType.Queen) => PieceDto.BlackQueen,
                (Player.Black, PieceType.Rook) => PieceDto.BlackRook,
                (Player.Black, PieceType.Bishop) => PieceDto.BlackBishop,
                (Player.Black, PieceType.Knight) => PieceDto.BlackKnight,
                (Player.Black, PieceType.Pawn) => PieceDto.BlackPawn,

                _ => throw new ArgumentOutOfRangeException(nameof(piece), $"Unbekannte Figur: {piece.Color}/{piece.Type}")
            };
        }
    }
}