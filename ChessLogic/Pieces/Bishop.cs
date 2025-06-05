using ChessLogic.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace ChessLogic
{
    // Repräsentiert die Schachfigur Läufer.
    public class Bishop : Piece
    {
        // Definiert den Typ der Figur als Läufer.
        public override PieceType Type => PieceType.Bishop;
        // Definiert die Farbe des Läufers (Weiss oder Schwarz).
        public override Player Color { get; }

        // Statisches Array der möglichen Bewegungsrichtungen für einen Läufer (diagonal).
        private static readonly Direction[] dirs = new Direction[]
        {
            Direction.NorthWest,
            Direction.NorthEast,
            Direction.SouthWest,
            Direction.SouthEast
        };

        // Konstruktor für einen Läufer.
        public Bishop(Player color)
        {
            Color = color;
        }

        // Erstellt eine tiefe Kopie des Läufer-Objekts.
        public override Piece Copy()
        {
            Bishop copy = new Bishop(Color);
            copy.HasMoved = HasMoved; // Übernimmt den Bewegungsstatus.
            return copy;
        }

        // Gibt alle möglichen Züge für den Läufer von der gegebenen Position auf dem Brett zurück.
        // Verwendet die allgemeine Logik aus der Piece-Basisklasse für Bewegungen in Richtungen.
        public override IEnumerable<Move> GetMoves(Position from, Board board)
        {
            // Wählt für jede mögliche Zielposition einen NormalMove aus.
            return MovePositionsInDirs(from, board, dirs).Select(to => new NormalMove(from, to));
        }
    }
}