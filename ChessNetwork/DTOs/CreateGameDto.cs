using ChessLogic;

namespace ChessNetwork.DTOs
{
    // DTO mit den Parametern zum Erstellen eines neuen Spiels.
    public class CreateGameDto
    {
        public string PlayerName { get; set; } = string.Empty;                                  // Der Name des Spielers, der das Spiel erstellt.
        public Player Color { get; set; }                                                       // Die vom Spieler gewünschte Farbe.
        public int InitialMinutes { get; set; }                                                 // Die anfängliche Bedenkzeit pro Spieler in Minuten.
        public OpponentType OpponentType { get; set; } = OpponentType.Human;                    // Der Typ des Gegners (z.B. "Human" oder "Computer").
        public ComputerDifficulty ComputerDifficulty { get; set; } = ComputerDifficulty.Medium; // Die Schwierigkeitsstufe des Computergegners (z.B. "Easy", "Medium", "Hard").
    }
}