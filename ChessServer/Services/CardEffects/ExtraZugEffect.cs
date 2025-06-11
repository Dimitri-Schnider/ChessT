using Chess.Logging;
using ChessLogic;
using ChessNetwork.Configuration;
using ChessNetwork.DTOs;
using System;

namespace ChessServer.Services.CardEffects
{
    public class ExtraZugEffect : ICardEffect
    {
        private readonly IChessLogger _logger;
        public ExtraZugEffect(IChessLogger logger) { _logger = logger; }

        public CardActivationResult Execute(GameSession session, Guid playerId, Player playerDataColor, string cardTypeId, string? fromSquareAlg, string? toSquareAlg)
        {
            if (cardTypeId != CardConstants.ExtraZug)
                return new CardActivationResult(false, ErrorMessage: $"ExtraZugEffect fälschlicherweise für Karte {cardTypeId} aufgerufen.");

            if (!session.CardManager.IsCardUsableGlobal(playerId, CardConstants.ExtraZug))
                return new CardActivationResult(false, ErrorMessage: $"Karte '{CardConstants.ExtraZug}' wurde von Spieler {playerId} bereits verwendet.");

            session.CardManager.SetPendingCardEffectForNextMove(playerId, CardConstants.ExtraZug);
            session.CardManager.MarkCardAsUsedGlobal(playerId, cardTypeId);

            _logger.LogExtraZugEffectApplied(playerId, session.GameId);
            return new CardActivationResult(true, EndsPlayerTurn: false, BoardUpdatedByCardEffect: false);
        }
    }
}