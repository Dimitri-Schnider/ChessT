using ChessLogic.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace ChessLogic
{
    // Repräsentiert die Schachfigur Turm.
    public class Rook : Piece
    {
        public override PieceType Type => PieceType.Rook;
        public override Player Color { get; }

        // Die horizontalen und vertikalen Bewegungsrichtungen für einen Turm.
        private static readonly Direction[] dirs =
        [
            Direction.North,
            Direction.South,
            Direction.East,
            Direction.West
        ];

        public Rook(Player color)
        {
            Color = color;
        }

        // Erstellt eine tiefe Kopie des Turm-Objekts.
        public override Piece Copy()
        {
            return new Rook(Color) { HasMoved = HasMoved };
        }

        // Gibt alle möglichen Züge für den Turm zurück.
        public override IEnumerable<Move> GetMoves(Position from, Board board)
        {
            return MovePositionsInDirs(from, board, dirs).Select(to => new NormalMove(from, to));
        }
    }
}