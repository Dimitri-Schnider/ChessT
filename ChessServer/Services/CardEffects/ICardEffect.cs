// File: [SolutionDir]/ChessServer/Services/CardEffects/ICardEffect.cs
using System;
using System.Collections.Generic;
using ChessLogic;
using ChessNetwork.DTOs;
using ChessServer.Services;

namespace ChessServer.Services.CardEffects
{
    public record CardActivationResult(
        bool Success,
        string? ErrorMessage = null,
        bool EndsPlayerTurn = true,
        Guid? PlayerIdToSignalDraw = null,
        bool BoardUpdatedByCardEffect = false,
        List<AffectedSquareInfo>? AffectedSquaresByCard = null,
        CardDto? CardGivenByPlayerForSwapEffect = null, // Für Kartentausch-Animation
        CardDto? CardReceivedByPlayerForSwapEffect = null // Für Kartentausch-Animation
    );

    public interface ICardEffect
    {
        CardActivationResult Execute(
            GameSession session,
            Guid playerId,
            Player playerDataColor,
            string cardTypeId,
            string? fromSquareAlg,   // 1. optionaler Parameter
            string? toSquareAlg      // 2. optionaler Parameter
        );
    }
}