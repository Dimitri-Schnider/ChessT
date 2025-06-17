using ChessLogic;

namespace ChessClient.Models
{
    // Kapselt alle Parameter, die zum Erstellen eines neuen Spiels benötigt werden.
    public class CreateGameParameters
    {
        public required string Name { get; set; }                                               // Der Name des Spielers.
        public Player Color { get; set; }                                                       // Die vom Spieler gewünschte Farbe (Weiss oder Schwarz).
        public int TimeMinutes { get; set; }                                                    // Die anfängliche Bedenkzeit in Minuten pro Spieler.
        public OpponentType OpponentType { get; set; } = OpponentType.Human;                    // Der Typ des Gegners (Mensch oder Computer).
        public ComputerDifficulty ComputerDifficulty { get; set; } = ComputerDifficulty.Medium; // Die Schwierigkeitsstufe des Computers.

    }

    // Definiert die möglichen Gegnertypen.
    public enum OpponentType
    {
        Human,
        Computer
    }

    // Definiert die Schwierigkeitsstufen für den Computergegner.
    public enum ComputerDifficulty
    {
        Easy,
        Medium,
        Hard
    }
}