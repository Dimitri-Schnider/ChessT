// File: [SolutionDir]\ChessClient\Models\CreateGameParameters.cs
using ChessLogic; // Benötigt für den Player Enum.

namespace ChessClient.Models
{
    // Parameter zum Erstellen eines neuen Spiels.
    public class CreateGameParameters
    {
        public required string Name { get; set; } // Name des Spielers.
        public Player Color { get; set; } // Gewünschte Farbe des Spielers.
        public int TimeMinutes { get; set; } // Initiale Bedenkzeit in Minuten pro Spieler.
        public OpponentType OpponentType { get; set; } = OpponentType.Human; // Standardmäßig Mensch
        public ComputerDifficulty ComputerDifficulty { get; set; } = ComputerDifficulty.Medium; // Standardmäßig Mittel
    }

    public enum OpponentType
    {
        Human,
        Computer
    }

    public enum ComputerDifficulty
    {
        Easy,   // Depth 1
        Medium, // Depth 10
        Hard    // Depth 30
    }
}