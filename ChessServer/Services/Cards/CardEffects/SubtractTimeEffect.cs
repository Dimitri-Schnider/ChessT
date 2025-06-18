using Chess.Logging;
using ChessLogic;
using ChessNetwork.Configuration;
using ChessNetwork.DTOs;
using System;

namespace ChessServer.Services.Cards.CardEffects
{
    // Implementiert den Karteneffekt, der dem Gegner Zeit abzieht.
    public class SubtractTimeEffect : ICardEffect
    {
        private readonly IChessLogger _logger;
        public SubtractTimeEffect(IChessLogger logger)
        {
            _logger = logger;
        }

        // Führt den Effekt aus: Zieht dem Gegner 2 Minuten Zeit ab.
        public CardActivationResult Execute(CardExecutionContext context)
        {
            if (context.RequestDto.CardTypeId != CardConstants.SubtractTime)
            {
                return new CardActivationResult(false, ErrorMessage: $"SubtractTimeEffect fälschlicherweise für Karte {context.RequestDto.CardTypeId} aufgerufen.");
            }

            Player opponentColor = context.PlayerColor.Opponent();
            Guid? opponentId = context.Session.GetPlayerIdByColor(opponentColor);
            if (!opponentId.HasValue)
            {
                return new CardActivationResult(false, ErrorMessage: "Gegner nicht gefunden für Zeitdiebstahl.");
            }

            TimeSpan opponentTime = context.Session.TimerService.GetCurrentTimeForPlayer(opponentColor);
            if (opponentTime < TimeSpan.FromMinutes(3))
            {
                return new CardActivationResult(false, ErrorMessage: "Zeitdiebstahl kann nur eingesetzt werden, wenn der Gegner 3 Minuten oder mehr Zeit hat.");
            }

            if (context.Session.TimerService.SubtractTime(opponentColor, TimeSpan.FromMinutes(2)))
            {
                _logger.LogSubtractTimeEffectApplied(opponentColor, context.PlayerColor, context.PlayerId, context.Session.GameId);
                return new CardActivationResult(true, BoardUpdatedByCardEffect: false);
            }

            return new CardActivationResult(false, ErrorMessage: "Zeit konnte nicht abgezogen werden (Timer-Fehler).");
        }
    }
}