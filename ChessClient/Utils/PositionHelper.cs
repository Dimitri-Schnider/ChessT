using System;
using System.Globalization;

namespace ChessClient.Utils
{
    // Eine statische Hilfsklasse zur Umwandlung zwischen Schachkoordinaten-Formaten.
    public static class PositionHelper
    {
        // Konvertiert eine algebraische Notation (z.B. "e4") in 0-basierte Zeilen- und Spaltenindizes.
        public static (int Row, int Column) ToIndices(string algebraicNotation)
        {
            // Überprüft, ob das Format gültig ist (z.B. zwei Zeichen lang).
            if (string.IsNullOrWhiteSpace(algebraicNotation) || algebraicNotation.Length != 2)
                throw new ArgumentException("Ungültiges Format für algebraische Notation. Erwartet z.B. 'e4'.", nameof(algebraicNotation));

            char fileChar = algebraicNotation[0]; // z.B. 'e'
            char rankChar = algebraicNotation[1]; // z.B. '4'

            // Überprüft, ob die Koordinaten innerhalb des Schachbretts liegen (a-h, 1-8).
            if (fileChar < 'a' || fileChar > 'h' || rankChar < '1' || rankChar > '8')
                throw new ArgumentException($"Ungültige Koordinate: '{algebraicNotation}'. Muss im Bereich a1-h8 liegen.", nameof(algebraicNotation));

            // Konvertiert den Buchstaben (file) in einen Spaltenindex (a=0, b=1, ...).
            int column = fileChar - 'a';
            // Konvertiert die Zahl (rank) in einen Zeilenindex (1=7, 2=6, ..., 8=0).
            int row = 8 - int.Parse(rankChar.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

            return (row, column);
        }

        // Konvertiert 0-basierte Zeilen- und Spaltenindizes in die algebraische Notation.
        public static string ToAlgebraic(int row, int column)
        {
            // Überprüft die Gültigkeit der Indizes.
            if (row < 0 || row > 7)
                throw new ArgumentOutOfRangeException(nameof(row), "Zeilenindex muss zwischen 0 und 7 liegen.");
            if (column < 0 || column > 7)
                throw new ArgumentOutOfRangeException(nameof(column), "Spaltenindex muss zwischen 0 und 7 liegen.");

            // Konvertiert den Spaltenindex in einen Buchstaben.
            char file = (char)('a' + column);
            // Konvertiert den Zeilenindex in eine Zahl.
            string rank = (8 - row).ToString(CultureInfo.InvariantCulture);

            return $"{file}{rank}";
        }
    }
}