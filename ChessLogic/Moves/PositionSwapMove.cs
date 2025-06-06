using ChessLogic.Utilities;
using System;

namespace ChessLogic.Moves
{
    // Repräsentiert einen Spezialzug, bei dem zwei eigene Figuren ihre Positionen tauschen.
    public class PositionSwapMove : Move
    {
        public override MoveType Type => MoveType.PositionSwap; // Typ des Zugs.
        public override Position FromPos { get; }               // Position der ersten Figur.
        public override Position ToPos { get; }                 // Position der zweiten Figur.

        public PositionSwapMove(Position piece1Pos, Position piece2Pos)
        {
            FromPos = piece1Pos;
            ToPos = piece2Pos;
        }

        // Führt den Positionstausch auf dem Brett aus.
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

            return false; // Kein Bauern- oder Schlagzug.
        }

        // Prüft die Legalität des Tauschs (gleiche Farbe, kein Selbstschach).
        public override bool IsLegal(Board board)
        {
            Piece? piece1 = board[FromPos];
            Piece? piece2 = board[ToPos];

            if (piece1 == null || piece2 == null || piece1.Color != piece2.Color || FromPos == ToPos)
            {
                return false;
            }

            // Prüft auf Selbstschach nach dem Tausch.
            return base.IsLegal(board);
        }
    }
}