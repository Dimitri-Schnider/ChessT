using System.Collections.Generic;

namespace ChessNetwork.DTOs
{
    public record InitialHandDto(
        List<CardDto> Hand,
        int DrawPileCount
    );
}