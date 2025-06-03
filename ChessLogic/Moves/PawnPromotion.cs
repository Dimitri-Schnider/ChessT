using ChessLogic.Utilities;
using System;

namespace ChessLogic
{
    // Repräsentiert eine Bauernumwandlung.
    public class PawnPromotion : Move
    {
        // Typ des Zugs.
        public override MoveType Type => MoveType.PawnPromotion;
        // Startposition des Bauern.
        public override Position FromPos { get; }
        // Zielposition (Umwandlungsfeld).
        public override Position ToPos { get; }
        // Figurentyp der Umwandlung.
        private readonly PieceType _promotionToType;
        public PieceType PromotionTo => _promotionToType;

        // Konstruktor für Bauernumwandlung.
        public PawnPromotion(Position from, Position to, PieceType newType)
        {
            FromPos = from;
            ToPos = to;
            _promotionToType = newType;
        }

        // Erstellt die neue Figur.
        private Piece CreatePromotionPiece(Player color)
        {
            return PromotionTo switch
            {
                PieceType.Knight => new Knight(color),
                PieceType.Bishop => new Bishop(color),
                PieceType.Rook => new Rook(color),
                _ => new Queen(color) // Standard: Dame.
            };
        }

        // Führt die Umwandlung aus.
        public override bool Execute(Board board)
        {
            Piece? pawn = board[FromPos];
            if (pawn == null || pawn.Type != PieceType.Pawn)
                throw new InvalidOperationException("Kein Bauer auf dem Startfeld für die Umwandlung.");
            board[FromPos] = null;
            Piece promotionPiece = CreatePromotionPiece(pawn.Color);
            promotionPiece.HasMoved = true;
            board[ToPos] = promotionPiece;
            return true; // Bauernzug, relevant für 50-Züge-Regel.
        }
    }
}