namespace ChessLogic
{
    // Definiert die Spieler.
    public enum Player
    {
        None,  // Kein Spieler (z.B. leeres Feld).
        White, // Weisser Spieler.
        Black  // Schwarzer Spieler.
    }

    // Erweiterungsmethoden für den Player-Enum.
    public static class PlayerExtensions
    {
        // Gibt den Gegenspieler zurück.
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