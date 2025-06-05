using ChessLogic.Utilities;
using System;

namespace ChessLogic
{
    // Repräsentiert einen Bauernumwandlungszug.
    public class PawnPromotion : Move
    {
        // Typ des Zugs ist immer PawnPromotion.
        public override MoveType Type => MoveType.PawnPromotion;
        // Startposition des Bauern.
        public override Position FromPos { get; }
        // Zielposition des Bauern (das Umwandlungsfeld auf der letzten Reihe).
        public override Position ToPos { get; }
        // Der Figurentyp, zu dem der Bauer umgewandelt wird.
        public PieceType PromotionTo { get; }

        // Konstruktor für einen Bauernumwandlungszug.
        public PawnPromotion(Position from, Position to, PieceType newType)
        {
            FromPos = from;
            ToPos = to;
            PromotionTo = newType;
        }

        // Erstellt die neue Figur basierend auf dem gewählten PromotionTo Typ und der Farbe des Bauern.
        private Piece CreatePromotionPiece(Player color)
        {
            return PromotionTo switch
            {
                PieceType.Knight => new Knight(color),
                PieceType.Bishop => new Bishop(color),
                PieceType.Rook => new Rook(color),
                PieceType.Queen => new Queen(color), // Standard und häufigste Wahl.
                _ => new Queen(color) // Fallback, sollte nicht erreicht werden bei validem PromotionTo.
            };
        }

        // Führt die Bauernumwandlung auf dem Brett aus.
        // Ersetzt den Bauern auf dem Zielfeld durch die neue Figur.
        // Gibt true zurück, da es ein Bauernzug ist.
        public override bool Execute(Board board)
        {
            Piece? pawn = board[FromPos];
            if (pawn == null || pawn.Type != PieceType.Pawn)
            {
                throw new InvalidOperationException("Kein Bauer auf dem Startfeld für die Umwandlung.");
            }

            board[FromPos] = null; // Entfernt den Bauern vom Startfeld.
            Piece promotionPiece = CreatePromotionPiece(pawn.Color); // Erstellt die neue Figur.
            promotionPiece.HasMoved = true; // Die neue Figur gilt als bewegt.
            board[ToPos] = promotionPiece; // Setzt die neue Figur auf das Umwandlungsfeld.
            return true;
        }
    }
}