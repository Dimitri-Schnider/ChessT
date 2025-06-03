// File: [SolutionDir]/ChessServer/Services/CardEffects/AddTimeEffect.cs
using System;
using ChessLogic;
using ChessServer.Services;
using ChessNetwork.Configuration;
using ChessNetwork.DTOs;
using Chess.Logging;

namespace ChessServer.Services.CardEffects
{
    public class AddTimeEffect : ICardEffect
    {
        private readonly IChessLogger _logger;

        public AddTimeEffect(IChessLogger logger)
        {
            _logger = logger;
        }

        public CardActivationResult Execute(GameSession session, Guid playerId, Player playerDataColor,
                                            string cardTypeId,
                                            string? fromSquareAlg,
                                            string? toSquareAlg)
        {
            if (cardTypeId != CardConstants.AddTime)
            {
                return new CardActivationResult(false, ErrorMessage: $"AddTimeEffect fälschlicherweise für Karte {cardTypeId} aufgerufen.");
            }

            if (session.TimerService.AddTime(playerDataColor, TimeSpan.FromMinutes(2)))
            {
                _logger.LogAddTimeEffectApplied(playerDataColor, playerId, session.GameId);
                return new CardActivationResult(true, BoardUpdatedByCardEffect: false);
            }
            return new CardActivationResult(false, ErrorMessage: "Zeit konnte nicht hinzugefügt werden (Timer-Fehler).");
        }
    }
}