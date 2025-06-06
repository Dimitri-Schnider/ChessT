namespace ChessLogic
{
    // Definiert die möglichen Spieler-Entitäten.
    public enum Player
    {
        None,  // Repräsentiert keinen Spieler (z.B. für ein Remis).
        White, // Der weisse Spieler.
        Black  // Der schwarze Spieler.
    }

    // Stellt Erweiterungsmethoden für den Player-Enum bereit.
    public static class PlayerExtensions
    {
        // Gibt den jeweiligen Gegenspieler zurück.
        public static Player Opponent(this Player player)
        {
            return player switch
            {
                Player.White => Player.Black,
                Player.Black => Player.White,
                _ => Player.None,
            };
        }
    }
}