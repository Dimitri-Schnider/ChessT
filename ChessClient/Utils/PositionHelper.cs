using System;
using System.Globalization;

namespace ChessClient.Utils
{
    // Hilfsklasse zur Umwandlung von Schachkoordinaten.
    public static class PositionHelper
    {
        // Konvertiert algebraische Notation (z.B. "e4") in Zeilen- und Spaltenindizes (0-7).
        public static (int Row, int Column) ToIndices(string algebraicNotation)
        {
            if (string.IsNullOrWhiteSpace(algebraicNotation) || algebraicNotation.Length != 2)
                throw new ArgumentException("Ungültiges Format für algebraische Notation. Erwartet z.B. 'e4'.", nameof(algebraicNotation));

            char fileChar = algebraicNotation[0];
            char rankChar = algebraicNotation[1];

            if (fileChar < 'a' || fileChar > 'h' || rankChar < '1' || rankChar > '8')
                throw new ArgumentException($"Ungültige Koordinate: '{algebraicNotation}'. Muss im Bereich a1-h8 liegen.", nameof(algebraicNotation));
            int column = fileChar - 'a';
            int row = 8 - int.Parse(rankChar.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

            return (row, column);
        }

        // Konvertiert Zeilen- und Spaltenindizes (0-7) in algebraische Notation (z.B. "e4").
        public static string ToAlgebraic(int row, int column)
        {
            if (row < 0 || row > 7)
                throw new ArgumentOutOfRangeException(nameof(row), "Zeilenindex muss zwischen 0 und 7 liegen.");
            if (column < 0 || column > 7)
                throw new ArgumentOutOfRangeException(nameof(column), "Spaltenindex muss zwischen 0 und 7 liegen.");
            char file = (char)('a' + column);
            string rank = (8 - row).ToString(CultureInfo.InvariantCulture);
            return $"{file}{rank}";
        }
    }
}