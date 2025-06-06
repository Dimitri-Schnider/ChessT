using ChessLogic.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessLogic
{
    // Repräsentiert die Schachfigur Bauer.
    public class Pawn : Piece
    {
        public override PieceType Type => PieceType.Pawn;
        public override Player Color { get; }

        // Die Vorwärtsrichtung des Bauern, abhängig von seiner Farbe.
        private readonly Direction forward;

        public Pawn(Player color)
        {
            Color = color;
            if (color == Player.White) forward = Direction.North;
            else if (color == Player.Black) forward = Direction.South;
            else throw new ArgumentException("Ein Bauer muss eine gültige Spielerfarbe haben.", nameof(color));
        }

        // Erstellt eine tiefe Kopie des Bauern-Objekts.
        public override Piece Copy()
        {
            return new Pawn(Color) { HasMoved = HasMoved };
        }

        // Prüft, ob der Bauer auf eine bestimmte Position ziehen kann.
        private static bool CanMoveTo(Position pos, Board board)
        {
            return Board.IsInside(pos) && board.IsEmpty(pos);
        }

        // Prüft, ob der Bauer auf einer bestimmten Position eine gegnerische Figur schlagen kann.
        private bool CanCaptureAt(Position pos, Board board)
        {
            if (!Board.IsInside(pos) || board.IsEmpty(pos)) return false;
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

        // Generiert die Vorwärtszüge eines Bauern (Einzelschritt, Doppelschritt, Umwandlung).
        private IEnumerable<Move> ForwardMoves(Position from, Board board)
        {
            Position oneMovePos = from + forward;
            if (CanMoveTo(oneMovePos, board))
            {
                if (oneMovePos.Row is 0 or 7)
                {
                    foreach (Move promMove in PromotionMoves(from, oneMovePos)) yield return promMove;
                }
                else
                {
                    yield return new NormalMove(from, oneMovePos);
                }

                Position twoMovesPos = oneMovePos + forward;
                if (!HasMoved && CanMoveTo(twoMovesPos, board))
                {
                    yield return new DoublePawn(from, twoMovesPos);
                }
            }
        }

        // Generiert die diagonalen Schlagzüge (normal und en passant).
        private IEnumerable<Move> DiagonalMoves(Position from, Board board)
        {
            foreach (Direction dir in new Direction[] { Direction.West, Direction.East })
            {
                Position to = from + forward + dir;

                if (to == board.GetPawnSkipPosition(Color.Opponent()))
                {
                    yield return new EnPassant(from, to);
                }
                else if (CanCaptureAt(to, board))
                {
                    if (to.Row is 0 or 7)
                    {
                        foreach (Move promMove in PromotionMoves(from, to)) yield return promMove;
                    }
                    else
                    {
                        yield return new NormalMove(from, to);
                    }
                }
            }
        }

        // Gibt alle möglichen Züge für den Bauern zurück.
        public override IEnumerable<Move> GetMoves(Position from, Board board)
        {
            return ForwardMoves(from, board).Concat(DiagonalMoves(from, board));
        }

        // Prüft, ob der Bauer von seiner Position aus den gegnerischen König bedroht.
        public override bool CanCaptureOpponentKing(Position from, Board board)
        {
            return DiagonalMoves(from, board).Any(move => board[move.ToPos] is { Type: PieceType.King });
        }
    }
}