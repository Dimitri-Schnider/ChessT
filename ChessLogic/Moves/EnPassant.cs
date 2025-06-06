using ChessLogic.Utilities;

namespace ChessLogic
{
    // Repräsentiert einen En-Passant-Schlagzug.
    public class EnPassant : Move
    {
        public override MoveType Type => MoveType.EnPassant;    // Typ des Zugs.
        public override Position FromPos { get; }               // Startposition des schlagenden Bauern.
        public override Position ToPos { get; }                 // Zielposition des schlagenden Bauern.

        // Die Position des Bauern, der En Passant geschlagen wird.
        private readonly Position capturePos;

        public EnPassant(Position from, Position to)
        {
            FromPos = from;
            ToPos = to;
            capturePos = new Position(from.Row, to.Column);
        }

        // Führt den Schlag aus, indem der schlagende Bauer bewegt und der geschlagene entfernt wird.
        public override bool Execute(Board board)
        {
            new NormalMove(FromPos, ToPos).Execute(board);
            board[capturePos] = null;
            return true; // Ist ein Schlagzug.
        }
    }
}