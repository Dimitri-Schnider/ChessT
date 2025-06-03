using ChessLogic.Utilities;
using System;

namespace ChessLogic
{
    // Repräsentiert einen normalen Figurenschritt.
    public class NormalMove : Move
    {
        // Typ des Zugs.
        public override MoveType Type => MoveType.Normal;
        // Startposition.
        public override Position FromPos { get; }
        // Zielposition.
        public override Position ToPos { get; }

        // Konstruktor für normalen Zug.
        public NormalMove(Position from, Position to)
        {
            FromPos = from;
            ToPos = to;
        }

        // Führt den normalen Zug aus.
        public override bool Execute(Board board)
        {
            if (board[FromPos] is not Piece piece)
                throw new InvalidOperationException($"NormalMove.Execute: Kein Piece vorhanden bei {FromPos}.");
            bool capture = !board.IsEmpty(ToPos);
            board[ToPos] = piece;
            board[FromPos] = null;
            piece.HasMoved = true;
            return capture || piece.Type == PieceType.Pawn; // Schlag- oder Bauernzug.
        }
    }
}