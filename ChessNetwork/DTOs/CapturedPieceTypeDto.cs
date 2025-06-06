using ChessLogic;

namespace ChessNetwork.DTOs
{
    // DTO zur Übermittlung des Typs einer geschlagenen Figur.
    public record CapturedPieceTypeDto(

        PieceType Type  // Der Typ der Figur, als sie geschlagen wurde.
    );
}