using Chess.Logging;
using ChessLogic;
using ChessNetwork.Configuration;
using ChessNetwork.DTOs;
using System;

namespace ChessServer.Services.CardEffects
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
        public CardActivationResult Execute(GameSession session, Guid playerId, Player playerDataColor,
                                            IHistoryManager historyManager,
                                            string cardTypeId,
                                            string? fromSquareAlg,
                                            string? toSquareAlg)
        {
            if (cardTypeId != CardConstants.SubtractTime)
            {
                return new CardActivationResult(false, ErrorMessage: $"SubtractTimeEffect fälschlicherweise für Karte {cardTypeId} aufgerufen.");
            }

            Player opponentColor = playerDataColor.Opponent();
            Guid? opponentId = session.GetPlayerIdByColor(opponentColor);
            if (!opponentId.HasValue)
            {
                return new CardActivationResult(false, ErrorMessage: "Gegner nicht gefunden für Zeitdiebstahl.");
            }

            TimeSpan opponentTime = session.TimerService.GetCurrentTimeForPlayer(opponentColor);
            if (opponentTime < TimeSpan.FromMinutes(3))
            {
                return new CardActivationResult(false, ErrorMessage: "Zeitdiebstahl kann nur eingesetzt werden, wenn der Gegner 3 Minuten oder mehr Zeit hat.");
            }

            if (session.TimerService.SubtractTime(opponentColor, TimeSpan.FromMinutes(2)))
            {
                _logger.LogSubtractTimeEffectApplied(opponentColor, playerDataColor, playerId, session.GameId);
                return new CardActivationResult(true, BoardUpdatedByCardEffect: false);
            }

            return new CardActivationResult(false, ErrorMessage: "Zeit konnte nicht abgezogen werden (Timer-Fehler).");
        }
    }
}