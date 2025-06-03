using ChessLogic.Utilities;

namespace ChessLogic
{
    // Repräsentiert die Schachfigur Dame.
    public class Queen : Piece
    {
        // Typ: Dame.
        public override PieceType Type => PieceType.Queen;
        // Farbe der Dame.
        public override Player Color { get; }

        // Mögliche Bewegungsrichtungen (kombiniert Turm und Läufer).
        private static readonly Direction[] dirs = new Direction[]
                {
            Direction.North, Direction.South, Direction.East, Direction.West,
            Direction.NorthWest, Direction.NorthEast, Direction.SouthWest, Direction.SouthEast
                };
        // Konstruktor.
        public Queen(Player color)
        {
            Color = color;
        }

        // Erstellt eine Kopie der Dame.
        public override Piece Copy()
        {
            Queen copy = new Queen(Color);
            copy.HasMoved = HasMoved;
            return copy;
        }

        // Gibt alle legalen Züge der Dame zurück.
        public override IEnumerable<Move> GetMoves(Position from, Board board)
        {
            return MovePositionsInDirs(from, board, dirs).Select(to => new NormalMove(from, to));
        }
    }
}