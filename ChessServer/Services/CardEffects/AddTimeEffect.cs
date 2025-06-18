using Chess.Logging;
using ChessLogic;
using ChessNetwork.Configuration;
using ChessNetwork.DTOs;
using System;

namespace ChessServer.Services.CardEffects
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
        public CardActivationResult Execute(GameSession session, Guid playerId, Player playerDataColor,
                                            IHistoryManager historyManager,
                                            string cardTypeId,
                                            string? fromSquareAlg,
                                            string? toSquareAlg)
        {
            // Sicherheitsüberprüfung, ob dieser Effekt für die korrekte Karte aufgerufen wurde.
            if (cardTypeId != CardConstants.AddTime)
            {
                return new CardActivationResult(false, ErrorMessage: $"AddTimeEffect fälschlicherweise für Karte {cardTypeId} aufgerufen.");
            }

            // Versucht, die Zeit über den TimerService der Session hinzuzufügen.
            if (session.TimerService.AddTime(playerDataColor, TimeSpan.FromMinutes(2)))
            {
                _logger.LogAddTimeEffectApplied(playerDataColor, playerId, session.GameId);
                // Gibt ein erfolgreiches Ergebnis zurück. Das Brett wurde nicht verändert.
                return new CardActivationResult(true, BoardUpdatedByCardEffect: false);
            }

            // Gibt einen Fehler zurück, falls das Hinzufügen der Zeit fehlschlägt.
            return new CardActivationResult(false, ErrorMessage: "Zeit konnte nicht hinzugefügt werden (Timer-Fehler).");
        }
    }
}