using Chess.Logging;
using ChessLogic;
using ChessNetwork.Configuration;
using ChessNetwork.DTOs;
using System;

namespace ChessServer.Services.Cards.CardEffects
{
    // Implementiert den Karteneffekt, der dem Spieler einen zusätzlichen Zug gewährt.
    public class ExtraZugEffect : ICardEffect
    {
        private readonly IChessLogger _logger;
        public ExtraZugEffect(IChessLogger logger) { _logger = logger; }

        // Führt den Effekt aus.
        public CardActivationResult Execute(CardExecutionContext context)
        {
            if (context.RequestDto.CardTypeId != CardConstants.ExtraZug)
                return new CardActivationResult(false, ErrorMessage: $"ExtraZugEffect fälschlicherweise für Karte {context.RequestDto.CardTypeId} aufgerufen.");

            if (!context.Session.CardManager.IsCardUsableGlobal(context.PlayerId, CardConstants.ExtraZug))
                return new CardActivationResult(false, ErrorMessage: $"Karte '{CardConstants.ExtraZug}' wurde von Spieler {context.PlayerId} bereits verwendet.");

            context.Session.CardManager.SetPendingCardEffectForNextMove(context.PlayerId, CardConstants.ExtraZug);
            context.Session.CardManager.MarkCardAsUsedGlobal(context.PlayerId, context.RequestDto.CardTypeId);

            _logger.LogExtraZugEffectApplied(context.PlayerId, context.Session.GameId);

            return new CardActivationResult(true, EndsPlayerTurn: false, BoardUpdatedByCardEffect: false);
        }
    }
}