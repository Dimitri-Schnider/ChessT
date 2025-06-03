using ChessLogic; // Benötigt für den Player Enum.
namespace ChessClient.Models
{
    // Parameter zum Erstellen eines neuen Spiels.
    public class CreateGameParameters
    {
        public required string Name { get; set; } // Name des Spielers.
        public Player Color { get; set; } // Gewünschte Farbe des Spielers.
        public int TimeMinutes { get; set; } // Initiale Bedenkzeit in Minuten pro Spieler.
    }
}