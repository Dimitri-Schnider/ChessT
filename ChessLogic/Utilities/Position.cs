using System;

namespace ChessLogic.Utilities
{
    // Repräsentiert eine Position (Feld) auf dem Schachbrett.
    public class Position
    {
        public int Row { get; }      // Zeilenindex (0-7).
        public int Column { get; }   // Spaltenindex (0-7).

        public Position(int row, int column)
        {
            Row = row;
            Column = column;
        }

        // Bestimmt die Farbe des Schachfeldes (Weiss oder Schwarz).
        public Player SquareColor()
        {
            return (Row + Column) % 2 == 0 ? Player.White : Player.Black;
        }

        // Überschreibt Equals für den Wertvergleich von Positionen.
        public override bool Equals(object? obj)
        {
            return obj is Position position && Row == position.Row && Column == position.Column;
        }

        // Stellt einen konsistenten Hashcode sicher.
        public override int GetHashCode()
        {
            return HashCode.Combine(Row, Column);
        }

        // Überlädt den Gleichheitsoperator (==).
        public static bool operator ==(Position? left, Position? right)
        {
            return System.Collections.Generic.EqualityComparer<Position>.Default.Equals(left, right);
        }

        // Überlädt den Ungleichheitsoperator (!=).
        public static bool operator !=(Position? left, Position? right)
        {
            return !(left == right);
        }

        // Addiert eine Richtung zu einer Position, um eine neue Position zu erhalten.
        public static Position operator +(Position pos, Direction dir)
        {
            return new Position(pos.Row + dir.RowDelta, pos.Column + dir.ColumnDelta);
        }
    }
}