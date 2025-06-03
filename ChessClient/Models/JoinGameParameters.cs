namespace ChessClient.Models
{
    // Parameter zum Beitreten eines vorhandenen Spiels.
    public class JoinGameParameters
    {
        public required string Name { get; set; } // Name des Spielers.
        public required string GameId { get; set; } // ID des Spiels, dem beigetreten werden soll.
    }
}