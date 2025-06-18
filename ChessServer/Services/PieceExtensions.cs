using ChessLogic;
using ChessNetwork.DTOs;
using System;

namespace ChessServer.Services
{
    // Stellt Erweiterungsmethoden für die `Piece`-Klasse aus dem ChessLogic-Projekt bereit.
    public static class PieceExtensions
    {
        // Konvertiert ein `Piece`-Logikobjekt in ein `PieceDto` (Data Transfer Object) für die Netzwerkübertragung.
        // Dies entkoppelt die Kernlogik von der Netzwerkdarstellung.
        public static PieceDto ToDto(this Piece piece)
        {
            // Verwendet ein switch-Expression, um die Kombination aus Farbe und Typ direkt auf das passende DTO-Enum abzubilden.
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

                // Wirft eine Ausnahme, falls eine unbekannte oder ungültige Figur konvertiert werden soll.
                _ => throw new ArgumentOutOfRangeException(nameof(piece), $"Unbekannte Figur: {piece.Color}/{piece.Type}")
            };
        }
    }
}