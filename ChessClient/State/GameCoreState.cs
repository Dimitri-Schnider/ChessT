using System;
using System.Collections.Generic;
using System.Globalization;
using ChessLogic;
using ChessNetwork.DTOs;
using ChessClient.Models;
using ChessClient.Utils;
using ChessClient.Extensions;

namespace ChessClient.State
{
    // Implementiert IGameCoreState und ist der zentrale Container für den Kernzustand des Spiels auf dem Client.
    // Diese Klasse hält alle fundamentalen Informationen wie Brettstellung, Spieler, Zeit usw.
    public class GameCoreState : IGameCoreState
    {
        // Event, das ausgelöst wird, wenn sich ein beliebiger Wert in diesem State ändert.
        public event Action? StateChanged;
        // Methode, um das StateChanged-Event sicher auszulösen und die UI zum Neu-Rendern zu veranlassen.
        protected virtual void OnStateChanged() => StateChanged?.Invoke();

        // --- Private Felder für den internen Zustand ---
        private bool _isPvCGame;
        private bool _isGameRunning;
        private bool _isExtraTurnSequenceActive;
        private int _extraTurnMovesMade;
        private PieceDto? _pieceCapturedInPendingMove;

        // --- Öffentliche Properties (Zustand) ---
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
        public bool IsPvCGame => _isPvCGame;
        public bool IsGameRunning => _isGameRunning;
        public bool IsExtraTurnSequenceActive => _isExtraTurnSequenceActive;
        public int ExtraTurnMovesMade => _extraTurnMovesMade;

        // --- Properties für Optimistic UI ---
        public bool IsAwaitingMoveConfirmation { get; private set; }
        public MoveDto? PendingMove { get; private set; }

        public GameCoreState() { }

        // --- Methoden für Optimistic UI ---

        // Führt einen Zug optimistisch auf dem Client aus, bevor die Server-Bestätigung eintrifft.
        public void ApplyOptimisticMove(MoveDto move)
        {
            if (BoardDto == null || IsAwaitingMoveConfirmation) return;

            var from = PositionHelper.ToIndices(move.From);
            var to = PositionHelper.ToIndices(move.To);

            // Merkt sich eine eventuell geschlagene Figur für einen möglichen Rollback.
            var pieceToMove = BoardDto.Squares[from.Row][from.Column];
            _pieceCapturedInPendingMove = BoardDto.Squares[to.Row][to.Column];

            // Führt den Zug auf dem lokalen Brett-DTO aus.
            BoardDto.Squares[to.Row][to.Column] = pieceToMove;
            BoardDto.Squares[from.Row][from.Column] = null;

            // Setzt den Status "warte auf Bestätigung".
            PendingMove = move;
            IsAwaitingMoveConfirmation = true;

            OnStateChanged();
        }

        // Macht einen optimistischen Zug rückgängig, falls der Server ihn ablehnt.
        public void RevertOptimisticMove()
        {
            if (BoardDto == null || !IsAwaitingMoveConfirmation || PendingMove == null) return;

            var from = PositionHelper.ToIndices(PendingMove.From);
            var to = PositionHelper.ToIndices(PendingMove.To);

            // Stellt die ursprüngliche Brettsituation wieder her.
            var pieceThatMoved = BoardDto.Squares[to.Row][to.Column];
            BoardDto.Squares[from.Row][from.Column] = pieceThatMoved;
            BoardDto.Squares[to.Row][to.Column] = _pieceCapturedInPendingMove;

            // Setzt den "Pending"-Status zurück.
            PendingMove = null;
            _pieceCapturedInPendingMove = null;
            IsAwaitingMoveConfirmation = false;

            OnStateChanged();
        }

        // Bestätigt einen optimistischen Zug, indem der offizielle Zustand vom Server übernommen wird.
        public void ConfirmOptimisticMove(BoardDto serverBoard)
        {
            // Der Server hat immer recht. Wir überschreiben unseren Zustand mit der Wahrheit des Servers.
            BoardDto = serverBoard;

            // Setzt den "Pending"-Status zurück.
            PendingMove = null;
            _pieceCapturedInPendingMove = null;
            IsAwaitingMoveConfirmation = false;
            OnStateChanged();
        }

        // --- Methoden für den Spiel-Lebenszyklus ---

        // Setzt den "Extrazug"-Modus aktiv oder inaktiv.
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

        // Zählt die im Extrazug gemachten Züge hoch.
        public void IncrementExtraTurnMovesMade()
        {
            _extraTurnMovesMade++;
            OnStateChanged();
        }

