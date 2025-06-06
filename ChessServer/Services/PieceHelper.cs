using ChessLogic;
using ChessLogic.Utilities;
using System.Collections.Generic;
using System.Globalization;

namespace ChessServer.Services
{
    public static class PieceHelper
    {
        // Gibt die eindeutige(n) Standard-Startposition(en) für einen Figurentyp und eine Farbe zurück.
        public static List<Position> GetOriginalStartSquares(PieceType pieceType, Player color)
        {
            var squares = new List<Position>();
            int rank = (color == Player.White) ? 7 : 0; // Grundreihe für Offiziere

            switch (pieceType)
            {
                case PieceType.King:
                    squares.Add(new Position(rank, 4)); // e-Linie
                    break;
                case PieceType.Queen:
                    squares.Add(new Position(rank, 3)); // d-Linie
                    break;
                case PieceType.Rook:
                    squares.Add(new Position(rank, 0)); // a-Linie
                    squares.Add(new Position(rank, 7)); // h-Linie
                    break;
                case PieceType.Bishop:
                    squares.Add(new Position(rank, 2)); // c-Linie
                    squares.Add(new Position(rank, 5)); // f-Linie
                    break;
                case PieceType.Knight:
                    squares.Add(new Position(rank, 1)); // b-Linie
                    squares.Add(new Position(rank, 6)); // g-Linie
                    break;
            }
            return squares;
        }

        // Konvertiert eine Position in algebraische Notation.
        public static string ToAlgebraic(Position pos)
        {
            if (pos == null) return string.Empty;
            char file = (char)('a' + pos.Column);
            string rankNum = (8 - pos.Row).ToString(CultureInfo.InvariantCulture);
            return $"{file}{rankNum}";
        }
    }
}