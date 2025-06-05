using ChessLogic.Utilities;
using System;

namespace ChessLogic.Moves
{
    // Repräsentiert einen speziellen Zug, bei dem zwei eigene Figuren ihre Positionen tauschen.
    public class PositionSwapMove : Move
    {
        // Typ des Zugs ist immer PositionSwap.
        public override MoveType Type => MoveType.PositionSwap;
        // Position der ersten Figur, die am Tausch beteiligt ist.
        public override Position FromPos { get; }
        // Position der zweiten Figur, die am Tausch beteiligt ist.
        public override Position ToPos { get; }

        // Konstruktor für einen Positionstausch-Zug.
        public PositionSwapMove(Position piece1Pos, Position piece2Pos)
        {
            FromPos = piece1Pos; // Dient als Position von Figur 1.
            ToPos = piece2Pos;   // Dient als Position von Figur 2.
        }

        // Führt den Positionstausch auf dem Brett aus.
        // Gibt false zurück, da dies kein Standard-Schlag- oder Bauernzug ist.
        public override bool Execute(Board board)
        {
            Piece? piece1 = board[FromPos];
            Piece? piece2 = board[ToPos];

            if (piece1 == null || piece2 == null)
            {
                throw new InvalidOperationException("Für PositionSwap müssen auf beiden Feldern Figuren stehen.");
            }

            // Tausche die Figuren auf dem Brett.
            board[ToPos] = piece1;
            board[FromPos] = piece2;

            // Beide Figuren gelten nach dem Tausch als bewegt.
            piece1.HasMoved = true;
            piece2.HasMoved = true;

            return false;
        }

        // Prüft die Legalität des Positionstauschs.
        // Stellt sicher, dass beide Figuren dem gleichen Spieler gehören,
        // die Positionen unterschiedlich sind und der eigene König nicht ins Schach gestellt wird.
        public override bool IsLegal(Board board)
        {
            Piece? piece1 = board[FromPos];
            Piece? piece2 = board[ToPos];

            // Beide Felder müssen besetzt sein.
            if (piece1 == null || piece2 == null)
            {
                return false;
            }
            // Beide Figuren müssen dieselbe Farbe haben.
            if (piece1.Color != piece2.Color)
            {
                return false;
            }
            // Die Positionen müssen unterschiedlich sein.
            if (FromPos == ToPos)
            {
                return false;
            }

            Player player = piece1.Color;
            Board boardCopy = board.Copy();
            Piece? p1Copy = boardCopy[FromPos];
            Piece? p2Copy = boardCopy[ToPos];

            // Führe den Tausch auf der Kopie durch.
            if (p1Copy != null && p2Copy != null)
            {
                boardCopy[ToPos] = p1Copy;
                boardCopy[FromPos] = p2Copy;
            }
            else
            {
                // Sollte nicht eintreten, da oben bereits auf null geprüft.
                return false;
            }
            // Der Zug ist legal, wenn der eigene König nach dem Tausch nicht im Schach steht.
            return !boardCopy.IsInCheck(player);
        }
    }
}