using ChessLogic.Utilities;
using System;

namespace ChessLogic.Moves
{
    // Repräsentiert einen Positionstausch zweier eigener Figuren.
    public class PositionSwapMove : Move
    {
        // Typ des Zugs.
        public override MoveType Type => MoveType.PositionSwap;
        // Position der ersten Figur.
        public override Position FromPos { get; }
        // Position der zweiten Figur.
        public override Position ToPos { get; }

        // Konstruktor für Positionstausch.
        public PositionSwapMove(Position piece1Pos, Position piece2Pos)
        {
            FromPos = piece1Pos;
            ToPos = piece2Pos;
        }

        // Führt den Tausch auf dem Brett aus.
        public override bool Execute(Board board)
        {
            Piece? piece1 = board[FromPos];
            Piece? piece2 = board[ToPos];

            if (piece1 == null || piece2 == null)
            {
                throw new InvalidOperationException("Für PositionSwap müssen auf beiden Feldern Figuren stehen.");
            }

            board[ToPos] = piece1;
            board[FromPos] = piece2;

            piece1.HasMoved = true;
            piece2.HasMoved = true;

            return false; // Kein direkter Bauern- oder Schlagzug.
        }

        // Grundlegende Legalitätsprüfung (Selbst-Schach).
        public override bool IsLegal(Board board)
        {
            Piece? piece1 = board[FromPos];
            Piece? piece2 = board[ToPos];

            if (piece1 == null || piece2 == null) return false;
            if (piece1.Color != piece2.Color) return false;
            if (FromPos == ToPos) return false;

            Player player = piece1.Color;
            Board boardCopy = board.Copy();
            Piece? p1Copy = boardCopy[FromPos];
            Piece? p2Copy = boardCopy[ToPos];

            if (p1Copy != null && p2Copy != null)
            {
                boardCopy[ToPos] = p1Copy;
                boardCopy[FromPos] = p2Copy;
            }
            else
            {
                return false;
            }
            return !boardCopy.IsInCheck(player);
        }
    }
}