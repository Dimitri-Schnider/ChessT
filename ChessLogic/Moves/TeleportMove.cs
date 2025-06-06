using ChessLogic.Utilities;
using System;

namespace ChessLogic.Moves
{
    // Repräsentiert einen Spezialzug, bei dem eine Figur zu einem beliebigen leeren Feld teleportiert wird.
    public class TeleportMove : Move
    {
        public override MoveType Type => MoveType.Teleport; // Typ des Zugs.
        public override Position FromPos { get; }           // Startposition der Figur.
        public override Position ToPos { get; }             // Zielposition.

        public TeleportMove(Position from, Position to)
        {
            FromPos = from;
            ToPos = to;
        }

        // Führt den Teleport-Zug aus.
        public override bool Execute(Board board)
        {
            Piece? piece = board[FromPos];
            if (piece == null)
            {
                throw new InvalidOperationException("Keine Figur auf der Startposition für den Teleport.");
            }

            board[ToPos] = piece;
            board[FromPos] = null;
            piece.HasMoved = true;

            return false; // Kein Bauern- oder Schlagzug.
        }

        // Prüft die Legalität des Zugs (Zielfeld muss leer sein, kein Selbstschach).
        public override bool IsLegal(Board board)
        {
            if (board[FromPos] == null || !board.IsEmpty(ToPos))
            {
                return false;
            }

            return base.IsLegal(board);
        }
    }
}