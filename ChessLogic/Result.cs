namespace ChessLogic
{
    // Repräsentiert das Ergebnis einer beendeten Schachpartie.
    public class Result
    {
        // Der Gewinner des Spiels. Ist Player.None bei einem Remis.
        public Player Winner { get; }
        // Der Grund für das Spielende (z.B. Schachmatt, Patt).
        public EndReason Reason { get; }

        public Result(Player winner, EndReason reason)
        {
            Winner = winner;
            Reason = reason;
        }

        // Statische Hilfsmethode, um ein Sieg-Ergebnis zu erstellen.
        public static Result Win(Player winner, EndReason reason = EndReason.Checkmate)
        {
            return new Result(winner, reason);
        }

        // Statische Hilfsmethode, um ein Remis-Ergebnis zu erstellen.
        public static Result Draw(EndReason reason)
        {
            return new Result(Player.None, reason);
        }
    }
}