using Chess.Logging;
using ChessLogic;
using ChessNetwork.Configuration;
using ChessNetwork.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessServer.Services.CardEffects
{
    public class CardSwapEffect : ICardEffect
    {
        private readonly IChessLogger _logger;
        private static readonly Random _random = new();

        public CardSwapEffect(IChessLogger logger)
        {
            _logger = logger;
        }

        public CardActivationResult Execute(GameSession session, Guid playerId, Player playerDataColor, IHistoryManager historyManager, string cardTypeId, string? fromSquareAlg, string? toSquareAlg)
        {
            if (cardTypeId != CardConstants.CardSwap || !Guid.TryParse(fromSquareAlg, out Guid ownCardInstanceIdToSwap))
            {
                return new CardActivationResult(false, ErrorMessage: "Ungültige Anfrage für Kartentausch.");
            }

            // Greift auf CardManager zu
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

            // Greift auf CardManager zu
            var opponentHand = session.CardManager.GetPlayerHand(opponentInfo.OpponentId);
            if (opponentHand.Count == 0)
            {
                session.CardManager.RemoveCardFromPlayerHand(playerId, cardToGive.InstanceId);
                return new CardActivationResult(true, ErrorMessage: "Dein Gegner hat keine Handkarten. Deine ausgewählte Karte verfällt.", CardGivenByPlayerForSwapEffect: cardToGive);
            }

            var cardToReceive = opponentHand[_random.Next(opponentHand.Count)];
            // Alle Kartenmanipulationen über den CardManager
            session.CardManager.RemoveCardFromPlayerHand(playerId, cardToGive.InstanceId);
            session.CardManager.RemoveCardFromPlayerHand(opponentInfo.OpponentId, cardToReceive.InstanceId);

            // KORREKTUR: Die folgenden zwei Zeilen wurden wieder aktiviert.
            session.CardManager.AddCardToPlayerHand(playerId, cardToReceive);
            session.CardManager.AddCardToPlayerHand(opponentInfo.OpponentId, cardToGive);

            _logger.LogCardSwapEffectExecuted(cardToGive.InstanceId, cardToReceive.InstanceId, playerId, session.GameId);
            return new CardActivationResult(
                Success: true,
                CardGivenByPlayerForSwapEffect: cardToGive,
                CardReceivedByPlayerForSwapEffect: cardToReceive
            );
        }
    }
}