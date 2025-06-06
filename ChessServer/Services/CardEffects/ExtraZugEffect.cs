using Chess.Logging;
using ChessLogic;
using ChessNetwork.Configuration;
using ChessNetwork.DTOs;
using System;

namespace ChessServer.Services.CardEffects
{
    // Implementiert den Karteneffekt, der dem Spieler einen zusätzlichen Zug gewährt.
    public class ExtraZugEffect : ICardEffect
    {
        private readonly IChessLogger _logger;

        public ExtraZugEffect(IChessLogger logger)
        {
            _logger = logger;
        }

        // Führt den Effekt aus: Markiert, dass der nächste Zug ein Extrazug ist.
        public CardActivationResult Execute(GameSession session, Guid playerId, Player playerDataColor,
                                            string cardTypeId,
                                            string? fromSquareAlg,
                                            string? toSquareAlg)
        {
            if (cardTypeId != CardConstants.ExtraZug)
            {
                return new CardActivationResult(false, ErrorMessage: $"ExtraZugEffect fälschlicherweise für Karte {cardTypeId} aufgerufen.");
            }

            if (!session.IsCardUsableGlobal(playerId, CardConstants.ExtraZug))
            {
                string errorMsg = $"Karte '{CardConstants.ExtraZug}' wurde von Spieler {playerId} bereits verwendet.";
                return new CardActivationResult(false, ErrorMessage: errorMsg);
            }

            session.MarkCardAsUsedGlobal(playerId, CardConstants.ExtraZug);
            session.SetPendingCardEffectForNextMove(playerId, CardConstants.ExtraZug);

            _logger.LogExtraZugEffectApplied(playerId, session.GameId);
            return new CardActivationResult(true, EndsPlayerTurn: false, BoardUpdatedByCardEffect: false);
        }
    }
}