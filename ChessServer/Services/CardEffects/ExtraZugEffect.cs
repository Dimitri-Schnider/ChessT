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
        public ExtraZugEffect(IChessLogger logger) { _logger = logger; }

        // Führt den Effekt aus.
        public CardActivationResult Execute(GameSession session, Guid playerId, Player playerDataColor, IHistoryManager historyManager, string cardTypeId, string? fromSquareAlg, string? toSquareAlg)
        {
            if (cardTypeId != CardConstants.ExtraZug)
                return new CardActivationResult(false, ErrorMessage: $"ExtraZugEffect fälschlicherweise für Karte {cardTypeId} aufgerufen.");

            // Prüft, ob diese Karte (die global nur einmal pro Spieler verwendet werden darf) bereits genutzt wurde.
            if (!session.CardManager.IsCardUsableGlobal(playerId, CardConstants.ExtraZug))
                return new CardActivationResult(false, ErrorMessage: $"Karte '{CardConstants.ExtraZug}' wurde von Spieler {playerId} bereits verwendet.");

            // Registriert den Effekt als "anstehend" für den nächsten Zug. Der eigentliche Effekt wird in GameSession.MakeMove angewendet.
            session.CardManager.SetPendingCardEffectForNextMove(playerId, CardConstants.ExtraZug);
            // Markiert die Karte als global verbraucht.
            session.CardManager.MarkCardAsUsedGlobal(playerId, cardTypeId);

            _logger.LogExtraZugEffectApplied(playerId, session.GameId);
            // Gibt ein erfolgreiches Ergebnis zurück. Wichtig: EndsPlayerTurn ist false, da der Spieler am Zug bleibt.
            return new CardActivationResult(true, EndsPlayerTurn: false, BoardUpdatedByCardEffect: false);
        }
    }
}