using System;
using System.Collections.Generic;
using System.Globalization;
using ChessLogic;
using ChessNetwork.DTOs;

namespace ChessClient.State
{
    public class GameCoreState : IGameCoreState
    {
        public event Action? StateChanged;
        protected virtual void OnStateChanged() => StateChanged?.Invoke();

        // Private Backing Fields für Properties, deren Setter optimiert werden
        private PlayerDto? _currentPlayerInfo;
        private BoardDto? _boardDto;
        private Player _myColor = Player.White;
        private Guid _gameId;
        private string? _gameIdFromQueryString;
        private bool _isGameIdFromQueryValidAndExists;
        private Player? _currentTurnPlayer;
        private bool _opponentJoined;
        private string _endGameMessage = "";
        private Dictionary<Player, string> _playerNames = new();
        private bool _isGameSpecificDataInitialized;
        private string _whiteTimeDisplay = "00:00";
        private string _blackTimeDisplay = "00:00";

        public PlayerDto? CurrentPlayerInfo { get => _currentPlayerInfo; private set => _currentPlayerInfo = value; }
        public BoardDto? BoardDto { get => _boardDto; private set => _boardDto = value; }
        public Player MyColor { get => _myColor; private set => _myColor = value; }
        public Guid GameId { get => _gameId; private set => _gameId = value; }
        public string? GameIdFromQueryString { get => _gameIdFromQueryString; private set => _gameIdFromQueryString = value; }
        public bool IsGameIdFromQueryValidAndExists { get => _isGameIdFromQueryValidAndExists; private set => _isGameIdFromQueryValidAndExists = value; }
        public Player? CurrentTurnPlayer { get => _currentTurnPlayer; private set => _currentTurnPlayer = value; }
        public bool OpponentJoined { get => _opponentJoined; private set => _opponentJoined = value; }
        public string EndGameMessage { get => _endGameMessage; private set => _endGameMessage = value; }
        public Dictionary<Player, string> PlayerNames { get => _playerNames; private set => _playerNames = value; }
        public bool IsGameSpecificDataInitialized { get => _isGameSpecificDataInitialized; private set => _isGameSpecificDataInitialized = value; }
        public string WhiteTimeDisplay { get => _whiteTimeDisplay; private set => _whiteTimeDisplay = value; }
        public string BlackTimeDisplay { get => _blackTimeDisplay; private set => _blackTimeDisplay = value; }
        public GameCoreState()
        {
        }

        public void InitializeNewGame(CreateGameResultDto result, string playerName, Player assignedColor, int initialTimeMinutes)
        {
            CurrentPlayerInfo = new PlayerDto(result.PlayerId, playerName);
            MyColor = assignedColor;
            BoardDto = result.Board;
            GameId = result.GameId;
            OpponentJoined = false;
            PlayerNames.Clear();
            if (CurrentPlayerInfo != null) PlayerNames[MyColor] = CurrentPlayerInfo.Name;
            CurrentTurnPlayer = Player.White;
            IsGameSpecificDataInitialized = false;
            EndGameMessage = "";
            UpdateDisplayedTimes(TimeSpan.FromMinutes(initialTimeMinutes), TimeSpan.FromMinutes(initialTimeMinutes), Player.White);
            // OnStateChanged() wird hier bewusst am Ende einmal gerufen, da viele Zustände sich ändern.
            OnStateChanged();
        }

        public void InitializeJoinedGame(JoinGameResultDto result, Guid gameId, Player assignedColor)
        {
            CurrentPlayerInfo = new PlayerDto(result.PlayerId, result.Name);
            MyColor = assignedColor;
            BoardDto = result.Board;
            GameId = gameId;
            OpponentJoined = true;
            PlayerNames.Clear();
            if (CurrentPlayerInfo != null) PlayerNames[MyColor] = CurrentPlayerInfo.Name;
            IsGameSpecificDataInitialized = false;
            EndGameMessage = "";
            // OnStateChanged() hier, da mehrere Zustände geändert wurden.
            OnStateChanged();
        }

