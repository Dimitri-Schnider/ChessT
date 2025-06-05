namespace ChessLogic.Utilities
{
    // Repräsentiert eine Richtung auf dem Schachbrett als Änderung der Zeilen- und Spaltenindizes.
    public class Direction
    {
        // Vordefinierte Hauptrichtungen.
        public readonly static Direction North = new Direction(-1, 0); // Bewegung eine Reihe nach oben.
        public readonly static Direction South = new Direction(1, 0);  // Bewegung eine Reihe nach unten.
        public readonly static Direction East = new Direction(0, 1);   // Bewegung eine Spalte nach rechts.
        public readonly static Direction West = new Direction(0, -1);  // Bewegung eine Spalte nach links.

        // Vordefinierte diagonale Richtungen, zusammengesetzt aus den Hauptrichtungen.
        public readonly static Direction NorthEast = North + East; // Oben-rechts.
        public readonly static Direction NorthWest = North + West; // Oben-links.
        public readonly static Direction SouthEast = South + East; // Unten-rechts.
        public readonly static Direction SouthWest = South + West; // Unten-links.

        // Die Änderung im Zeilenindex (negativ für aufwärts, positiv für abwärts).
        public int RowDelta { get; }
        // Die Änderung im Spaltenindex (negativ für links, positiv für rechts).
        public int ColumnDelta { get; }

        // Konstruktor für eine Richtung.
        public Direction(int rowDelta, int columnDelta)
        {
            RowDelta = rowDelta;
            ColumnDelta = columnDelta;
        }

        // Addiert zwei Richtungen, um eine neue kombinierte Richtung zu erhalten.
        public static Direction operator +(Direction dir1, Direction dir2)
        {
            return new Direction(dir1.RowDelta + dir2.RowDelta, dir1.ColumnDelta + dir2.ColumnDelta);
        }

        // Multipliziert eine Richtung mit einem Skalar, um die Bewegung in dieser Richtung zu verlängern.
        // Z.B. 2 * Direction.North bedeutet zwei Schritte nach Norden.
        public static Direction operator *(int scalar, Direction dir)
        {
            return new Direction(scalar * dir.RowDelta, scalar * dir.ColumnDelta);
        }
    }
}