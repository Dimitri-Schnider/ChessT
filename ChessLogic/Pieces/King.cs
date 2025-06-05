using ChessLogic.Utilities;
using System.Collections.Generic; 
using System.Linq;

namespace ChessLogic
{
    // Repräsentiert die Schachfigur König.
    public class King : Piece
    {
        // Definiert den Typ der Figur als König.
        public override PieceType Type => PieceType.King;
        // Definiert die Farbe des Königs (Weiss oder Schwarz).
        public override Player Color { get; }

        // Statisches Array der möglichen Bewegungsrichtungen für einen König (alle angrenzenden Felder).
        private static readonly Direction[] dirs = new Direction[]
        {
            Direction.North, Direction.South, Direction.East, Direction.West,
            Direction.NorthWest, Direction.NorthEast, Direction.SouthWest, Direction.SouthEast
        };

        // Konstruktor für einen König.
        public King(Player color)
        {
            Color = color;
        }

        // Prüft, ob ein Turm auf der gegebenen Position für eine Rochade noch unbewegt ist.
        private static bool IsUnmovedRook(Position pos, Board board)
        {
            if (board.IsEmpty(pos))
            {
                return false;
            }
            Piece? piece = board[pos];
            return piece != null && piece.Type == PieceType.Rook && !piece.HasMoved;
        }

        // Prüft, ob alle Positionen in einer Liste von Positionen auf dem Brett leer sind.
        private static bool AllEmpty(IEnumerable<Position> positions, Board board)
        {
            return positions.All(board.IsEmpty);
        }

        // Prüft, ob die Bedingungen für eine kurze Rochade (Königsseite) erfüllt sind.
        private bool CanCastleKingSide(Position from, Board board)
        {
            // König darf noch nicht gezogen haben.
            if (HasMoved)
            {
                return false;
            }
            // Position des Turms auf der Königsseite.
            Position rookPos = new Position(from.Row, 7);
            // Felder zwischen König und Turm müssen leer sein.
            Position[] betweenPositions = { new(from.Row, 5), new(from.Row, 6) };
            return IsUnmovedRook(rookPos, board) && AllEmpty(betweenPositions, board);
        }

        // Prüft, ob die Bedingungen für eine lange Rochade (Damenseite) erfüllt sind.
        private bool CanCastleQueenSide(Position from, Board board)
        {
            // König darf noch nicht gezogen haben.
            if (HasMoved)
            {
                return false;
            }
            // Position des Turms auf der Damenseite.
            Position rookPos = new Position(from.Row, 0);
            // Felder zwischen König und Turm müssen leer sein.
            Position[] betweenPositions = { new(from.Row, 1), new(from.Row, 2), new(from.Row, 3) };
            return IsUnmovedRook(rookPos, board) && AllEmpty(betweenPositions, board);
        }

        // Erstellt eine tiefe Kopie des König-Objekts.
        public override Piece Copy()
        {
            King copy = new King(Color);
            copy.HasMoved = HasMoved; // Übernimmt den Bewegungsstatus.
            return copy;
        }

        // Generiert alle potenziellen normalen (nicht-Rochade) Zielpositionen für den König.
        private IEnumerable<Position> MovePositions(Position from, Board board)
        {
            foreach (Direction dir in dirs)
            {
                Position to = from + dir;
                // Ziel muss auf dem Brett sein.
                if (!Board.IsInside(to))
                {
                    continue;
                }
                // Ziel muss leer oder von einer gegnerischen Figur besetzt sein.
                if (board.IsEmpty(to) || board[to]?.Color != Color)
                {
                    yield return to;
                }
            }
        }

        // Gibt alle möglichen Züge für den König zurück, inklusive normaler Züge und Rochaden.
        public override IEnumerable<Move> GetMoves(Position from, Board board)
        {
            // Normale Königszüge.
            foreach (Position to in MovePositions(from, board))
            {
                yield return new NormalMove(from, to);
            }
            // Mögliche kurze Rochade.
            if (CanCastleKingSide(from, board))
            {
                yield return new Castle(MoveType.CastleKS, from);
            }
            // Mögliche lange Rochade.
            if (CanCastleQueenSide(from, board))
            {
                yield return new Castle(MoveType.CastleQS, from);
            }
        }

        // Prüft, ob der König von seiner aktuellen Position aus den gegnerischen König (theoretisch) schlagen könnte.
        // Relevant für die Schachmatt- und Patt-Logik, um zu sehen, ob der gegnerische König ein Feld betreten kann.
        public override bool CanCaptureOpponentKing(Position from, Board board)
        {
            // Prüft, ob eine der möglichen Zielpositionen vom gegnerischen König besetzt ist.
            return MovePositions(from, board).Any(to =>
            {
                Piece? piece = board[to];
                return piece != null && piece.Type == PieceType.King;
            });
        }
    }
}