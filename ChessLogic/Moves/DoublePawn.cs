using ChessLogic.Utilities;

namespace ChessLogic
{
    // Repräsentiert einen Bauern-Doppelschritt von der Startposition.
    public class DoublePawn : Move
    {
        // Typ des Zugs ist immer DoublePawn.
        public override MoveType Type => MoveType.DoublePawn;
        // Startposition des Bauern.
        public override Position FromPos { get; }
        // Zielposition des Bauern (zwei Felder vorwärts).
        public override Position ToPos { get; }

        // Die Position, die vom Bauern übersprungen wird (relevant für En Passant).
        private readonly Position skippedPos;

        // Konstruktor für einen Bauern-Doppelschritt.
        public DoublePawn(Position from, Position to)
        {
            FromPos = from;
            ToPos = to;
            // Berechnet die übersprungene Position.
            skippedPos = new Position((from.Row + to.Row) / 2, from.Column);
        }

        // Führt den Bauern-Doppelschritt auf dem Brett aus.
        // Setzt die En-Passant-Möglichkeit für den Gegner.
        // Gibt true zurück, da es ein Bauernzug ist (relevant für 50-Züge-Regel).
        public override bool Execute(Board board)
        {
            Player player = board[FromPos]!.Color; // Farbe des ziehenden Bauern.
            board.SetPawnSkipPosition(player, skippedPos); // Speichert die übersprungene Position.
            new NormalMove(FromPos, ToPos).Execute(board); // Führt den eigentlichen Figurenschritt aus.
            return true;
        }
    }
}