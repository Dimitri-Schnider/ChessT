using ChessLogic.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace ChessLogic
{
    // Repräsentiert die Schachfigur Springer.
    public class Knight : Piece
    {
        public override PieceType Type => PieceType.Knight;
        public override Player Color { get; }

        public Knight(Player color)
        {
            Color = color;
        }

        // Erstellt eine tiefe Kopie des Springer-Objekts.
        public override Piece Copy()
        {
            return new Knight(Color) { HasMoved = HasMoved };
        }

        // Generiert alle acht potenziellen L-förmigen Sprungziele.
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

        // Filtert die potenziellen Sprungziele auf legale Züge.
        private IEnumerable<Position> MovePositions(Position from, Board board)
        {
            return PotentialToPositions(from)
                .Where(pos => Board.IsInside(pos) && (board.IsEmpty(pos) || board[pos]?.Color != Color));
        }

        // Gibt alle möglichen Züge für den Springer zurück.
        public override IEnumerable<Move> GetMoves(Position from, Board board)
        {
            return MovePositions(from, board).Select(to => new NormalMove(from, to));
        }
    }
}