using ChessLogic.Utilities;
using System.Linq;

namespace ChessLogic
{
    // Repräsentiert die Schachfigur König.
    public class King : Piece
    {
        // Typ: König.
        public override PieceType Type => PieceType.King;
        // Farbe des Königs.
        public override Player Color { get; }

        // Mögliche Bewegungsrichtungen (alle angrenzenden Felder).
        private static readonly Direction[] dirs = new Direction[]
        {
            Direction.North, Direction.South, Direction.East, Direction.West,
            Direction.NorthWest, Direction.NorthEast, Direction.SouthWest, Direction.SouthEast
        };
        // Konstruktor.
        public King(Player color)
        {
            Color = color;
        }

        // Prüft, ob ein Turm für Rochade unbewegt ist.
        private static bool IsUnmovedRook(Position pos, Board board)
        {
            if (board.IsEmpty(pos)) return false;
            Piece? piece = board[pos];
            return piece != null && piece.Type == PieceType.Rook && !piece.HasMoved;
        }

        // Prüft, ob alle Positionen in einer Liste leer sind.
        private static bool AllEmpty(IEnumerable<Position> positions, Board board)
        {
            return positions.All(board.IsEmpty);
        }

        // Prüft Möglichkeit zur kurzen Rochade.
        private bool CanCastleKingSide(Position from, Board board)
        {
            if (HasMoved) return false;
            Position rookPos = new Position(from.Row, 7);
            Position[] betweenPositions = { new(from.Row, 5), new(from.Row, 6) };
            return IsUnmovedRook(rookPos, board) && AllEmpty(betweenPositions, board);
        }

        // Prüft Möglichkeit zur langen Rochade.
        private bool CanCastleQueenSide(Position from, Board board)
        {
            if (HasMoved) return false;
            Position rookPos = new Position(from.Row, 0);
            Position[] betweenPositions = { new(from.Row, 1), new(from.Row, 2), new(from.Row, 3) };
            return IsUnmovedRook(rookPos, board) && AllEmpty(betweenPositions, board);
        }

        // Erstellt eine Kopie des Königs.
        public override Piece Copy()
        {
            King copy = new King(Color);
            copy.HasMoved = HasMoved;
            return copy;
        }

        // Gibt alle möglichen Zielfelder des Königs zurück.
        private IEnumerable<Position> MovePositions(Position from, Board board)
        {
            foreach (Direction dir in dirs)
            {
                Position to = from + dir;
                if (!Board.IsInside(to)) continue;
                if (board.IsEmpty(to) || board[to]?.Color != Color)
                {
                    yield return to;
                }
            }
        }

        // Gibt alle legalen Züge des Königs zurück (inkl. Rochade).
        public override IEnumerable<Move> GetMoves(Position from, Board board)
        {
            foreach (Position to in MovePositions(from, board))
            {
                yield return new NormalMove(from, to);
            }
            if (CanCastleKingSide(from, board))
            {
                yield return new Castle(MoveType.CastleKS, from);
            }
            if (CanCastleQueenSide(from, board))
            {
                yield return new Castle(MoveType.CastleQS, from);
            }
        }

        // Prüft, ob der König einen gegnerischen König schlagen könnte (theoretisch).
        public override bool CanCaptureOpponentKing(Position from, Board board)
        {
            return MovePositions(from, board).Any(to =>
            {
                Piece? piece = board[to];
                return piece != null && piece.Type == PieceType.King;
            });
        }
    }
}