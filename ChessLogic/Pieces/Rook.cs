using ChessLogic.Utilities;
using System.Collections.Generic; 
using System.Linq;

namespace ChessLogic
{
    // Repräsentiert die Schachfigur Turm.
    public class Rook : Piece
    {
        // Definiert den Typ der Figur als Turm.
        public override PieceType Type => PieceType.Rook;
        // Definiert die Farbe des Turms (Weiss oder Schwarz).
        public override Player Color { get; }

        // Statisches Array der möglichen Bewegungsrichtungen für einen Turm (horizontal und vertikal).
        private static readonly Direction[] dirs = new Direction[]
        {
            Direction.North,
            Direction.South,
            Direction.East,
            Direction.West
        };

        // Konstruktor für einen Turm.
        public Rook(Player color)
        {
            Color = color;
        }

        // Erstellt eine tiefe Kopie des Turm-Objekts.
        public override Piece Copy()
        {
            Rook copy = new Rook(Color);
            copy.HasMoved = HasMoved; // Übernimmt den Bewegungsstatus.
            return copy;
        }

        // Gibt alle möglichen Züge für den Turm von der gegebenen Position auf dem Brett zurück.
        // Verwendet die allgemeine Logik aus der Piece-Basisklasse für Bewegungen in Richtungen.
        public override IEnumerable<Move> GetMoves(Position from, Board board)
        {
            // Wählt für jede mögliche Zielposition einen NormalMove aus.
            return MovePositionsInDirs(from, board, dirs).Select(to => new NormalMove(from, to));
        }
    }
}