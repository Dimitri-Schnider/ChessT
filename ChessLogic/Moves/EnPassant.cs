using ChessLogic.Utilities;

namespace ChessLogic
{
    // Repräsentiert einen En-Passant-Schlag.
    public class EnPassant : Move
    {
        // Typ des Zugs.
        public override MoveType Type => MoveType.EnPassant;
        // Startposition des schlagenden Bauern.
        public override Position FromPos { get; }
        // Zielposition des schlagenden Bauern.
        public override Position ToPos { get; }
        // Position des geschlagenen Bauern.
        private readonly Position capturePos;

        // Konstruktor für En-Passant-Schlag.
        public EnPassant(Position from, Position to)
        {
            FromPos = from;
            ToPos = to;
            capturePos = new Position(from.Row, to.Column);
        }

        // Führt den En-Passant-Schlag aus.
        public override bool Execute(Board board)
        {
            new NormalMove(FromPos, ToPos).Execute(board);
            board[capturePos] = null; // Entfernt geschlagenen Bauern.
            return true; // Schlagzug, relevant für 50-Züge-Regel.
        }
    }
}