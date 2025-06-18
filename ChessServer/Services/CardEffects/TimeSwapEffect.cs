using Chess.Logging;
using ChessLogic;
using ChessNetwork.Configuration;
using ChessNetwork.DTOs;
using System;

namespace ChessServer.Services.CardEffects
{
    // Implementiert den Karteneffekt, der die Bedenkzeiten der Spieler tauscht.
    public class TimeSwapEffect : ICardEffect
    {
        private readonly IChessLogger _logger;
        public TimeSwapEffect(IChessLogger logger)
        {
            _logger = logger;
        }

        // Führt den Zeittausch-Effekt aus.
        public CardActivationResult Execute(CardExecutionContext context)
        {
            if (context.RequestDto.CardTypeId != CardConstants.TimeSwap)
            {
                return new CardActivationResult(false, ErrorMessage: $"TimeSwapEffect fälschlicherweise für Karte {context.RequestDto.CardTypeId} aufgerufen.");
            }

            Player opponentColor = context.PlayerColor.Opponent();
            Guid? opponentId = context.Session.GetPlayerIdByColor(opponentColor);
            if (!opponentId.HasValue)
            {
                return new CardActivationResult(false, ErrorMessage: "Gegner nicht gefunden für Zeittausch.");
            }

            if (context.Session.TimerService.SwapTimes(context.PlayerColor, opponentColor))
            {
                _logger.LogTimeSwapEffectApplied(context.PlayerColor, opponentColor, context.Session.GameId);
                return new CardActivationResult(true, BoardUpdatedByCardEffect: false);
            }

            return new CardActivationResult(false, ErrorMessage: "Zeiten konnten nicht getauscht werden (Timer-Fehler).");
        }
    }
}