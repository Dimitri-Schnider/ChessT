using ChessLogic.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace ChessLogic
{
    // Repräsentiert die Schachfigur Dame.
    public class Queen : Piece
    {
        public override PieceType Type => PieceType.Queen;
        public override Player Color { get; }

        // Alle 8 Richtungen, in die sich eine Dame bewegen kann.
        private static readonly Direction[] dirs =
        [
            Direction.North,
            Direction.South,
            Direction.East,
            Direction.West,
            Direction.NorthWest,
            Direction.NorthEast,
            Direction.SouthWest,
            Direction.SouthEast
        ];

        public Queen(Player color)
        {
            Color = color;
        }

        // Erstellt eine tiefe Kopie des Damen-Objekts.
        public override Piece Copy()
        {
            return new Queen(Color) { HasMoved = HasMoved };
        }

        // Gibt alle möglichen Züge für die Dame zurück.
        public override IEnumerable<Move> GetMoves(Position from, Board board)
        {
            return MovePositionsInDirs(from, board, dirs).Select(to => new NormalMove(from, to));
        }
    }
}