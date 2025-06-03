// File: [SolutionDir]/ChessServer/Services/CardEffects/TimeSwapEffect.cs
using System;
using ChessLogic;
using ChessServer.Services;
using ChessNetwork.Configuration;
using ChessNetwork.DTOs;
using Chess.Logging;

namespace ChessServer.Services.CardEffects
{
    public class TimeSwapEffect : ICardEffect
    {
        private readonly IChessLogger _logger;

        public TimeSwapEffect(IChessLogger logger)
        {
            _logger = logger;
        }

        public CardActivationResult Execute(GameSession session, Guid playerId, Player playerDataColor,
                                            string cardTypeId,
                                            string? fromSquareAlg,
                                            string? toSquareAlg)
        {
            if (cardTypeId != CardConstants.TimeSwap)
            {
                return new CardActivationResult(false, ErrorMessage: $"TimeSwapEffect fälschlicherweise für Karte {cardTypeId} aufgerufen.");
            }

            Player opponentColor = playerDataColor.Opponent();
            Guid? opponentId = session.GetPlayerIdByColor(opponentColor);
            if (!opponentId.HasValue)
            {
                return new CardActivationResult(false, ErrorMessage: "Gegner nicht gefunden für Zeittausch.");
            }

            if (session.TimerService.SwapTimes(playerDataColor, opponentColor))
            {
                _logger.LogTimeSwapEffectApplied(playerDataColor, opponentColor, session.GameId);
                return new CardActivationResult(true, BoardUpdatedByCardEffect: false);
            }
            return new CardActivationResult(false, ErrorMessage: "Zeiten konnten nicht getauscht werden (Timer-Fehler).");
        }
    }
}