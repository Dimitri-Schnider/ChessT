using ChessLogic.Utilities;

namespace ChessLogic
{
    // Abstrakte Basisklasse für alle Schachzüge.
    public abstract class Move
    {
        // Typ des Zugs.
        public abstract MoveType Type { get; }
        // Startposition.
        public abstract Position FromPos { get; }
        // Zielposition.
        public abstract Position ToPos { get; }

        // Führt den Zug aus. Gibt true zurück, wenn Schlag- oder Bauernzug.
        public abstract bool Execute(Board board);

        // Prüft die Legalität des Zugs (Standardimplementierung prüft Selbst-Schach).
        public virtual bool IsLegal(Board board)
        {
            if (board.IsEmpty(FromPos))
                return false;
            Player player = board[FromPos]!.Color;
            Board boardCopy = board.Copy();
            Execute(boardCopy);
            return !boardCopy.IsInCheck(player);
        }
    }
}