namespace ChessNetwork.DTOs
{
    // DTO für die Anfrage zum Beitritt zu einem Spiel.
    // Enthält den Namen des beitretenden Spielers.
    public record JoinDto(string PlayerName);
}