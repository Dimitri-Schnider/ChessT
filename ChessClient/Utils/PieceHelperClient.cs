using ChessLogic;
using System.Collections.Generic;
using System.Linq;

namespace ChessClient.Utils
{
    public static class PieceHelperClient
    {
        // Gibt die eindeutige(n) Standard-Startposition(en) für einen Figurentyp und eine Farbe zurück.
        // Angepasste Version für Client-Seite.
        public static List<string> GetOriginalStartSquares(PieceType pieceType, Player color)
        {
            var squares = new List<string>();
            int rankRow = (color == Player.White) ? 7 : 0; // 0-basierter Index für das Brett-Array

            switch (pieceType)
            {
                case PieceType.King:
                    squares.Add(PositionHelper.ToAlgebraic(rankRow, 4)); // e-Linie
                    break;
                case PieceType.Queen:
                    squares.Add(PositionHelper.ToAlgebraic(rankRow, 3)); // d-Linie
                    break;
                case PieceType.Rook:
                    squares.Add(PositionHelper.ToAlgebraic(rankRow, 0)); // a-Linie
                    squares.Add(PositionHelper.ToAlgebraic(rankRow, 7)); // h-Linie
                    break;
                case PieceType.Bishop:
                    squares.Add(PositionHelper.ToAlgebraic(rankRow, 2)); // c-Linie
                    squares.Add(PositionHelper.ToAlgebraic(rankRow, 5)); // f-Linie
                    break;
                case PieceType.Knight:
                    squares.Add(PositionHelper.ToAlgebraic(rankRow, 1)); // b-Linie
                    squares.Add(PositionHelper.ToAlgebraic(rankRow, 6)); // g-Linie
                    break;
                    // Bauern werden hier nicht behandelt
            }
            return squares;
        }
    }
}