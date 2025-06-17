using ChessLogic;
using System.Collections.Generic;
using System.Linq;

namespace ChessClient.Utils
{
    // Stellt clientseitige Hilfsmethoden für schachfigurenspezifische Logik bereit.
    public static class PieceHelperClient
    {
        // Gibt die ursprünglichen Startfelder für einen bestimmten Figurentyp und eine Farbe zurück.
        // Dies ist besonders für den "Wiedergeburt"-Karteneffekt nützlich.
        public static List<string> GetOriginalStartSquares(PieceType pieceType, Player color)
        {
            var squares = new List<string>();
            // Bestimmt die Grundreihe basierend auf der Spielerfarbe (0 für Schwarz, 7 für Weiss).
            int rankRow = (color == Player.White) ? 7 : 0;

            // Fügt die entsprechenden Startkoordinaten basierend auf dem Figurentyp hinzu.
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
                    // Bauern werden hier nicht berücksichtigt, da sie nicht wiederbelebt werden können.
            }
            return squares;
        }
    }
}