using System;
using System.Collections.Generic;
using ChessLogic;
using ChessNetwork.DTOs;
using ChessClient.Models;
namespace ChessClient.State
{
    public interface IGameCoreState
    {
        event Action? StateChanged;
        PlayerDto? CurrentPlayerInfo { get; }
        BoardDto? BoardDto { get; }
        Player MyColor { get; }
        Guid GameId { get; }
        string? GameIdFromQueryString { get; }
        bool IsGameIdFromQueryValidAndExists { get; }
        Player? CurrentTurnPlayer { get; }
        bool OpponentJoined { get; }
        string EndGameMessage { get; }
        Dictionary<Player, string> PlayerNames { get; }
        bool IsGameSpecificDataInitialized { get; }
        string WhiteTimeDisplay { get; }
        string BlackTimeDisplay { get; }
        bool IsPvCGame { get; }
        bool IsGameRunning { get; }
        // --- NEUE PROPERTIES FÜR OPTIMISTIC UI ---
        bool IsAwaitingMoveConfirmation { get; }
        MoveDto? PendingMove { get; }

        bool IsExtraTurnSequenceActive { get; }
        int ExtraTurnMovesMade { get; }
        void SetExtraTurnSequenceActive(bool isActive);
        void IncrementExtraTurnMovesMade();

        void InitializeNewGame(CreateGameResultDto result, CreateGameParameters args);
        void InitializeJoinedGame(JoinGameResultDto result, Guid gameId, Player assignedColor);
        void SetGameIdFromQuery(string? gameIdQuery, bool isValidAndExists);
        void UpdatePlayerNames(Dictionary<Player, string> names);
        void SetPlayerName(Player color, string name);
        void UpdateBoard(BoardDto newBoard);
        void SetCurrentTurnPlayer(Player? player);
        void SetOpponentJoined(bool joined);
        void SetEndGameMessage(string message);
        void ClearEndGameMessage();
        void SetGameSpecificDataInitialized(bool initialized);
        void UpdateDisplayedTimes(TimeSpan whiteTime, TimeSpan blackTime, Player? activeTimerPlayer);
        void ResetForNewGame(int initialTimeMinutes = 15);
        void SetIsPvCGame(bool isPvC);
        void SetGameRunning(bool isRunning);
        // --- NEUE METHODEN FÜR OPTIMISTIC UI ---
        void ApplyOptimisticMove(MoveDto move);
        void RevertOptimisticMove();
        void ConfirmOptimisticMove(BoardDto serverBoard);
    }
}