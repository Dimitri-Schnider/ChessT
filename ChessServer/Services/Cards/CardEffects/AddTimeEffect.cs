using Chess.Logging;
using ChessLogic;
using ChessNetwork.Configuration;
using ChessNetwork.DTOs;
using System;

namespace ChessServer.Services.Cards.CardEffects
{
    // Implementiert den Karteneffekt, der dem Spieler Zeit hinzufügt.
    public class AddTimeEffect : ICardEffect
    {
        private readonly IChessLogger _logger;

        // Konstruktor zur Initialisierung des Loggers.
        public AddTimeEffect(IChessLogger logger)
        {
            _logger = logger;
        }

        // Führt den Effekt aus: Fügt dem Spieler 2 Minuten Zeit hinzu.
        public CardActivationResult Execute(CardExecutionContext context)
        {
            // Sicherheitsüberprüfung, ob dieser Effekt für die korrekte Karte aufgerufen wurde.
            if (context.RequestDto.CardTypeId != CardConstants.AddTime)
            {
                return new CardActivationResult(false, ErrorMessage: $"AddTimeEffect fälschlicherweise für Karte {context.RequestDto.CardTypeId} aufgerufen.");
            }

            // Versucht, die Zeit über den TimerService der Session hinzuzufügen.
            if (context.Session.TimerService.AddTime(context.PlayerColor, TimeSpan.FromMinutes(2)))
            {
                _logger.LogAddTimeEffectApplied(context.PlayerColor, context.PlayerId, context.Session.GameId);
                return new CardActivationResult(true, BoardUpdatedByCardEffect: false);
            }

            // Gibt einen Fehler zurück, falls das Hinzufügen der Zeit fehlschlägt.
            return new CardActivationResult(false, ErrorMessage: "Zeit konnte nicht hinzugefügt werden (Timer-Fehler).");
        }
    }
}