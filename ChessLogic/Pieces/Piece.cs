using ChessLogic.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace ChessLogic
{
    // Abstrakte Basisklasse für alle Schachfiguren.
    public abstract class Piece
    {
        public abstract PieceType Type { get; } // Der Typ der Figur (Bauer, Springer, etc.).
        public abstract Player Color { get; }   // Die Farbe der Figur (Weiss oder Schwarz).
        public bool HasMoved { get; set; }      // Gibt an, ob die Figur bereits bewegt wurde.

        // Erstellt eine tiefe Kopie der Figur.
        public abstract Piece Copy();

        // Gibt alle möglichen Züge für diese Figur von der Position 'from' zurück.
        public abstract IEnumerable<Move> GetMoves(Position from, Board board);

        // Generiert alle möglichen Zielpositionen in einer einzelnen Richtung.
        protected IEnumerable<Position> MovePositionsInDir(Position from, Board board, Direction dir)
        {
            for (Position pos = from + dir; Board.IsInside(pos); pos += dir)
            {
                if (board.IsEmpty(pos))
                {
                    yield return pos;
                    continue;
                }

                if (board[pos]?.Color != Color)
                {
                    yield return pos;
                }

                yield break;
            }
        }

        // Generiert alle möglichen Zielpositionen in einem Array von Richtungen.
        protected IEnumerable<Position> MovePositionsInDirs(Position from, Board board, Direction[] dirs)
        {
            return dirs.SelectMany(dir => MovePositionsInDir(from, board, dir));
        }

        // Prüft, ob diese Figur von ihrer Position aus den gegnerischen König bedrohen kann.
        public virtual bool CanCaptureOpponentKing(Position from, Board board)
        {
            return GetMoves(from, board).Any(move => board[move.ToPos] is { Type: PieceType.King });
        }
    }
}