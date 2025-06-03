using ChessLogic.Utilities;
using System;
using System.Linq;

namespace ChessLogic
{
    // Repräsentiert die Schachfigur Bauer.
    public class Pawn : Piece
    {
        // Typ: Bauer.
        public override PieceType Type => PieceType.Pawn;
        // Farbe des Bauern.
        public override Player Color { get; }
        // Vorwärtsrichtung des Bauern, abhängig von der Farbe.
        private readonly Direction forward;

        // Konstruktor.
        public Pawn(Player color)
        {
            Color = color;
            if (color == Player.White)
            {
                forward = Direction.North;
            }
            else if (color == Player.Black)
            {
                forward = Direction.South;
            }
            else
            {
                throw new ArgumentException("Ein Bauer muss eine gültige Spielerfarbe haben.", nameof(color));
            }
        }

        // Erstellt eine Kopie des Bauern.
        public override Piece Copy()
        {
            Pawn copy = new Pawn(Color);
            copy.HasMoved = HasMoved;
            return copy;
        }

        // Prüft, ob ein Feld für einen Vorwärtszug frei ist.
        private static bool CanMoveTo(Position pos, Board board)
        {
            return Board.IsInside(pos) && board.IsEmpty(pos);
        }

        // Prüft, ob auf einem Feld eine gegnerische Figur geschlagen werden kann.
        private bool CanCaptureAt(Position pos, Board board)
        {
            if (!Board.IsInside(pos) || board.IsEmpty(pos))
            {
                return false;
            }
            return board[pos]?.Color != Color;
        }

        // Generiert alle möglichen Umwandlungszüge.
        private static IEnumerable<Move> PromotionMoves(Position from, Position to)
        {
            yield return new PawnPromotion(from, to, PieceType.Knight);
            yield return new PawnPromotion(from, to, PieceType.Bishop);
            yield return new PawnPromotion(from, to, PieceType.Rook);
            yield return new PawnPromotion(from, to, PieceType.Queen);
        }

        // Generiert Vorwärtszüge (Einzelschritt, Doppelschritt, Umwandlung).
        private IEnumerable<Move> ForwardMoves(Position from, Board board)
        {
            Position oneMovePos = from + forward;
            if (CanMoveTo(oneMovePos, board))
            {
                if (oneMovePos.Row == 0 || oneMovePos.Row == 7) // Umwandlungsreihe erreicht.
                {
                    foreach (Move promMove in PromotionMoves(from, oneMovePos)) { yield return promMove; }
                }
                else { yield return new NormalMove(from, oneMovePos); }

                Position twoMovesPos = oneMovePos + forward;
                if (!HasMoved && CanMoveTo(twoMovesPos, board)) // Doppelschritt von Startposition.
                {
                    yield return new DoublePawn(from, twoMovesPos);
                }
            }
        }

        // Generiert diagonale Schlagzüge (normal und En Passant).
        private IEnumerable<Move> DiagonalMoves(Position from, Board board)
        {
            foreach (Direction dir in new Direction[] { Direction.West, Direction.East })
            {
                Position to = from + forward + dir;
                if (Board.IsInside(to) && to == board.GetPawnSkipPosition(Color.Opponent())) // En Passant.
                {
                    yield return new EnPassant(from, to);
                }
                else if (CanCaptureAt(to, board)) // Normaler Schlagzug.
                {
                    if (to.Row == 0 || to.Row == 7) // Umwandlung nach Schlagzug.
                    {
                        foreach (Move promMove in PromotionMoves(from, to)) { yield return promMove; }
                    }
                    else { yield return new NormalMove(from, to); }
                }
            }
        }

        // Gibt alle legalen Züge des Bauern zurück.
        public override IEnumerable<Move> GetMoves(Position from, Board board)
        {
            return ForwardMoves(from, board).Concat(DiagonalMoves(from, board));
        }

        // Prüft, ob der Bauer einen gegnerischen König schlagen könnte (theoretisch).
        public override bool CanCaptureOpponentKing(Position from, Board board)
        {
            return DiagonalMoves(from, board).Any(move =>
            {
                Piece? pieceOnToPos = board[move.ToPos];
                return pieceOnToPos != null && pieceOnToPos.Type == PieceType.King;
            });
        }
    }
}