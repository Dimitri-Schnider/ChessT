using System;
using System.Collections.Generic;
using System.Globalization;
using ChessLogic;
using ChessNetwork.DTOs;
using ChessClient.Models;

namespace ChessClient.State
{
    public class GameCoreState : IGameCoreState
    {
        public event Action? StateChanged;
        protected virtual void OnStateChanged() => StateChanged?.Invoke();

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
        private bool _isPvCGame;
        private bool _isGameRunning;

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
        public bool IsPvCGame { get => _isPvCGame; private set => _isPvCGame = value; }
        public bool IsGameRunning { get => _isGameRunning; private set => _isGameRunning = value; }

        public GameCoreState()
        {
        }

        public void InitializeNewGame(CreateGameResultDto result, string playerName, Player assignedColor, int initialTimeMinutes, string opponentTypeString) // NEU: opponentTypeString
        {
            CurrentPlayerInfo = new PlayerDto(result.PlayerId, playerName);
            MyColor = assignedColor;
            BoardDto = result.Board;
            GameId = result.GameId;
            IsPvCGame = (opponentTypeString == OpponentType.Computer.ToString()); // NEU: Setze IsPvCGame
            OpponentJoined = IsPvCGame; // Bei PvC ist der "Gegner" sofort da
            PlayerNames.Clear();
            if (CurrentPlayerInfo != null) PlayerNames[MyColor] = CurrentPlayerInfo.Name;

            if (IsPvCGame)
            {
                Player computerColor = MyColor.Opponent();
                // Der Computer-Name wird serverseitig gesetzt und über GetOpponentInfo geholt.
                // Hier können wir einen Platzhalter setzen oder warten, bis UpdatePlayerNames() ihn holt.
                // PlayerNames[computerColor] = $"Computer"; // Platzhalter
            }

            CurrentTurnPlayer = Player.White;
            IsGameSpecificDataInitialized = false;
            EndGameMessage = "";
            UpdateDisplayedTimes(TimeSpan.FromMinutes(initialTimeMinutes), TimeSpan.FromMinutes(initialTimeMinutes), Player.White);
            IsGameRunning = false;
            OnStateChanged();
        }

        public void InitializeJoinedGame(JoinGameResultDto result, Guid gameId, Player assignedColor)
        {
            CurrentPlayerInfo = new PlayerDto(result.PlayerId, result.Name);
            MyColor = assignedColor;
            BoardDto = result.Board;
            GameId = gameId;
            OpponentJoined = true;
            IsPvCGame = false; // Wer einem Spiel beitritt, spielt gegen einen Menschen (Annahme)
            PlayerNames.Clear();
            if (CurrentPlayerInfo != null) PlayerNames[MyColor] = CurrentPlayerInfo.Name;
            IsGameSpecificDataInitialized = false;
            EndGameMessage = "";
            IsGameRunning = false;
            OnStateChanged();
        }

        public void SetIsPvCGame(bool isPvC) 
        {
            if (_isPvCGame == isPvC) return;
            _isPvCGame = isPvC;
            OnStateChanged();
        }

        public void SetGameRunning(bool isRunning)
        {
            if (_isGameRunning == isRunning) return;
            _isGameRunning = isRunning;
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
            IsPvCGame = false;
            IsGameRunning = false;
            UpdateDisplayedTimes(TimeSpan.FromMinutes(initialTimeMinutes), TimeSpan.FromMinutes(initialTimeMinutes), Player.White);
            OnStateChanged();
        }
    }
}