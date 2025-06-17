namespace ChessClient.Models
{
    // Kapselt die Parameter, die zum Beitreten eines Spiels benötigt werden.
    public class JoinGameParameters
    {
        public required string Name { get; set; }   // Der Name des Spielers, der beitritt.
        public required string GameId { get; set; } // Die eindeutige ID des Spiels, dem beigetreten werden soll.
    }
}