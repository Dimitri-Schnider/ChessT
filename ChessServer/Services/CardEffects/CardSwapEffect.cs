using Chess.Logging;
using ChessLogic;
using ChessNetwork.Configuration;
using ChessNetwork.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessServer.Services.CardEffects
{
    // Implementiert den Karteneffekt, bei dem eine Karte mit dem Gegner getauscht wird.
    public class CardSwapEffect : ICardEffect
    {
        private readonly IChessLogger _logger;
        private static readonly Random _random = new Random();

        public CardSwapEffect(IChessLogger logger)
        {
            _logger = logger;
        }

        // Führt den Tauscheffekt aus.
        public CardActivationResult Execute(GameSession session, Guid playerId, Player playerDataColor,
                                            string cardTypeId,
                                            string? fromSquareAlg,
                                            string? toSquareAlg)
        {
            string? cardInstanceIdToSwapFromHandString = fromSquareAlg;

            if (cardTypeId != CardConstants.CardSwap)
            {
                return new CardActivationResult(false, ErrorMessage: $"CardSwapEffect fälschlicherweise für Karte {cardTypeId} aufgerufen.");
            }

            if (string.IsNullOrEmpty(cardInstanceIdToSwapFromHandString) || !Guid.TryParse(cardInstanceIdToSwapFromHandString, out Guid ownCardInstanceIdToSwap))
            {
                _logger.LogCardSwapEffectPlayerCardInstanceNotFound(Guid.Empty, playerId, session.GameId);
                return new CardActivationResult(
                    Success: true,
                    ErrorMessage: "Keine eigene Karte zum Tauschen ausgewählt oder vorhanden. Kartentausch-Karte verfällt.",
                    EndsPlayerTurn: true,
                    BoardUpdatedByCardEffect: false,
                    CardGivenByPlayerForSwapEffect: null,
                    CardReceivedByPlayerForSwapEffect: null
                );
            }

            List<CardDto> playerHand = session.GetPlayerHand(playerId);
            CardDto? cardToGive = playerHand.FirstOrDefault(c => c.InstanceId == ownCardInstanceIdToSwap);

            if (cardToGive == null)
            {
                _logger.LogCardSwapEffectPlayerCardInstanceNotFound(ownCardInstanceIdToSwap, playerId, session.GameId);
                return new CardActivationResult(false, ErrorMessage: $"Die ausgewählte Karte (Instanz: {ownCardInstanceIdToSwap}) befindet sich nicht auf deiner Hand.");
            }

            OpponentInfoDto? opponentInfo = session.GetOpponentDetails(playerId);
            if (opponentInfo == null)
            {
                return new CardActivationResult(false, ErrorMessage: "Kein Gegner im Spiel für Kartentausch gefunden.");
            }

            Guid opponentId = opponentInfo.OpponentId;
            List<CardDto> opponentHand = session.GetPlayerHand(opponentId);
            if (opponentHand.Count == 0)
            {
                _logger.LogCardSwapEffectOpponentNoCards(playerId, session.GameId);
                session.RemoveCardFromPlayerHand(playerId, cardToGive.InstanceId);
                return new CardActivationResult(
                    Success: true,
                    ErrorMessage: "Dein Gegner hat keine Handkarten. Deine ausgewählte Karte verfällt.",
                    EndsPlayerTurn: true,
                    BoardUpdatedByCardEffect: false,
                    CardGivenByPlayerForSwapEffect: cardToGive,
                    CardReceivedByPlayerForSwapEffect: null
                );
            }

            CardDto cardToReceiveFromOpponent = opponentHand[_random.Next(opponentHand.Count)];
            bool ownCardRemoved = session.RemoveCardFromPlayerHand(playerId, cardToGive.InstanceId);
            session.AddCardToPlayerHand(playerId, cardToReceiveFromOpponent);
            bool opponentCardRemoved = session.RemoveCardFromPlayerHand(opponentId, cardToReceiveFromOpponent.InstanceId);
            session.AddCardToPlayerHand(opponentId, cardToGive);

            if (!(ownCardRemoved && opponentCardRemoved))
            {
                return new CardActivationResult(false, ErrorMessage: "Interner Fehler beim Entfernen der Karten während des Tauschs.");
            }

            _logger.LogCardSwapEffectExecuted(cardToGive.InstanceId, cardToReceiveFromOpponent.InstanceId, playerId, session.GameId);
            return new CardActivationResult(
                Success: true,
                EndsPlayerTurn: true,
                BoardUpdatedByCardEffect: false,
                CardGivenByPlayerForSwapEffect: cardToGive,
                CardReceivedByPlayerForSwapEffect: cardToReceiveFromOpponent
            );
        }
    }
}