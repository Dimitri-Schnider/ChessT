using ChessLogic.Utilities;
using System;

namespace ChessLogic.Moves
{
    // Repräsentiert einen Teleport-Zug einer Figur.
    public class TeleportMove : Move
    {
        // Typ des Zugs.
        public override MoveType Type => MoveType.Teleport;
        // Startposition der Figur.
        public override Position FromPos { get; }
        // Zielposition der Figur.
        public override Position ToPos { get; }

        // Konstruktor für Teleport-Zug.
        public TeleportMove(Position from, Position to)
        {
            FromPos = from;
            ToPos = to;
        }

        // Führt den Teleport auf dem Brett aus.
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

            return false; // Kein direkter Bauern- oder Schlagzug.
        }

        // Grundlegende Legalitätsprüfung (Selbst-Schach, Zielfeld leer).
        public override bool IsLegal(Board board)
        {
            Piece? piece = board[FromPos];
            if (piece == null)
            {
                return false;
            }

            if (!board.IsEmpty(ToPos))
            {
                return false;
            }

            Player player = piece.Color;
            Board boardCopy = board.Copy();
            Piece? pieceInCopy = boardCopy[FromPos];
            if (pieceInCopy != null)
            {
                boardCopy[ToPos] = pieceInCopy;
                boardCopy[FromPos] = null;
            }
            else
            {
                return false;
            }
            return !boardCopy.IsInCheck(player);
        }
    }
}