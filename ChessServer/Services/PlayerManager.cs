using Chess.Logging;
using ChessLogic;
using ChessNetwork.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessServer.Services
{
    public class PlayerManager : IPlayerManager
    {
        private readonly Guid _gameId;
        private readonly IChessLogger _logger;
        private readonly object _lock = new object();

        // --- Extrahierte Felder aus GameSession ---
        private readonly Dictionary<Guid, (string Name, Player Color)> _players = new();
        private Guid _firstPlayerId = Guid.Empty;
        private Player _firstPlayerActualColor;
        private Guid? _playerWhiteId;
        private Guid? _playerBlackId;

        // --- Spiel-spezifische Konfiguration ---
        public string OpponentType { get; }
        private readonly string _computerDifficultyString;
        public Guid? ComputerPlayerId { get; private set; }

        // --- Öffentliche Eigenschaften ---
        public int PlayerCount => _players.Count;
        public bool HasOpponent => _players.Count > 1;
        public Guid FirstPlayerId => _firstPlayerId;
        public Player FirstPlayerColor => _firstPlayerActualColor;

        public PlayerManager(Guid gameId, string opponentType, string computerDifficulty, IChessLogger logger)
        {
            _gameId = gameId;
            OpponentType = opponentType;
            _computerDifficultyString = computerDifficulty;
            _logger = logger;
        }

        public (Guid PlayerId, Player Color) Join(string playerName, Player? preferredColor = null)
        {
            var newPlayerId = Guid.NewGuid();
            var assignedColor = JoinInternal(newPlayerId, playerName, preferredColor);

            if (OpponentType == "Computer" && PlayerCount == 1 && ComputerPlayerId == null)
            {
                InitializeComputerPlayer();
            }

            return (newPlayerId, assignedColor);
        }

        private Player JoinInternal(Guid playerId, string playerName, Player? preferredColorForCreator = null)
        {
            lock (_lock)
            {
                if (_players.TryGetValue(playerId, out (string Name, Player Color) value))
                {
                    return value.Color;
                }

                if (_players.Count >= 2)
                {
                    throw new InvalidOperationException("Spiel ist bereits voll.");
                }

                Player assignedColor;
                if (_players.Count == 0) // Erster Spieler (Ersteller)
                {
                    assignedColor = preferredColorForCreator ?? Player.White;
                    _firstPlayerActualColor = assignedColor;
                    _firstPlayerId = playerId;

                    if (assignedColor == Player.White) _playerWhiteId = playerId;
                    else _playerBlackId = playerId;
                }
                else // Zweiter Spieler
                {
                    assignedColor = _firstPlayerActualColor.Opponent();
                    if ((assignedColor == Player.White && _playerWhiteId != null) || (assignedColor == Player.Black && _playerBlackId != null))
                    {
                        throw new InvalidOperationException($"Farbe {assignedColor} ist unerwartet bereits belegt.");
                    }
                    if (assignedColor == Player.White) _playerWhiteId = playerId;
                    else _playerBlackId = playerId;
                }

                _players[playerId] = (playerName, assignedColor);
                return assignedColor;
            }
        }

        private void InitializeComputerPlayer()
        {
            lock (_lock)
            {
                if (!(_players.Count == 1 && ComputerPlayerId == null && OpponentType == "Computer"))
                {
                    return; // Sollte nicht passieren, aber zur Sicherheit
                }

                Player humanPlayerColor = _firstPlayerActualColor;
                Player computerColor = humanPlayerColor.Opponent();
                Guid computerId = Guid.NewGuid();
                string computerName = $"Computer ({_computerDifficultyString})";

                ComputerPlayerId = computerId;
                _players[computerId] = (computerName, computerColor);

                if (computerColor == Player.White)
                {
                    _playerWhiteId = computerId;
                    if (_playerBlackId == null) _playerBlackId = _firstPlayerId;
                }
                else
                {
                    _playerBlackId = computerId;
                    if (_playerWhiteId == null) _playerWhiteId = _firstPlayerId;
                }

                _logger.LogPlayerJoinedGame(computerName, _gameId);
            }
        }

        public Player GetPlayerColor(Guid playerId)
        {
            lock (_lock)
            {
                if (_players.TryGetValue(playerId, out var playerData))
                {
                    return playerData.Color;
                }

                if (playerId == _firstPlayerId && _players.Count <= 1)
                {
                    return _firstPlayerActualColor;
                }

                _logger.LogSessionColorNotDetermined(_gameId, playerId, _players.Count);
                throw new InvalidOperationException($"Spieler mit ID {playerId} nicht in der Session {_gameId} gefunden oder Farbe noch nicht eindeutig zugewiesen.");
            }
        }

        public Guid? GetPlayerIdByColor(Player color)
        {
            lock (_lock)
            {
                if (color == Player.White) return _playerWhiteId;
                if (color == Player.Black) return _playerBlackId;

                _logger.LogGetPlayerIdByColorFailed(_gameId, color, _playerWhiteId, _playerBlackId);
                return null;
            }
        }

        public string? GetPlayerName(Guid playerId)
        {
            lock (_lock)
            {
                return _players.TryGetValue(playerId, out var playerData) ? playerData.Name : null;
            }
        }

        public OpponentInfoDto? GetOpponentDetails(Guid currentPlayerId)
        {
            lock (_lock)
            {
                if (!_players.TryGetValue(currentPlayerId, out var currentPlayerDetails))
                {
                    _logger.LogCurrentPlayerNotFoundForOpponentDetails(currentPlayerId, _gameId);
                    return null;
                }

                Player opponentColor = currentPlayerDetails.Color.Opponent();
                Guid? opponentId = GetPlayerIdByColor(opponentColor);

                if (opponentId.HasValue && _players.TryGetValue(opponentId.Value, out var opponentData))
                {
                    return new OpponentInfoDto(opponentId.Value, opponentData.Name, opponentColor);
                }

                _logger.LogNoOpponentFoundForPlayer(currentPlayerId, currentPlayerDetails.Color, _gameId);
                return null;
            }
        }
    }
}