using ChessLogic.Utilities;

namespace ChessLogic
{
    // Repräsentiert einen Bauern-Doppelschritt von der Startposition.
    public class DoublePawn : Move
    {
        public override MoveType Type => MoveType.DoublePawn;   // Typ des Zugs.
        public override Position FromPos { get; }               // Startposition des Bauern.
        public override Position ToPos { get; }                 // Zielposition (zwei Felder vorwärts).

        // Die Position, die vom Bauern übersprungen wird (relevant für En Passant).
        private readonly Position skippedPos;

        public DoublePawn(Position from, Position to)
        {
            FromPos = from;
            ToPos = to;
            skippedPos = new Position((from.Row + to.Row) / 2, from.Column);
        }

        // Führt den Doppelschritt aus und setzt die En-Passant-Möglichkeit.
        public override bool Execute(Board board)
        {
            Player player = board[FromPos]!.Color;
            board.SetPawnSkipPosition(player, skippedPos);
            new NormalMove(FromPos, ToPos).Execute(board);
            return true; // Ist ein Bauernzug.
        }
    }
}