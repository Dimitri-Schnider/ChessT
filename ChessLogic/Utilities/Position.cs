using System;
using System.Collections.Generic;

namespace ChessLogic.Utilities
{
    // Repräsentiert eine Position auf dem Schachbrett (Zeile, Spalte).
    public class Position
    {
        // Zeilenindex (0-7).
        public int Row { get; }
        // Spaltenindex (0-7).
        public int Column { get; }

        // Konstruktor.
        public Position(int row, int column)
        {
            Row = row;
            Column = column;
        }

        // Gibt die Farbe des Feldes zurück.
        public Player SquareColor()
        {
            if ((Row + Column) % 2 == 0)
            {
                return Player.White;
            }
            return Player.Black;
        }

        // Überschreibt Equals für Positionsvergleich.
        public override bool Equals(object? obj)
        {
            return obj is Position position &&
                   Row == position.Row &&
                   Column == position.Column;
        }

        // Überschreibt GetHashCode.
        public override int GetHashCode()
        {
            return HashCode.Combine(Row, Column);
        }

        // Gleichheitsoperator.
        public static bool operator ==(Position? left, Position? right)
        {
            if (left is null)
            {
                return right is null;
            }
            return left.Equals(right);
        }

        // Ungleichheitsoperator.
        public static bool operator !=(Position? left, Position? right)
        {
            return !(left == right);
        }

        // Addiert eine Richtung zu einer Position.
        public static Position operator +(Position pos, Direction dir)
        {
            return new Position(pos.Row + dir.RowDelta, pos.Column + dir.ColumnDelta);
        }
    }
}