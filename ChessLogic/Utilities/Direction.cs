namespace ChessLogic.Utilities
{
    // Repräsentiert eine Richtung auf dem Schachbrett.
    public class Direction
    {
        // Vordefinierte Hauptrichtungen.
        public readonly static Direction North = new Direction(-1, 0);
        public readonly static Direction South = new Direction(1, 0);
        public readonly static Direction East = new Direction(0, 1);
        public readonly static Direction West = new Direction(0, -1);
        // Vordefinierte diagonale Richtungen.
        public readonly static Direction NorthEast = North + East;
        public readonly static Direction NorthWest = North + West;
        public readonly static Direction SouthEast = South + East;
        public readonly static Direction SouthWest = South + West;

        // Änderung der Zeile.
        public int RowDelta { get; }
        // Änderung der Spalte.
        public int ColumnDelta { get; }

        // Konstruktor.
        public Direction(int rowDelta, int columnDelta)
        {
            RowDelta = rowDelta;
            ColumnDelta = columnDelta;
        }

        // Addiert zwei Richtungen.
        public static Direction operator +(Direction dir1, Direction dir2)
        {
            return new Direction(dir1.RowDelta + dir2.RowDelta, dir1.ColumnDelta + dir2.ColumnDelta);
        }

        // Multipliziert eine Richtung mit einem Skalar.
        public static Direction operator *(int scalar, Direction dir)
        {
            return new Direction(scalar * dir.RowDelta, scalar * dir.ColumnDelta);
        }
    }
}