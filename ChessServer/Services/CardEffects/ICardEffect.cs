using ChessLogic;
using ChessNetwork.DTOs;
using System;
using System.Collections.Generic;

namespace ChessServer.Services.CardEffects
{
    // Definiert das Ergebnis einer Kartenaktivierung, das vom Server an den Client zurückgegeben wird.
    public record CardActivationResult(
        bool Success,
        string? ErrorMessage = null,
        bool EndsPlayerTurn = true,
        Guid? PlayerIdToSignalDraw = null,
        bool BoardUpdatedByCardEffect = false,
        List<AffectedSquareInfo>? AffectedSquaresByCard = null,
        CardDto? CardGivenByPlayerForSwapEffect = null,
        CardDto? CardReceivedByPlayerForSwapEffect = null
    );

    // Definiert den Vertrag für alle Karteneffekt-Implementierungen.
    public interface ICardEffect
    {
        // Führt den spezifischen Karteneffekt aus und gibt das Ergebnis zurück.
        CardActivationResult Execute(
            GameSession session,
            Guid playerId,
            Player playerDataColor,
            IHistoryManager historyManager, 
            string cardTypeId,
            string? fromSquareAlg,
            string? toSquareAlg
        );
    }
}