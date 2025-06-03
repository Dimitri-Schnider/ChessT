using ChessLogic.Utilities;
using System.Linq;

namespace ChessLogic
{
    // Repräsentiert die Schachfigur Springer.
    public class Knight : Piece
    {
        // Typ: Springer.
        public override PieceType Type => PieceType.Knight;
        // Farbe des Springers.
        public override Player Color { get; }

        // Konstruktor.
        public Knight(Player color)
        {
            Color = color;
        }

        // Erstellt eine Kopie des Springers.
        public override Piece Copy()
        {
            Knight copy = new Knight(Color);
            copy.HasMoved = HasMoved;
            return copy;
        }

        // Generiert alle potenziellen L-förmigen Sprungziele.
        private static IEnumerable<Position> PotentialToPositions(Position from)
        {
            foreach (Direction vDir in new Direction[] { Direction.North, Direction.South })
            {
                foreach (Direction hDir in new Direction[] { Direction.West, Direction.East })
                {
                    yield return from + 2 * vDir + hDir;
                    yield return from + 2 * hDir + vDir;
                }
            }
        }

        // Filtert potenzielle Ziele nach Brettgrenzen und Belegung.
        private IEnumerable<Position> MovePositions(Position from, Board board)
        {
            return PotentialToPositions(from).Where(pos => Board.IsInside(pos)
                 && (board.IsEmpty(pos) || board[pos]?.Color != Color));
        }

        // Gibt alle legalen Züge des Springers zurück.
        public override IEnumerable<Move> GetMoves(Position from, Board board)
        {
            return MovePositions(from, board).Select(to => new NormalMove(from, to));
        }
    }
}