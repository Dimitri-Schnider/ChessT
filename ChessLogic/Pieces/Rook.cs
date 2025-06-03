using ChessLogic.Utilities;

namespace ChessLogic
{
    // Repräsentiert die Schachfigur Turm.
    public class Rook : Piece
    {
        // Typ: Turm.
        public override PieceType Type => PieceType.Rook;
        // Farbe des Turms.
        public override Player Color { get; }

        // Mögliche Bewegungsrichtungen (horizontal und vertikal).
        private static readonly Direction[] dirs = new Direction[]
                {
            Direction.North,
            Direction.South,
            Direction.East,
            Direction.West
                };
        // Konstruktor.
        public Rook(Player color)
        {
            Color = color;
        }

        // Erstellt eine Kopie des Turms.
        public override Piece Copy()
        {
            Rook copy = new Rook(Color);
            copy.HasMoved = HasMoved;
            return copy;
        }

        // Gibt alle legalen Züge des Turms zurück.
        public override IEnumerable<Move> GetMoves(Position from, Board board)
        {
            return MovePositionsInDirs(from, board, dirs).Select(to => new NormalMove(from, to));
        }
    }
}