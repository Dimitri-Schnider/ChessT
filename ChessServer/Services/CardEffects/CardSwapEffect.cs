using Chess.Logging;
using ChessLogic;
using ChessNetwork.Configuration;
using ChessNetwork.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessServer.Services.CardEffects
{
    // Implementiert den Karteneffekt, der eine zufällige Karte mit dem Gegner tauscht.
    public class CardSwapEffect : ICardEffect
    {
        private readonly IChessLogger _logger;
        // Statisches Random-Objekt, um die Zufälligkeit zu gewährleisten.
        private static readonly Random _random = new();

        // Konstruktor zur Initialisierung des Loggers.
        public CardSwapEffect(IChessLogger logger)
        {
            _logger = logger;
        }

        // Führt den Tauscheffekt aus.
        public CardActivationResult Execute(GameSession session, Guid playerId, Player playerDataColor, IHistoryManager historyManager, string cardTypeId, string? fromSquareAlg, string? toSquareAlg)
        {
            // Validiert die Anfrage. `fromSquareAlg` wird hier missbraucht, um die Instanz-ID der eigenen Karte zu übertragen.
            if (cardTypeId != CardConstants.CardSwap || !Guid.TryParse(fromSquareAlg, out Guid ownCardInstanceIdToSwap))
            {
                return new CardActivationResult(false, ErrorMessage: "Ungültige Anfrage für Kartentausch.");
            }

            // Greift auf den CardManager zu, um die Hand des Spielers zu erhalten.
            var playerHand = session.CardManager.GetPlayerHand(playerId);
            var cardToGive = playerHand.FirstOrDefault(c => c.InstanceId == ownCardInstanceIdToSwap);
            if (cardToGive == null)
            {
                return new CardActivationResult(false, ErrorMessage: "Ausgewählte Karte nicht auf der Hand.");
            }

            var opponentInfo = session.GetOpponentDetails(playerId);
            if (opponentInfo == null)
            {
                return new CardActivationResult(false, ErrorMessage: "Kein Gegner für Kartentausch gefunden.");
            }

            // Greift auf den CardManager zu, um die Hand des Gegners zu erhalten.
            var opponentHand = session.CardManager.GetPlayerHand(opponentInfo.OpponentId);
            // Spezialfall: Wenn der Gegner keine Karten hat, verfällt die eigene Karte.
            if (opponentHand.Count == 0)
            {
                session.CardManager.RemoveCardFromPlayerHand(playerId, cardToGive.InstanceId);
                return new CardActivationResult(true, ErrorMessage: "Dein Gegner hat keine Handkarten. Deine ausgewählte Karte verfällt.", CardGivenByPlayerForSwapEffect: cardToGive);
            }

            // Wählt eine zufällige Karte aus der Hand des Gegners aus.
            var cardToReceive = opponentHand[_random.Next(opponentHand.Count)];
            // Alle Kartenmanipulationen werden über den CardManager abgewickelt, um die Konsistenz sicherzustellen.
            session.CardManager.RemoveCardFromPlayerHand(playerId, cardToGive.InstanceId);
            session.CardManager.RemoveCardFromPlayerHand(opponentInfo.OpponentId, cardToReceive.InstanceId);

            // Fügt die getauschten Karten den jeweiligen Händen wieder hinzu.
            session.CardManager.AddCardToPlayerHand(playerId, cardToReceive);
            session.CardManager.AddCardToPlayerHand(opponentInfo.OpponentId, cardToGive);

            _logger.LogCardSwapEffectExecuted(cardToGive.InstanceId, cardToReceive.InstanceId, playerId, session.GameId);
            // Gibt ein erfolgreiches Ergebnis mit den Details des Tauschs für die Animation zurück.
            return new CardActivationResult(
                Success: true,
                CardGivenByPlayerForSwapEffect: cardToGive,
                CardReceivedByPlayerForSwapEffect: cardToReceive
            );
        }
    }
}