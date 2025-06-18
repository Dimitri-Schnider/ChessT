using Chess.Logging;
using ChessLogic;
using ChessNetwork.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessServer.Services
{
    // Implementiert IPlayerManager und verwaltet die Spieler innerhalb einer GameSession.
    // Diese Klasse kapselt die Logik zur Spieler- und Farbzuweisung und zur Identifizierung von Gegnern.
    public class PlayerManager : IPlayerManager
    {
        private readonly Guid _gameId;
        private readonly IChessLogger _logger;
        private readonly object _lock = new object();

        // Speichert alle Spieler der Sitzung (ID -> (Name, Farbe))
        private readonly Dictionary<Guid, (string Name, Player Color)> _players = new();

        // Felder zur Verwaltung der Spieler und ihrer Farben
        private Guid _firstPlayerId = Guid.Empty;   // Der Spieler, der das Spiel erstellt hat.
        private Player _firstPlayerActualColor;     // Die tatsächlich zugewiesene Farbe des ersten Spielers.
        private Guid? _playerWhiteId;               // ID des weissen Spielers.
        private Guid? _playerBlackId;               // ID des schwarzen Spielers.

        // Spiel-spezifische Konfiguration 
        public string OpponentType { get; }                 // "Human" oder "Computer".
        private readonly string _computerDifficultyString;  // Schwierigkeitsgrad des Computers.
        public Guid? ComputerPlayerId { get; private set; } // ID des Computergegners.

        // Öffentliche Eigenschaften
        public int PlayerCount => _players.Count;
        public bool HasOpponent => _players.Count > 1;
        public Guid FirstPlayerId => _firstPlayerId;
        public Player FirstPlayerColor => _firstPlayerActualColor;

        // Konstruktor: Initialisiert den Manager für eine neue Spielsitzung.
        public PlayerManager(Guid gameId, string opponentType, string computerDifficulty, IChessLogger logger)
        {
            _gameId = gameId;
            OpponentType = opponentType;
            _computerDifficultyString = computerDifficulty;
            _logger = logger;
        }

        // Fügt einen neuen Spieler der Sitzung hinzu.
        public (Guid PlayerId, Player Color) Join(string playerName, Player? preferredColor = null)
        {
            var newPlayerId = Guid.NewGuid();
            var assignedColor = JoinInternal(newPlayerId, playerName, preferredColor);

            // Wenn es ein Spiel gegen den Computer ist und nur ein menschlicher Spieler beigetreten ist,
            // wird der Computergegner automatisch initialisiert.
            if (OpponentType == "Computer" && PlayerCount == 1 && ComputerPlayerId == null)
            {
                InitializeComputerPlayer();
            }

            return (newPlayerId, assignedColor);
        }

        // Interne Methode zur Zuweisung von Farben und zum Hinzufügen des Spielers.
        private Player JoinInternal(Guid playerId, string playerName, Player? preferredColorForCreator = null)
        {
            lock (_lock)
            {
                // Verhindert, dass derselbe Spieler mehrmals beitritt.
                if (_players.TryGetValue(playerId, out (string Name, Player Color) value))
                {
                    return value.Color;
                }

                // Verhindert, dass mehr als zwei Spieler beitreten.
                if (_players.Count >= 2)
                {
                    throw new InvalidOperationException("Spiel ist bereits voll.");
                }

                Player assignedColor;
                if (_players.Count == 0) // Erster Spieler (Ersteller)
                {
                    // Weist die gewünschte Farbe zu oder standardmässig Weiss.
                    assignedColor = preferredColorForCreator ?? Player.White;
                    _firstPlayerActualColor = assignedColor;
                    _firstPlayerId = playerId;

                    if (assignedColor == Player.White) _playerWhiteId = playerId;
                    else _playerBlackId = playerId;
                }
                else // Zweiter Spieler
                {
                    // Weist die gegnerische Farbe zu.
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

        // Erstellt den Computergegner und fügt ihn der Sitzung hinzu.
        private void InitializeComputerPlayer()
        {
            lock (_lock)
            {
                if (!(_players.Count == 1 && ComputerPlayerId == null && OpponentType == "Computer"))
                {
                    return; // Sicherheitsprüfung
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

        // Gibt die Farbe eines Spielers anhand seiner ID zurück.
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

        // Gibt die ID eines Spielers anhand seiner Farbe zurück.
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

        // Gibt den Namen eines Spielers anhand seiner ID zurück.
        public string? GetPlayerName(Guid playerId)
        {
            lock (_lock)
            {
                return _players.TryGetValue(playerId, out var playerData) ? playerData.Name : null;
            }
        }

        // Gibt die Informationen des Gegners zurück.
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