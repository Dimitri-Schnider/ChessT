using ChessLogic.Utilities;
using System.Linq;

namespace ChessLogic
{
    // Abstrakte Basisklasse für alle Schachfiguren.
    public abstract class Piece
    {
        // Typ der Figur (Bauer, Springer, etc.).
        public abstract PieceType Type { get; }
        // Farbe der Figur (Weiss oder Schwarz).
        public abstract Player Color { get; }
        // Gibt an, ob die Figur bereits bewegt wurde.
        public bool HasMoved { get; set; }

        // Erstellt eine Kopie der Figur.
        public abstract Piece Copy();
        // Gibt alle möglichen Züge der Figur zurück.
        public abstract IEnumerable<Move> GetMoves(Position from, Board board);

        // Generiert Zielfelder in einer bestimmten Richtung, solange Felder frei sind oder eine gegnerische Figur geschlagen werden kann.
        protected IEnumerable<Position> MovePositionsInDir(Position from, Board board, Direction dir)
        {
            for (Position pos = from + dir; Board.IsInside(pos); pos += dir)
            {
                if (board.IsEmpty(pos))
                {
                    yield return pos;
                    continue;
                }

                Piece? piece = board[pos];
                if (piece?.Color != Color) // Kann gegnerische Figur schlagen.
                {
                    yield return pos;
                }
                yield break; // Eigene Figur oder geschlagene gegnerische Figur blockiert weiteren Weg.
            }
        }

        // Generiert Zielfelder in mehreren Richtungen.
        protected IEnumerable<Position> MovePositionsInDirs(Position from, Board board, Direction[] dirs)
        {
            return dirs.SelectMany(dir => MovePositionsInDir(from, board, dir));
        }

        // Prüft, ob die Figur den gegnerischen König schlagen könnte (Standardimplementierung).
        public virtual bool CanCaptureOpponentKing(Position from, Board board)
        {
            return GetMoves(from, board).Any(move =>
            {
                Piece? pieceOnToPos = board[move.ToPos];
                return pieceOnToPos != null && pieceOnToPos.Type == PieceType.King;
            });
        }
    }
}