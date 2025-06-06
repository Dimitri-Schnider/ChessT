using ChessLogic.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace ChessLogic
{
    // Repräsentiert die Schachfigur König.
    public class King : Piece
    {
        public override PieceType Type => PieceType.King;
        public override Player Color { get; }

        // Alle 8 möglichen Richtungen, in die sich ein König bewegen kann.
        private static readonly Direction[] dirs = new Direction[]
        {
            Direction.North, Direction.South, Direction.East, Direction.West,
            Direction.NorthWest, Direction.NorthEast, Direction.SouthWest, Direction.SouthEast
        };

        public King(Player color)
        {
            Color = color;
        }

        // Prüft, ob ein Turm auf einer bestimmten Position für eine Rochade unbewegt ist.
        private static bool IsUnmovedRook(Position pos, Board board)
        {
            return board[pos] is Rook rook && !rook.HasMoved;
        }

        // Prüft, ob alle übergebenen Positionen auf dem Brett leer sind.
        private static bool AllEmpty(IEnumerable<Position> positions, Board board)
        {
            return positions.All(board.IsEmpty);
        }

        // Prüft, ob die Bedingungen für eine kurze Rochade (Königsseite) erfüllt sind.
        private bool CanCastleKingSide(Position from, Board board)
        {
            if (HasMoved) return false;
            Position rookPos = new(from.Row, 7);
            Position[] betweenPositions = { new(from.Row, 5), new(from.Row, 6) };
            return IsUnmovedRook(rookPos, board) && AllEmpty(betweenPositions, board);
        }

        // Prüft, ob die Bedingungen für eine lange Rochade (Damenseite) erfüllt sind.
        private bool CanCastleQueenSide(Position from, Board board)
        {
            if (HasMoved) return false;
            Position rookPos = new(from.Row, 0);
            Position[] betweenPositions = { new(from.Row, 1), new(from.Row, 2), new(from.Row, 3) };
            return IsUnmovedRook(rookPos, board) && AllEmpty(betweenPositions, board);
        }

        // Erstellt eine tiefe Kopie des König-Objekts.
        public override Piece Copy()
        {
            return new King(Color) { HasMoved = HasMoved };
        }

        // Generiert alle potenziellen normalen (nicht-Rochade) Zielpositionen.
        private IEnumerable<Position> MovePositions(Position from, Board board)
        {
            foreach (Direction dir in dirs)
            {
                Position to = from + dir;
                if (Board.IsInside(to) && (board.IsEmpty(to) || board[to]?.Color != Color))
                {
                    yield return to;
                }
            }
        }

        // Gibt alle möglichen Züge für den König zurück, inklusive Rochaden.
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

        // Prüft, ob der König von seiner Position aus den gegnerischen König bedroht.
        public override bool CanCaptureOpponentKing(Position from, Board board)
        {
            return MovePositions(from, board).Any(to => board[to] is { Type: PieceType.King });
        }
    }
}