using ChessLogic.Utilities;
using System.Collections.Generic;
using System.Linq; 

namespace ChessLogic
{
    // Abstrakte Basisklasse für alle Schachfiguren.
    public abstract class Piece
    {
        // Definiert den Typ der Figur (Bauer, Springer, Dame, etc.).
        public abstract PieceType Type { get; }
        // Definiert die Farbe der Figur (Weiss oder Schwarz).
        public abstract Player Color { get; }
        // Gibt an, ob die Figur im Laufe des Spiels bereits bewegt wurde.
        // Wichtig für Rochade (König, Turm) und Bauern-Doppelschritt.
        public bool HasMoved { get; set; } = false; // Standardwert ist false.

        // Erstellt eine tiefe Kopie der Figur. Jede abgeleitete Klasse muss dies implementieren.
        public abstract Piece Copy();

        // Gibt alle möglichen Züge zurück, die diese Figur von der gegebenen Position 'from'
        // auf dem Brett 'board' ausführen kann. Beinhaltet keine Legalitätsprüfung bezüglich Selbstschach.
        public abstract IEnumerable<Move> GetMoves(Position from, Board board);

        // Generiert alle möglichen Zielpositionen in einer einzelnen, gegebenen Richtung.
        // Die Bewegung stoppt, wenn das Brettende erreicht wird, eine eigene Figur im Weg steht
        // oder eine gegnerische Figur geschlagen werden kann (danach kann nicht weitergezogen werden).
        protected IEnumerable<Position> MovePositionsInDir(Position from, Board board, Direction dir)
        {
            for (Position pos = from + dir; Board.IsInside(pos); pos += dir)
            {
                // Wenn das Feld leer ist, ist es ein mögliches Ziel.
                if (board.IsEmpty(pos))
                {
                    yield return pos;
                    continue; // Suche weiter in diese Richtung.
                }

                Piece? piece = board[pos];
                // Wenn das Feld von einer gegnerischen Figur besetzt ist, kann diese geschlagen werden.
                if (piece?.Color != Color)
                {
                    yield return pos;
                }
                // Eigene Figur oder geschlagene gegnerische Figur blockiert weiteren Weg.
                yield break;
            }
        }

        // Generiert alle möglichen Zielpositionen in einem Array von Richtungen.
        // Nützlich für Figuren wie Turm, Läufer, Dame.
        protected IEnumerable<Position> MovePositionsInDirs(Position from, Board board, Direction[] dirs)
        {
            // Kombiniert die Ergebnisse von MovePositionsInDir für jede angegebene Richtung.
            return dirs.SelectMany(dir => MovePositionsInDir(from, board, dir));
        }

        // Prüft, ob diese Figur von ihrer aktuellen Position 'from' aus den gegnerischen König (theoretisch) schlagen könnte.
        // Wird verwendet, um Schachsituationen zu erkennen.
        // Die Standardimplementierung prüft alle generierten Züge der Figur.
        public virtual bool CanCaptureOpponentKing(Position from, Board board)
        {
            // Prüft, ob einer der möglichen Züge auf ein Feld zielt, das vom gegnerischen König besetzt ist.
            return GetMoves(from, board).Any(move =>
            {
                Piece? pieceOnToPos = board[move.ToPos];
                return pieceOnToPos != null && pieceOnToPos.Type == PieceType.King;
            });
        }
    }
}