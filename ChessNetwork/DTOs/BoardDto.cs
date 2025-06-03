namespace ChessNetwork.DTOs
{
    // Data Transfer Object (DTO) für den Schachbrettzustand.
    // Enthält ein 2D-Array von PieceDto?, das die Figuren repräsentiert.
    public record BoardDto(PieceDto?[][] Squares);
}