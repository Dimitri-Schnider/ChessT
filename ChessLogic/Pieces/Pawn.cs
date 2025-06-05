using ChessLogic.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessLogic
{
    // Repräsentiert die Schachfigur Bauer.
    public class Pawn : Piece
    {
        // Definiert den Typ der Figur als Bauer.
        public override PieceType Type => PieceType.Pawn;
        // Definiert die Farbe des Bauern (Weiss oder Schwarz).
        public override Player Color { get; }

        // Die Vorwärtsrichtung des Bauern, abhängig von seiner Farbe.
        // Weisse Bauern ziehen nach Norden (kleinere Zeilenindizes), schwarze nach Süden (grössere Zeilenindizes).
        private readonly Direction forward;

        // Konstruktor für einen Bauern.
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
                // Dies sollte nie passieren, wenn Player nur White oder Black ist.
                throw new ArgumentException("Ein Bauer muss eine gültige Spielerfarbe haben.", nameof(color));
            }
        }

        // Erstellt eine tiefe Kopie des Bauern-Objekts.
        public override Piece Copy()
        {
            Pawn copy = new Pawn(Color);
            copy.HasMoved = HasMoved; // Übernimmt den Bewegungsstatus.
            return copy;
        }

        // Prüft, ob der Bauer auf eine bestimmte Position ziehen kann (Feld muss leer sein).
        private static bool CanMoveTo(Position pos, Board board)
        {
            return Board.IsInside(pos) && board.IsEmpty(pos);
        }

        // Prüft, ob der Bauer auf einer bestimmten Position eine gegnerische Figur schlagen kann.
        private bool CanCaptureAt(Position pos, Board board)
        {
            // Feld muss auf dem Brett und nicht leer sein.
            if (!Board.IsInside(pos) || board.IsEmpty(pos))
            {
                return false;
            }
            // Die Figur auf dem Feld muss die gegnerische Farbe haben.
            return board[pos]?.Color != Color;
        }

        // Generiert alle möglichen Umwandlungszüge für einen Bauern, der die letzte Reihe erreicht.
        private static IEnumerable<Move> PromotionMoves(Position from, Position to)
        {
            yield return new PawnPromotion(from, to, PieceType.Knight);
            yield return new PawnPromotion(from, to, PieceType.Bishop);
            yield return new PawnPromotion(from, to, PieceType.Rook);
            yield return new PawnPromotion(from, to, PieceType.Queen); // Dame ist die häufigste Wahl.
        }

        // Generiert die Vorwärtszüge eines Bauern.
        // Beinhaltet Einzelschritt, Doppelschritt (falls möglich) und Umwandlungszüge.
        private IEnumerable<Move> ForwardMoves(Position from, Board board)
        {
            Position oneMovePos = from + forward; // Position ein Feld vorwärts.

            if (CanMoveTo(oneMovePos, board)) // Wenn das Feld direkt vor dem Bauern frei ist.
            {
                // Prüft, ob die letzte Reihe erreicht wurde (Umwandlung).
                if (oneMovePos.Row == 0 || oneMovePos.Row == 7)
                {
                    foreach (Move promMove in PromotionMoves(from, oneMovePos))
                    {
                        yield return promMove;
                    }
                }
                else // Normaler Einzelschritt.
                {
                    yield return new NormalMove(from, oneMovePos);
                }

                Position twoMovesPos = oneMovePos + forward; // Position zwei Felder vorwärts.
                // Wenn der Bauer noch nicht gezogen hat und das Feld zwei Schritte vor ihm frei ist.
                if (!HasMoved && CanMoveTo(twoMovesPos, board))
                {
                    yield return new DoublePawn(from, twoMovesPos); // Doppelschritt.
                }
            }
        }

        // Generiert die diagonalen Schlagzüge eines Bauern.
        // Beinhaltet normale Schlagzüge und En-Passant-Schläge.
        private IEnumerable<Move> DiagonalMoves(Position from, Board board)
        {
            // Iteriert über die beiden diagonalen Schlagrichtungen (West und Ost relativ zur Vorwärtsrichtung).
            foreach (Direction dir in new Direction[] { Direction.West, Direction.East })
            {
                Position to = from + forward + dir; // Potenzielles Schlagfeld.

                // Prüft auf En-Passant-Möglichkeit.
                if (Board.IsInside(to) && to == board.GetPawnSkipPosition(Color.Opponent()))
                {
                    yield return new EnPassant(from, to);
                }
                // Prüft auf normalen diagonalen Schlagzug.
                else if (CanCaptureAt(to, board))
                {
                    // Prüft, ob der Schlagzug zu einer Umwandlung führt.
                    if (to.Row == 0 || to.Row == 7)
                    {
                        foreach (Move promMove in PromotionMoves(from, to))
                        {
                            yield return promMove;
                        }
                    }
                    else // Normaler Schlagzug ohne Umwandlung.
                    {
                        yield return new NormalMove(from, to);
                    }
                }
            }
        }

        // Gibt alle möglichen Züge für den Bauern zurück (Kombination aus Vorwärts- und Diagonalzügen).
        public override IEnumerable<Move> GetMoves(Position from, Board board)
        {
            return ForwardMoves(from, board).Concat(DiagonalMoves(from, board));
        }

        // Prüft, ob der Bauer von seiner aktuellen Position aus den gegnerischen König (theoretisch) schlagen könnte.
        public override bool CanCaptureOpponentKing(Position from, Board board)
        {
            // Prüft, ob einer der diagonalen Schlagzüge auf das Feld des gegnerischen Königs zielt.
            return DiagonalMoves(from, board).Any(move =>
            {
                Piece? pieceOnToPos = board[move.ToPos];
                return pieceOnToPos != null && pieceOnToPos.Type == PieceType.King;
            });
        }
    }
}