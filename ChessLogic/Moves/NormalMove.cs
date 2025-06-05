using ChessLogic.Utilities;
using System;

namespace ChessLogic
{
    // Repräsentiert einen normalen Figurenschritt (kein Spezialzug wie Rochade oder En Passant).
    public class NormalMove : Move
    {
        // Typ des Zugs ist immer Normal.
        public override MoveType Type => MoveType.Normal;
        // Startposition der Figur.
        public override Position FromPos { get; }
        // Zielposition der Figur.
        public override Position ToPos { get; }

        // Konstruktor für einen normalen Zug.
        public NormalMove(Position from, Position to)
        {
            FromPos = from;
            ToPos = to;
        }

        // Führt den normalen Zug auf dem Brett aus.
        // Bewegt die Figur von FromPos nach ToPos.
        // Gibt true zurück, wenn es ein Schlagzug oder ein Bauernzug war.
        public override bool Execute(Board board)
        {
            Piece? piece = board[FromPos];
            if (piece == null)
            {
                throw new InvalidOperationException($"NormalMove.Execute: Kein Piece vorhanden bei {FromPos}.");
            }

            bool capture = !board.IsEmpty(ToPos); // Prüft, ob eine Figur geschlagen wird.
            board[ToPos] = piece; // Setzt die Figur auf das Zielfeld.
            board[FromPos] = null; // Leert das Startfeld.
            piece.HasMoved = true; // Markiert die Figur als bewegt.

            // Gibt true zurück, wenn es ein Schlagzug ODER ein Bauernzug war.
            return capture || piece.Type == PieceType.Pawn;
        }
    }
}