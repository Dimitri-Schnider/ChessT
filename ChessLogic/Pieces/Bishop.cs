using ChessLogic.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace ChessLogic
{
    // Repräsentiert die Schachfigur Läufer.
    public class Bishop : Piece
    {
        public override PieceType Type => PieceType.Bishop;
        public override Player Color { get; }

        // Die möglichen diagonalen Bewegungsrichtungen für einen Läufer.
        private static readonly Direction[] dirs = new Direction[]
        {
            Direction.NorthWest,
            Direction.NorthEast,
            Direction.SouthWest,
            Direction.SouthEast
        };

        public Bishop(Player color)
        {
            Color = color;
        }

        // Erstellt eine tiefe Kopie des Läufer-Objekts.
        public override Piece Copy()
        {
            return new Bishop(Color) { HasMoved = HasMoved };
        }

        // Gibt alle möglichen Züge für den Läufer zurück.
        public override IEnumerable<Move> GetMoves(Position from, Board board)
        {
            return MovePositionsInDirs(from, board, dirs).Select(to => new NormalMove(from, to));
        }
    }
}