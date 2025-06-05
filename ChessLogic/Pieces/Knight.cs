using ChessLogic.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace ChessLogic
{
    // Repräsentiert die Schachfigur Springer.
    public class Knight : Piece
    {
        // Definiert den Typ der Figur als Springer.
        public override PieceType Type => PieceType.Knight;
        // Definiert die Farbe des Springers (Weiss oder Schwarz).
        public override Player Color { get; }

        // Konstruktor für einen Springer.
        public Knight(Player color)
        {
            Color = color;
        }

        // Erstellt eine tiefe Kopie des Springer-Objekts.
        public override Piece Copy()
        {
            Knight copy = new Knight(Color);
            copy.HasMoved = HasMoved; // Übernimmt den Bewegungsstatus (obwohl für Springer irrelevant).
            return copy;
        }

        // Generiert alle acht potenziellen L-förmigen Sprungziele eines Springers von einer gegebenen Position.
        private static IEnumerable<Position> PotentialToPositions(Position from)
        {
            // Iteriert über vertikale und horizontale Komponenten der L-Bewegung.
            foreach (Direction vDir in new Direction[] { Direction.North, Direction.South }) // Zwei Schritte vertikal oder horizontal.
            {
                foreach (Direction hDir in new Direction[] { Direction.West, Direction.East }) // Ein Schritt in die orthogonale Richtung.
                {
                    yield return from + 2 * vDir + hDir; // Zwei vertikal, einer horizontal.
                    yield return from + 2 * hDir + vDir; // Zwei horizontal, einer vertikal.
                }
            }
        }

        // Filtert die potenziellen Sprungziele:
        // - Muss innerhalb des Bretts liegen.
        // - Darf nicht von einer eigenen Figur besetzt sein.
        private IEnumerable<Position> MovePositions(Position from, Board board)
        {
            return PotentialToPositions(from)
                .Where(pos => Board.IsInside(pos) && (board.IsEmpty(pos) || board[pos]?.Color != Color));
        }

        // Gibt alle möglichen Züge für den Springer von der gegebenen Position auf dem Brett zurück.
        public override IEnumerable<Move> GetMoves(Position from, Board board)
        {
            // Wandelt jede gültige Zielposition in einen NormalMove um.
            return MovePositions(from, board).Select(to => new NormalMove(from, to));
        }
    }
}