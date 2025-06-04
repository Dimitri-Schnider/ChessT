using ChessLogic;

namespace ChessNetwork.DTOs
{
    // DTO zum Erstellen eines neuen Spiels.
    public class CreateGameDto
    {
        // Name des Spielers, der das Spiel erstellt. Erforderlich.
        public required string PlayerName { get; set; }
        // Gewünschte Farbe des erstellenden Spielers.
        public Player Color { get; set; }
        // Anfängliche Bedenkzeit pro Spieler in Minuten.
        public int InitialMinutes { get; set; }

        // NEUE Properties (verwenden Sie hier einfache Typen für DTOs)
        public string OpponentType { get; set; } = "Human"; // z.B. "Human", "Computer"
        public string ComputerDifficulty { get; set; } = "Medium"; // z.B. "Easy", "Medium", "Hard"
    }
}