        // Initialisiert den gesamten Zustand für ein neu erstelltes Spiel.
        public void InitializeNewGame(CreateGameResultDto result, CreateGameParameters args)
        {
            CurrentPlayerInfo = new PlayerDto(result.PlayerId, args.Name);
            MyColor = result.Color;
            BoardDto = result.Board;
            GameId = result.GameId;
            _isPvCGame = (args.OpponentType == OpponentType.Computer);
            OpponentJoined = _isPvCGame;

            var newPlayerNames = new Dictionary<Player, string>();
            newPlayerNames[MyColor] = args.Name;
            if (_isPvCGame)
            {
                Player computerColor = MyColor.Opponent();
                string computerName = $"Computer ({args.ComputerDifficulty})";
                newPlayerNames[computerColor] = computerName;
            }
            PlayerNames = newPlayerNames;

            CurrentTurnPlayer = Player.White;
            IsGameSpecificDataInitialized = false;
            EndGameMessage = "";
            UpdateDisplayedTimes(TimeSpan.FromMinutes(args.TimeMinutes), TimeSpan.FromMinutes(args.TimeMinutes), Player.White);
            _isGameRunning = false;
            SetExtraTurnSequenceActive(false);
            OnStateChanged();
        }

        // Initialisiert den Zustand, nachdem ein Spieler einem bestehenden Spiel beigetreten ist.
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

        // Verarbeitet die Game-ID aus der URL.
        public void SetGameIdFromQuery(string? gameIdQuery, bool isValidAndExists)
        {
            if (GameIdFromQueryString == gameIdQuery && IsGameIdFromQueryValidAndExists == isValidAndExists) return;
            GameIdFromQueryString = gameIdQuery;
            IsGameIdFromQueryValidAndExists = isValidAndExists;
            OnStateChanged();
        }

        // Aktualisiert die Namen beider Spieler.
        public void UpdatePlayerNames(Dictionary<Player, string> names)
        {
            PlayerNames = new Dictionary<Player, string>(names);
            OnStateChanged();
        }

        // Setzt den Namen für einen spezifischen Spieler (Weiss oder Schwarz).
        public void SetPlayerName(Player color, string name)
        {
            if (PlayerNames.TryGetValue(color, out var existingName) && existingName == name) return;
            PlayerNames[color] = name;
            OnStateChanged();
        }

        // Aktualisiert die Brettstellung mit einem neuen DTO vom Server.
        public void UpdateBoard(BoardDto newBoard)
        {
            // Verhindert ein Update, wenn gerade auf die Bestätigung eines optimistischen Zugs gewartet wird.
            if (IsAwaitingMoveConfirmation) return;

            BoardDto = newBoard;
            OnStateChanged();
        }

        // Setzt den Spieler, der aktuell am Zug ist.
        public void SetCurrentTurnPlayer(Player? player)
        {
            if (CurrentTurnPlayer == player) return;
            CurrentTurnPlayer = player;
            OnStateChanged();
        }

        // Setzt den Status, ob ein Gegner dem Spiel beigetreten ist.
        public void SetOpponentJoined(bool joined)
        {
            if (OpponentJoined == joined) return;
            OpponentJoined = joined;
            OnStateChanged();
        }

        // Setzt die Nachricht, die bei Spielende angezeigt wird.
        public void SetEndGameMessage(string message)
        {
            if (EndGameMessage == message) return;
            EndGameMessage = message;
            OnStateChanged();
        }

        // Leert die Endspiel-Nachricht.
        public void ClearEndGameMessage()
        {
            if (string.IsNullOrEmpty(EndGameMessage)) return;
            EndGameMessage = "";
            OnStateChanged();
        }

        // Setzt, ob spiel-spezifische Daten initialisiert wurden.
        public void SetGameSpecificDataInitialized(bool initialized)
        {
            if (IsGameSpecificDataInitialized == initialized) return;
            IsGameSpecificDataInitialized = initialized;
            OnStateChanged();
        }

        // Aktualisiert die angezeigten Bedenkzeiten.
        public void UpdateDisplayedTimes(TimeSpan whiteTime, TimeSpan blackTime, Player? activeTimerPlayer)
        {
            string newWhiteTimeDisplay = whiteTime.ToString(@"mm\:ss", CultureInfo.InvariantCulture);
            string newBlackTimeDisplay = blackTime.ToString(@"mm\:ss", CultureInfo.InvariantCulture);
            if (WhiteTimeDisplay == newWhiteTimeDisplay && BlackTimeDisplay == newBlackTimeDisplay) return;

            WhiteTimeDisplay = newWhiteTimeDisplay;
            BlackTimeDisplay = newBlackTimeDisplay;
            OnStateChanged();
        }

        // Setzt den gesamten Spielzustand für ein neues Spiel zurück.
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

            // Setzt auch den Optimistic UI State zurück.
            PendingMove = null;
            _pieceCapturedInPendingMove = null;
            IsAwaitingMoveConfirmation = false;

            UpdateDisplayedTimes(TimeSpan.FromMinutes(initialTimeMinutes), TimeSpan.FromMinutes(initialTimeMinutes), Player.White);
            OnStateChanged();
        }

        // Setzt, ob es sich um ein Spiel gegen den Computer handelt.
        public void SetIsPvCGame(bool isPvC)
        {
            if (_isPvCGame == isPvC) return;
            _isPvCGame = isPvC;
            OnStateChanged();
        }

        // Setzt, ob das Spiel gerade aktiv läuft (nach dem Countdown).
        public void SetGameRunning(bool isRunning)
        {
            if (_isGameRunning == isRunning) return;
            _isGameRunning = isRunning;
            OnStateChanged();
        }
    }
}