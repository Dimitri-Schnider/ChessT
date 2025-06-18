using ChessLogic;
using ChessNetwork.DTOs;
using System;

namespace ChessServer.Services.CardEffects
{
    // Kapselt alle für die Ausführung eines Karteneffekts notwendigen Daten.
    public record CardExecutionContext(
        GameSession Session,
        Guid PlayerId,
        Player PlayerColor,
        IHistoryManager HistoryManager,
        ActivateCardRequestDto RequestDto // Das DTO enthält alle optionalen Parameter wie Felder, Figurentypen etc.
    );
}