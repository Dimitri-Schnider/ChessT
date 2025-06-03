using ChessLogic.Utilities;

namespace ChessLogic
{
    // Repräsentiert die Schachfigur Läufer.
    public class Bishop : Piece
    {
        // Typ: Läufer.
        public override PieceType Type => PieceType.Bishop;
        // Farbe des Läufers.
        public override Player Color { get; }

        // Mögliche Bewegungsrichtungen (diagonal).
        private static readonly Direction[] dirs = new Direction[]
                {
            Direction.NorthWest,
            Direction.NorthEast,
            Direction.SouthWest,
            Direction.SouthEast
                };
        // Konstruktor.
        public Bishop(Player color)
        {
            Color = color;
        }

        // Erstellt eine Kopie des Läufers.
        public override Piece Copy()
        {
            Bishop copy = new Bishop(Color);
            copy.HasMoved = HasMoved;
            return copy;
        }

        // Gibt alle legalen Züge des Läufers zurück.
        public override IEnumerable<Move> GetMoves(Position from, Board board)
        {
            return MovePositionsInDirs(from, board, dirs).Select(to => new NormalMove(from, to));
        }
    }
}