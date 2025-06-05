using ChessLogic.Utilities;
using System.Collections.Generic; 
using System.Linq; 

namespace ChessLogic
{
    // Repräsentiert die Schachfigur Dame.
    public class Queen : Piece
    {
        // Definiert den Typ der Figur als Dame.
        public override PieceType Type => PieceType.Queen;
        // Definiert die Farbe der Dame (Weiss oder Schwarz).
        public override Player Color { get; }

        // Statisches Array der möglichen Bewegungsrichtungen für eine Dame.
        // Kombiniert die Richtungen von Turm (horizontal/vertikal) und Läufer (diagonal).
        private static readonly Direction[] dirs = new Direction[]
        {
            Direction.North, Direction.South, Direction.East, Direction.West,
            Direction.NorthWest, Direction.NorthEast, Direction.SouthWest, Direction.SouthEast
        };

        // Konstruktor für eine Dame.
        public Queen(Player color)
        {
            Color = color;
        }

        // Erstellt eine tiefe Kopie des Damen-Objekts.
        public override Piece Copy()
        {
            Queen copy = new Queen(Color);
            copy.HasMoved = HasMoved; // Übernimmt den Bewegungsstatus.
            return copy;
        }

        // Gibt alle möglichen Züge für die Dame von der gegebenen Position auf dem Brett zurück.
        // Verwendet die allgemeine Logik aus der Piece-Basisklasse für Bewegungen in Richtungen.
        public override IEnumerable<Move> GetMoves(Position from, Board board)
        {
            // Wählt für jede mögliche Zielposition einen NormalMove aus.
            return MovePositionsInDirs(from, board, dirs).Select(to => new NormalMove(from, to));
        }
    }
}