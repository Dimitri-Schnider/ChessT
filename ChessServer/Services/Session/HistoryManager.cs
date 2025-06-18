using ChessLogic;
using ChessNetwork.DTOs;
using System;
using System.Collections.Generic;

namespace ChessServer.Services.Session
{
    // Verwaltet den detaillierten Spielverlauf einer Partie.
    // Diese Klasse ist verantwortlich für das Sammeln und Speichern aller Züge und gespielten Karten
    // sowie der Metadaten des Spiels (Spieler, Zeiten, Ergebnis).
    public class HistoryManager : IHistoryManager
    {
        private int _moveCounter;                                   // Ein einfacher Zähler für die fortlaufende Zugnummer.
        private readonly GameHistoryDto _gameHistory;               // Das zentrale DTO, das den gesamten Spielverlauf kapselt.
        private readonly List<PlayedMoveDto> _playedMoves = new();  // Liste aller ausgeführten Züge.
        private readonly List<PlayedCardDto> _playedCards = new();  // Liste aller aktivierten Karten.

        // Konstruktor: Initialisiert einen neuen Spielverlauf mit den grundlegenden Metadaten.
        public HistoryManager(Guid gameId, int initialTimeMinutes)
        {
            _gameHistory = new GameHistoryDto
            {
                GameId = gameId,
                InitialTimeMinutes = initialTimeMinutes,
                DateTimeStartedUtc = DateTime.UtcNow
            };
        }

        // Fügt einen neuen Zug zur Historie hinzu und inkrementiert den Zugzähler.
        public void AddMove(PlayedMoveDto move)
        {
            _moveCounter++;
            move.MoveNumber = _moveCounter;
            _playedMoves.Add(move);
        }

        // Gibt die aktuelle Anzahl der Züge zurück.
        public int GetMoveCount() => _moveCounter;

        // Fügt eine gespielte Karte zur Historie hinzu.
        public void AddPlayedCard(PlayedCardDto card, bool boardWasUpdatedByCard)
        {
            // Weist der Karte die passende Zugnummer zu.
            // Wenn die Karte das Brett verändert, gehört sie zum aktuellen Zug (_moveCounter).
            // Ansonsten (z.B. Zeitkarte) wird sie als Aktion *vor* dem nächsten Zug (_moveCounter + 1) betrachtet.
            card.MoveNumberWhenActivated = boardWasUpdatedByCard ? _moveCounter : _moveCounter + 1;
            _playedCards.Add(card);
        }

        // Aktualisiert den Spielverlauf mit dem Endergebnis der Partie.
        public void UpdateOnGameOver(Result result)
        {
            // Verhindert, dass das Ergebnis mehrfach gesetzt wird.
            if (_gameHistory.DateTimeEndedUtc.HasValue) return;
            _gameHistory.Winner = result.Winner;
            _gameHistory.ReasonForGameEnd = result.Reason;
            _gameHistory.DateTimeEndedUtc = DateTime.UtcNow;
        }

        // Stellt das vollständige GameHistoryDto zusammen, indem Spielerinformationen hinzugefügt werden.
        public GameHistoryDto GetGameHistory(IPlayerManager playerManager)
        {
            _gameHistory.PlayerWhiteId = playerManager.GetPlayerIdByColor(Player.White);
            _gameHistory.PlayerBlackId = playerManager.GetPlayerIdByColor(Player.Black);
            _gameHistory.PlayerWhiteName = _gameHistory.PlayerWhiteId.HasValue ? playerManager.GetPlayerName(_gameHistory.PlayerWhiteId.Value) : null;
            _gameHistory.PlayerBlackName = _gameHistory.PlayerBlackId.HasValue ? playerManager.GetPlayerName(_gameHistory.PlayerBlackId.Value) : null;
            // Erstellt Kopien der Listen, um die Originale vor externen Änderungen zu schützen.
            _gameHistory.Moves = new List<PlayedMoveDto>(_playedMoves);
            _gameHistory.PlayedCards = new List<PlayedCardDto>(_playedCards);

            return _gameHistory;
        }
    }
}