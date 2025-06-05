using System;
using System.Collections.Generic; // Wird hier nicht direkt verwendet, aber oft im Kontext von Positionen

namespace ChessLogic.Utilities
{
    // Repräsentiert eine Position auf dem Schachbrett mittels Zeilen- und Spaltenindizes (0-7).
    public class Position
    {
        // Zeilenindex, wobei 0 die oberste Reihe (schwarze Grundreihe) und 7 die unterste Reihe (weisse Grundreihe) ist.
        public int Row { get; }
        // Spaltenindex, wobei 0 die a-Linie und 7 die h-Linie ist.
        public int Column { get; }

        // Konstruktor zur Erstellung eines Positionsobjekts.
        public Position(int row, int column)
        {
            Row = row;
            Column = column;
        }

        // Bestimmt die Farbe des Schachfeldes an dieser Position.
        // Felder mit gerader Summe von Zeile und Spalte sind hier als "Weiss" definiert,
        // ungerade als "Schwarz". Dies hängt von der Konvention ab (ob A1 hell oder dunkel ist).
        public Player SquareColor()
        {
            if ((Row + Column) % 2 == 0)
            {
                return Player.White; // Oder Player.Light, je nach Definition der Feldfarben.
            }
            return Player.Black; // Oder Player.Dark.
        }

        // Überschreibt die Equals-Methode, um zwei Positionen auf Gleichheit ihrer Koordinaten zu prüfen.
        public override bool Equals(object? obj)
        {
            return obj is Position position &&
                   Row == position.Row &&
                   Column == position.Column;
        }

        // Überschreibt GetHashCode, um konsistente Hashwerte für gleiche Positionen zu gewährleisten.
        public override int GetHashCode()
        {
            return HashCode.Combine(Row, Column);
        }

        // Überlädt den Gleichheitsoperator (==) für den direkten Vergleich zweier Positionsobjekte.
        public static bool operator ==(Position? left, Position? right)
        {
            if (left is null)
            {
                return right is null; // Null ist nur gleich null.
            }
            return left.Equals(right); // Verwendet die überschriebene Equals-Methode.
        }

        // Überlädt den Ungleichheitsoperator (!=).
        public static bool operator !=(Position? left, Position? right)
        {
            return !(left == right);
        }

        // Überlädt den Additionsoperator (+), um eine Richtung zu einer Position zu addieren
        // und eine neue Position zurückzugeben.
        public static Position operator +(Position pos, Direction dir)
        {
            return new Position(pos.Row + dir.RowDelta, pos.Column + dir.ColumnDelta);
        }
    }
}