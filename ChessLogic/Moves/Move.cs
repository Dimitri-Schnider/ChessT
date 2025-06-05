using ChessLogic.Utilities;

namespace ChessLogic
{
    // Abstrakte Basisklasse für alle Arten von Schachzügen.
    public abstract class Move
    {
        // Definiert den Typ des Zugs (z.B. Normal, Castle, EnPassant).
        public abstract MoveType Type { get; }
        // Definiert die Startposition der Figur, die den Zug ausführt.
        public abstract Position FromPos { get; }
        // Definiert die Zielposition der Figur nach dem Zug.
        public abstract Position ToPos { get; }

        // Führt den Zug auf dem gegebenen Schachbrett aus.
        // Gibt true zurück, wenn der Zug ein Schlagzug oder ein Bauernzug war
        // (relevant für die 50-Züge-Regel).
        public abstract bool Execute(Board board);

        // Prüft, ob der Zug unter den aktuellen Brettbedingungen legal ist.
        // Die Standardimplementierung prüft, ob der eigene König nach dem Zug im Schach steht.
        public virtual bool IsLegal(Board board)
        {
            // Ein Zug von einem leeren Feld ist nie legal.
            if (board.IsEmpty(FromPos))
            {
                return false;
            }
            Player player = board[FromPos]!.Color; // Farbe des Spielers, der am Zug ist.
            Board boardCopy = board.Copy(); // Erstellt eine Kopie des Bretts, um den Zug zu testen.
            Execute(boardCopy); // Führt den Zug auf der Kopie aus.
            return !boardCopy.IsInCheck(player); // Der Zug ist legal, wenn der eigene König nicht im Schach steht.
        }
    }
}