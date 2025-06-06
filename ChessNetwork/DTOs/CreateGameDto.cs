using ChessLogic;

namespace ChessNetwork.DTOs
{
    // DTO mit den Parametern zum Erstellen eines neuen Spiels.
    public class CreateGameDto
    {
        public required string PlayerName { get; set; }             // Der Name des Spielers, der das Spiel erstellt.
        public Player Color { get; set; }                           // Die vom Spieler gewünschte Farbe.
        public int InitialMinutes { get; set; }                     // Die anfängliche Bedenkzeit pro Spieler in Minuten.
        public string OpponentType { get; set; } = "Human";         // Der Typ des Gegners (z.B. "Human" oder "Computer").
        public string ComputerDifficulty { get; set; } = "Easy";    // Die Schwierigkeitsstufe des Computergegners (z.B. "Easy", "Medium", "Hard").
    }
}