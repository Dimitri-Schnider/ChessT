namespace ChessLogic
{
    // Repräsentiert das Ergebnis eines Schachspiels.
    public class Result
    {
        // Gewinner des Spiels (None bei Remis).
        public Player Winner { get; }
        // Grund für das Spielende.
        public EndReason Reason { get; }

        // Konstruktor für ein Spielergebnis.
        public Result(Player winner, EndReason reason)
        {
            Winner = winner;
            Reason = reason;
        }

        // Erstellt ein Sieg-Ergebnis.
        public static Result Win(Player winner, EndReason reason = EndReason.Checkmate)
        {
            return new Result(winner, reason);
        }

        // Erstellt ein Remis-Ergebnis.
        public static Result Draw(EndReason reason)
        {
            return new Result(Player.None, reason);
        }
    }
}