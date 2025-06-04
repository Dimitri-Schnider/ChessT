using System;
using System.Collections.Generic;

namespace ChessNetwork.DTOs
{
    public class ServerCardActivationResultDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public required string CardId { get; set; }
        public List<AffectedSquareInfo>? AffectedSquaresByCard { get; set; }
        public bool EndsPlayerTurn { get; set; } = true;
        public bool BoardUpdatedByCardEffect { get; set; }
        public Guid? PlayerIdToSignalCardDraw { get; set; }
        public CardDto? NewlyDrawnCard { get; set; }

        public CardDto? CardGivenByPlayerForSwap { get; set; }
        public CardDto? CardReceivedByPlayerForSwap { get; set; }

        public PositionDto? PawnPromotionPendingAt { get; set; }
    }
}