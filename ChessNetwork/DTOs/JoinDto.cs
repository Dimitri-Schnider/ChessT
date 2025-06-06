namespace ChessNetwork.DTOs
{
    // DTO für die Anfrage zum Beitritt zu einem Spiel.
    public record JoinDto(
        string PlayerName   // Der Name des Spielers, der dem Spiel beitreten möchte.

    );
}