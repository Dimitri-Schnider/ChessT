using ChessLogic.Utilities;
using System;

namespace ChessLogic
{
    // Repräsentiert einen normalen Figurenschritt (kein Spezialzug).
    public class NormalMove : Move
    {
        public override MoveType Type => MoveType.Normal;   // Typ des Zugs.
        public override Position FromPos { get; }           // Startposition der Figur.
        public override Position ToPos { get; }             // Zielposition der Figur.

        public NormalMove(Position from, Position to)
        {
            FromPos = from;
            ToPos = to;
        }

        // Führt den Zug aus und gibt zurück, ob es ein Schlag- oder Bauernzug war.
        public override bool Execute(Board board)
        {
            Piece? piece = board[FromPos];
            if (piece == null)
            {
                throw new InvalidOperationException($"NormalMove.Execute: Kein Piece vorhanden bei {FromPos}.");
            }

            bool capture = !board.IsEmpty(ToPos);
            board[ToPos] = piece;
            board[FromPos] = null;
            piece.HasMoved = true;

            return capture || piece.Type == PieceType.Pawn;
        }
    }
}