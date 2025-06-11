using ChessLogic;
using ChessNetwork.DTOs;
using System;
using System.Collections.Generic;

namespace ChessServer.Services
{
    public class HistoryManager : IHistoryManager
    {
        private int _moveCounter;
        private readonly GameHistoryDto _gameHistory;
        private readonly List<PlayedMoveDto> _playedMoves = new();
        private readonly List<PlayedCardDto> _playedCards = new();
        public HistoryManager(Guid gameId, int initialTimeMinutes)
        {
            _gameHistory = new GameHistoryDto
            {
                GameId = gameId,
                InitialTimeMinutes = initialTimeMinutes,
                DateTimeStartedUtc = DateTime.UtcNow

            };
        }

        public void AddMove(PlayedMoveDto move)
        {
            _moveCounter++;
            move.MoveNumber = _moveCounter;
            _playedMoves.Add(move);
        }

        public int GetMoveCount() => _moveCounter;

        public void AddPlayedCard(PlayedCardDto card)
        {
            card.MoveNumberWhenActivated = _moveCounter + 1;
            _playedCards.Add(card);
        }

        public void UpdateOnGameOver(Result result)
        {
            if (_gameHistory.DateTimeEndedUtc.HasValue) return;
            _gameHistory.Winner = result.Winner;
            _gameHistory.ReasonForGameEnd = result.Reason;
            _gameHistory.DateTimeEndedUtc = DateTime.UtcNow;
        }

        public GameHistoryDto GetGameHistory(IPlayerManager playerManager)
        {
            _gameHistory.PlayerWhiteId = playerManager.GetPlayerIdByColor(Player.White);
            _gameHistory.PlayerBlackId = playerManager.GetPlayerIdByColor(Player.Black);
            _gameHistory.PlayerWhiteName = _gameHistory.PlayerWhiteId.HasValue ? playerManager.GetPlayerName(_gameHistory.PlayerWhiteId.Value) : null;
            _gameHistory.PlayerBlackName = _gameHistory.PlayerBlackId.HasValue ? playerManager.GetPlayerName(_gameHistory.PlayerBlackId.Value) : null;
            _gameHistory.Moves = new List<PlayedMoveDto>(_playedMoves);
            _gameHistory.PlayedCards = new List<PlayedCardDto>(_playedCards);

            return _gameHistory;
        }
    }
}