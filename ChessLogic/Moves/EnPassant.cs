using ChessLogic.Utilities;

namespace ChessLogic
{
    // Repräsentiert einen En-Passant-Schlagzug.
    public class EnPassant : Move
    {
        // Typ des Zugs ist immer EnPassant.
        public override MoveType Type => MoveType.EnPassant;
        // Startposition des schlagenden Bauern.
        public override Position FromPos { get; }
        // Zielposition des schlagenden Bauern (das Feld hinter dem geschlagenen Bauern).
        public override Position ToPos { get; }

        // Die Position des Bauern, der En Passant geschlagen wird.
        private readonly Position capturePos;

        // Konstruktor für einen En-Passant-Schlag.
        public EnPassant(Position from, Position to)
        {
            FromPos = from;
            ToPos = to;
            // Der geschlagene Bauer steht auf derselben Reihe wie der schlagende Bauer vor seinem Zug,
            // aber auf der Spalte des Zielfeldes.
            capturePos = new Position(from.Row, to.Column);
        }

        // Führt den En-Passant-Schlag auf dem Brett aus.
        // Bewegt den schlagenden Bauern und entfernt den geschlagenen Bauern.
        // Gibt true zurück, da es ein Schlagzug ist (relevant für 50-Züge-Regel).
        public override bool Execute(Board board)
        {
            new NormalMove(FromPos, ToPos).Execute(board); // Bewegt den schlagenden Bauern.
            board[capturePos] = null; // Entfernt den geschlagenen Bauern vom Brett.
            return true;
        }
    }
}