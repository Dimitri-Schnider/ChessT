using ChessLogic.Utilities;

namespace ChessLogic
{
    // Repräsentiert einen Bauern-Doppelschritt.
    public class DoublePawn : Move
    {
        // Typ des Zugs.
        public override MoveType Type => MoveType.DoublePawn;
        // Startposition des Bauern.
        public override Position FromPos { get; }
        // Zielposition des Bauern.
        public override Position ToPos { get; }
        // Übersprungene Position für En Passant.
        private readonly Position skippedPos;

        // Konstruktor für Bauern-Doppelschritt.
        public DoublePawn(Position from, Position to)
        {
            FromPos = from;
            ToPos = to;
            skippedPos = new Position((from.Row + to.Row) / 2, from.Column);
        }

        // Führt den Doppelschritt aus.
        public override bool Execute(Board board)
        {
            Player player = board[FromPos]!.Color;
            board.SetPawnSkipPosition(player, skippedPos);
            new NormalMove(FromPos, ToPos).Execute(board);
            return true; // Bauernzug, relevant für 50-Züge-Regel.
        }
    }
}