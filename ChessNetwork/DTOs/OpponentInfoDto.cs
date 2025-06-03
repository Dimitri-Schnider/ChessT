using ChessLogic;

namespace ChessNetwork.DTOs
{
    public record OpponentInfoDto(
        Guid OpponentId,
        string OpponentName,
        Player OpponentColor
    );
}