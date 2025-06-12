// File: [SolutionDir]\ChessClient\State\GameCoreState.cs
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

        public PlayerDto? CurrentPlayerInfo { get; private set; }
        public BoardDto? BoardDto { get; private set; }
        public Player MyColor { get; private set; } = Player.White;
        public Guid GameId { get; private set; }
        public string? GameIdFromQueryString { get; private set; }
        public bool IsGameIdFromQueryValidAndExists { get; private set; }
        public Player? CurrentTurnPlayer { get; private set; }
        public bool OpponentJoined { get; private set; }
        public string EndGameMessage { get; private set; } = "";
        public Dictionary<Player, string> PlayerNames { get; private set; } = new();
        public bool IsGameSpecificDataInitialized { get; private set; }
        public string WhiteTimeDisplay { get; private set; } = "00:00";
        public string BlackTimeDisplay { get; private set; } = "00:00";

        // Korrigiert: Private Felder hinzugefügt
        private bool _isPvCGame;
        private bool _isGameRunning;
        private bool _isExtraTurnSequenceActive;
        private int _extraTurnMovesMade;

        public bool IsPvCGame => _isPvCGame;
        public bool IsGameRunning => _isGameRunning;
        public bool IsExtraTurnSequenceActive => _isExtraTurnSequenceActive;
        public int ExtraTurnMovesMade => _extraTurnMovesMade;

        public GameCoreState()
        {
        }

        public void SetExtraTurnSequenceActive(bool isActive)
        {
            if (_isExtraTurnSequenceActive == isActive) return;
            _isExtraTurnSequenceActive = isActive;
            if (!isActive)
            {
                _extraTurnMovesMade = 0;
            }
            OnStateChanged();
        }

        public void IncrementExtraTurnMovesMade()
        {
            _extraTurnMovesMade++;
            OnStateChanged();
        }

        public void InitializeNewGame(CreateGameResultDto result, CreateGameParameters args)
        {
            CurrentPlayerInfo = new PlayerDto(result.PlayerId, args.Name);
            MyColor = result.Color;
            BoardDto = result.Board;
            GameId = result.GameId;
            _isPvCGame = (args.OpponentType == OpponentType.Computer);
            OpponentJoined = _isPvCGame;

            // KORREKTUR: Erstelle ein neues Dictionary und weise es am Ende zu.
            // Dies stellt eine "atomare" Aktualisierung sicher und verhindert Race Conditions im UI-Rendering.
            var newPlayerNames = new Dictionary<Player, string>();

            // 1. Füge den menschlichen Spieler hinzu.
            newPlayerNames[MyColor] = args.Name;

            // 2. Wenn es ein PvC-Spiel ist, füge den Computer hinzu.
            if (_isPvCGame)
            {
                Player computerColor = MyColor.Opponent();
                string computerName = $"Computer ({args.ComputerDifficulty})";
                newPlayerNames[computerColor] = computerName;
            }

            // 3. Weise das vollständig vorbereitete Dictionary dem State zu.
            PlayerNames = newPlayerNames;

            CurrentTurnPlayer = Player.White;
            IsGameSpecificDataInitialized = false;
            EndGameMessage = "";
            UpdateDisplayedTimes(TimeSpan.FromMinutes(args.TimeMinutes), TimeSpan.FromMinutes(args.TimeMinutes), Player.White);
            _isGameRunning = false;
            SetExtraTurnSequenceActive(false);
            OnStateChanged();
        }

        public void InitializeJoinedGame(JoinGameResultDto result, Guid gameId, Player assignedColor)
        {
            CurrentPlayerInfo = new PlayerDto(result.PlayerId, result.Name);
            MyColor = assignedColor;
            BoardDto = result.Board;
            GameId = gameId;
            OpponentJoined = true;
            _isPvCGame = false;
            PlayerNames.Clear();
            if (CurrentPlayerInfo != null) PlayerNames[MyColor] = CurrentPlayerInfo.Name;
            IsGameSpecificDataInitialized = false;
            EndGameMessage = "";
            _isGameRunning = false;
            SetExtraTurnSequenceActive(false);
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

            bool wasGameRunning = string.IsNullOrEmpty(EndGameMessage);
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
            _isPvCGame = false;
            _isGameRunning = false;
            SetExtraTurnSequenceActive(false);
            UpdateDisplayedTimes(TimeSpan.FromMinutes(initialTimeMinutes), TimeSpan.FromMinutes(initialTimeMinutes), Player.White);
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
    }
}