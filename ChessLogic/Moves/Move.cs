using ChessLogic.Utilities;

namespace ChessLogic
{
    // Abstrakte Basisklasse für alle Arten von Schachzügen.
    public abstract class Move
    {
        public abstract MoveType Type { get; }      // Definiert den Typ des Zugs (z.B. Normal, Castle).
        public abstract Position FromPos { get; }   // Definiert die Startposition der Figur.
        public abstract Position ToPos { get; }     // Definiert die Zielposition der Figur.
        public abstract bool Execute(Board board);  // Führt den Zug auf dem Brett aus. Gibt true zurück, wenn es ein Schlag- oder Bauernzug war.

        // Prüft, ob der Zug legal ist, insbesondere ob der eigene König danach im Schach stehen würde.
        public virtual bool IsLegal(Board board)
        {
            if (board.IsEmpty(FromPos))
            {
                return false;
            }

            Player player = board[FromPos]!.Color;
            Board boardCopy = board.Copy();
            Execute(boardCopy);
            return !boardCopy.IsInCheck(player);
        }
    }
}