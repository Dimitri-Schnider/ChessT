using ChessLogic;
using ChessNetwork.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChessServer.Services
{
    public interface ICardManager
    {
        void InitializeDecksForPlayer(Guid playerId, int initialTimeMinutes);
        List<CardDto> GetPlayerHand(Guid playerId);
        int GetDrawPileCount(Guid playerId);
        IEnumerable<CapturedPieceTypeDto> GetCapturedPieceTypesOfPlayer(Player playerColor);
        CardDto? GetCardDefinitionForAnimation(string cardTypeId);
        Task<ServerCardActivationResultDto> ActivateCard(Guid playerId, ActivateCardRequestDto dto);
        void IncrementPlayerMoveCount(Guid playerId);
        (bool ShouldDraw, CardDto? DrawnCard) CheckAndProcessCardDraw(Guid playerId);
        void AddCapturedPiece(Player ownerColor, PieceType pieceType);
        bool IsCardUsableGlobal(Guid playerId, string cardTypeId);
        void SetPendingCardEffectForNextMove(Guid playerId, string cardTypeId);
        string? GetAndClearPendingCardEffect(Guid playerId);
        bool RemoveCardFromPlayerHand(Guid playerId, Guid cardInstanceIdToRemove);
        void MarkCardAsUsedGlobal(Guid playerId, string cardTypeId);
        void AddCardToPlayerHand(Guid playerId, CardDto cardToAdd);
        void RemoveCapturedPieceOfType(Player ownerColor, PieceType type);
        CardDto? DrawCardForPlayer(Guid playerId);
    }
}