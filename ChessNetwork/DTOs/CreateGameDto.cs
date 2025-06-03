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
    }
}