        public void SetGameIdFromQuery(string? gameIdQuery, bool isValidAndExists)
        {
            if (GameIdFromQueryString == gameIdQuery && IsGameIdFromQueryValidAndExists == isValidAndExists) return;
            GameIdFromQueryString = gameIdQuery;
            IsGameIdFromQueryValidAndExists = isValidAndExists;
            OnStateChanged();
        }

        public void UpdatePlayerNames(Dictionary<Player, string> names)
        {
            // Für Dictionaries ist ein einfacher Vergleich schwierig, wenn Inhalte sich ändern.
            // Hier wird angenommen, dass ein Aufruf immer eine relevante Änderung bedeutet.
            PlayerNames = new Dictionary<Player, string>(names);
            OnStateChanged();
        }

        public void SetPlayerName(Player color, string name)
        {
            if (PlayerNames.TryGetValue(color, out var existingName) && existingName == name) return;
            PlayerNames[color] = name;
            OnStateChanged();
        }

        public void UpdateBoard(BoardDto newBoard)
        {
            // BoardDto ist ein Record, Vergleich auf Referenz oder Inhalt (wenn überschrieben)
            // Hier gehen wir davon aus, dass ein neues BoardDto immer eine Änderung ist.
            BoardDto = newBoard;
            OnStateChanged();
        }

        public void SetCurrentTurnPlayer(Player? player)
        {
            if (CurrentTurnPlayer == player) return;
            CurrentTurnPlayer = player;
            OnStateChanged();
        }

        public void SetOpponentJoined(bool joined)
        {
            if (OpponentJoined == joined) return;
            OpponentJoined = joined;
            OnStateChanged();
        }

        public void SetEndGameMessage(string message)
        {
            if (EndGameMessage == message) return;
            EndGameMessage = message;
            OnStateChanged();
        }

        public void ClearEndGameMessage()
        {
            if (string.IsNullOrEmpty(EndGameMessage)) return;
            EndGameMessage = "";
            OnStateChanged();
        }

        public void SetGameSpecificDataInitialized(bool initialized)
        {
            if (IsGameSpecificDataInitialized == initialized) return;
            IsGameSpecificDataInitialized = initialized;
            OnStateChanged();
        }

        public void UpdateDisplayedTimes(TimeSpan whiteTime, TimeSpan blackTime, Player? activeTimerPlayer)
        {
            string newWhiteTimeDisplay = whiteTime.ToString(@"mm\:ss", CultureInfo.InvariantCulture);
            string newBlackTimeDisplay = blackTime.ToString(@"mm\:ss", CultureInfo.InvariantCulture);
            // CurrentTurnPlayer wird separat gesetzt, hier nur Times prüfen.
            if (WhiteTimeDisplay == newWhiteTimeDisplay && BlackTimeDisplay == newBlackTimeDisplay) return;

            WhiteTimeDisplay = newWhiteTimeDisplay;
            BlackTimeDisplay = newBlackTimeDisplay;
            OnStateChanged();
        }

        public void ResetForNewGame(int initialTimeMinutes = 15)
        {
            CurrentPlayerInfo = null;
            BoardDto = null;
            MyColor = Player.White;
            GameId = Guid.Empty;
            GameIdFromQueryString = null;
            IsGameIdFromQueryValidAndExists = false;
            CurrentTurnPlayer = Player.White;
            OpponentJoined = false;
            EndGameMessage = "";
            PlayerNames.Clear();
            IsGameSpecificDataInitialized = false;
            UpdateDisplayedTimes(TimeSpan.FromMinutes(initialTimeMinutes), TimeSpan.FromMinutes(initialTimeMinutes), Player.White);
            // Umfassende Reset-Aktion, OnStateChanged ist hier gerechtfertigt.
            OnStateChanged();
        }
    }
}