using ChessLogic.Utilities;
using System;

namespace ChessLogic.Moves
{
    // Repräsentiert einen speziellen Zug, bei dem eine Figur zu einem beliebigen leeren Feld teleportiert wird.
    public class TeleportMove : Move
    {
        // Typ des Zugs ist immer Teleport.
        public override MoveType Type => MoveType.Teleport;
        // Startposition der Figur, die teleportiert wird.
        public override Position FromPos { get; }
        // Zielposition, zu der die Figur teleportiert wird.
        public override Position ToPos { get; }

        // Konstruktor für einen Teleport-Zug.
        public TeleportMove(Position from, Position to)
        {
            FromPos = from;
            ToPos = to;
        }

        // Führt den Teleport-Zug auf dem Brett aus.
        // Gibt false zurück, da dies kein Standard-Schlag- oder Bauernzug ist.
        public override bool Execute(Board board)
        {
            Piece? piece = board[FromPos];
            if (piece == null)
            {
                throw new InvalidOperationException("Keine Figur auf der Startposition für den Teleport.");
            }

            board[ToPos] = piece; // Setzt die Figur auf das Zielfeld.
            board[FromPos] = null; // Leert das Startfeld.
            piece.HasMoved = true; // Die Figur gilt nach dem Teleport als bewegt.

            return false;
        }

        // Prüft die Legalität des Teleport-Zugs.
        // Stellt sicher, dass eine Figur auf FromPos steht, das Zielfeld ToPos leer ist
        // und der eigene König nicht ins Schach gestellt wird.
        public override bool IsLegal(Board board)
        {
            Piece? piece = board[FromPos];
            // Es muss eine Figur auf der Startposition vorhanden sein.
            if (piece == null)
            {
                return false;
            }

            // Das Zielfeld muss leer sein.
            if (!board.IsEmpty(ToPos))
            {
                return false;
            }

            Player player = piece.Color;
            Board boardCopy = board.Copy();
            Piece? pieceInCopy = boardCopy[FromPos];

            // Führe den Teleport auf der Kopie durch.
            if (pieceInCopy != null)
            {
                boardCopy[ToPos] = pieceInCopy;
                boardCopy[FromPos] = null;
            }
            else
            {
                // Sollte nicht eintreten, da oben bereits auf null geprüft.
                return false;
            }
            // Der Zug ist legal, wenn der eigene König nach dem Teleport nicht im Schach steht.
            return !boardCopy.IsInCheck(player);
        }
    }
}