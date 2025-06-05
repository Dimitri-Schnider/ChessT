namespace ChessLogic
{
    // Definiert die möglichen Spieler-Entitäten in einer Schachpartie.
    public enum Player
    {
        None,  // Repräsentiert keinen Spieler, z.B. für ein leeres Feld oder ein Remis-Ergebnis.
        White, // Der weisse Spieler.
        Black  // Der schwarze Spieler.
    }

    // Stellt Erweiterungsmethoden für den Player-Enum bereit.
    public static class PlayerExtensions
    {
        // Gibt den jeweiligen Gegenspieler zurück.
        // Für Player.None wird Player.None zurückgegeben.
        public static Player Opponent(this Player player)
        {
            return player switch
            {
                Player.White => Player.Black,
                Player.Black => Player.White,
                _ => Player.None, // Player.None hat keinen Gegner.
            };
        }
    }
}