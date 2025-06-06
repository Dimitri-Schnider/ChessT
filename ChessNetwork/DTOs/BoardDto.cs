namespace ChessNetwork.DTOs
{
    // Data Transfer Object (DTO) für den Schachbrettzustand.
    public record BoardDto(PieceDto?[][] Squares);
}