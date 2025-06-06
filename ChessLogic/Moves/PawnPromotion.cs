using ChessLogic.Utilities;
using System;

namespace ChessLogic
{
    // Repräsentiert einen Bauernumwandlungszug.
    public class PawnPromotion : Move
    {
        public override MoveType Type => MoveType.PawnPromotion;    // Typ des Zugs.
        public override Position FromPos { get; }                   // Startposition des Bauern.
        public override Position ToPos { get; }                     // Zielposition auf der letzten Reihe.
        public PieceType PromotionTo { get; }                       // Der Figurentyp, zu dem umgewandelt wird.

        public PawnPromotion(Position from, Position to, PieceType newType)
        {
            FromPos = from;
            ToPos = to;
            PromotionTo = newType;
        }

        // Erstellt die neue Figur basierend auf dem gewählten Typ.
        private Piece CreatePromotionPiece(Player color)
        {
            return PromotionTo switch
            {
                PieceType.Knight => new Knight(color),
                PieceType.Bishop => new Bishop(color),
                PieceType.Rook => new Rook(color),
                _ => new Queen(color) // Standard und Fallback ist die Dame.
            };
        }

        // Führt die Umwandlung aus, indem der Bauer durch die neue Figur ersetzt wird.
        public override bool Execute(Board board)
        {
            Piece? pawn = board[FromPos];
            if (pawn is not { Type: PieceType.Pawn })
            {
                throw new InvalidOperationException("Kein Bauer auf dem Startfeld für die Umwandlung.");
            }

            board[FromPos] = null;
            Piece promotionPiece = CreatePromotionPiece(pawn.Color);
            promotionPiece.HasMoved = true;
            board[ToPos] = promotionPiece;

            return true; // Ist ein Bauernzug.
        }
    }
}