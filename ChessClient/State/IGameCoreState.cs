using System;
using System.Collections.Generic;
using ChessLogic;
using ChessNetwork.DTOs;
using ChessClient.Models;

namespace ChessClient.State
{

    // Definiert den Vertrag für den zentralen State-Container, der den Kernzustand des Spiels verwaltet.
    // Diese Schnittstelle kapselt alle fundamentalen Informationen, die für die Darstellung und Logik des Spiels notwendig sind,
    // wie die Brettstellung, Spielerinformationen, Timer und den allgemeinen Spielstatus.

    public interface IGameCoreState
    {
    
        // Event, das ausgelöst wird, wenn sich ein beliebiger Wert in diesem State ändert.
        // UI-Komponenten abonnieren dieses Event, um bei Zustandsänderungen neu zu rendern.
        event Action? StateChanged;
    
        PlayerDto? CurrentPlayerInfo { get; }                   // Enthält die Informationen des aktuell spielenden Clients (eigene Spieler-ID und Name).
        BoardDto? BoardDto { get; }                             // Das Datenübertragungsobjekt (DTO), das die aktuelle Stellung aller Figuren auf dem Schachbrett enthält.
        Player MyColor { get; }                                 // Die Farbe (Weiss oder Schwarz), die der lokale Spieler in der aktuellen Partie hat.
        Guid GameId { get; }                                    // Die eindeutige ID des aktuellen Spiels.
        string? GameIdFromQueryString { get; }                  // Die aus der URL-Query ausgelesene Spiel-ID, falls vorhanden.
        bool IsGameIdFromQueryValidAndExists { get; }           // Gibt an, ob die aus der URL-Query gelesene Spiel-ID auf dem Server existiert und gültig ist.
        Player? CurrentTurnPlayer { get; }                      // Der Spieler, der aktuell am Zug ist.
        bool OpponentJoined { get; }                            // Gibt an, ob der Gegner dem Spiel beigetreten ist.
        string EndGameMessage { get; }                          // Eine Nachricht, die das Ergebnis des Spiels beschreibt (z.B. "Schachmatt! Du hast gewonnen."). Ist leer, solange das Spiel läuft.
        Dictionary<Player, string> PlayerNames { get; }         // Ein Dictionary, das die Spielernamen den jeweiligen Farben zuordnet
        bool IsGameSpecificDataInitialized { get; }             // Gibt an, ob die spiel-spezifischen Daten (wie Gegnername) bereits initialisiert wurden.
        string WhiteTimeDisplay { get; }                        // Die formatierte, anzuzeigende Bedenkzeit für Spieler Weiss (z.B. "14:55").
        string BlackTimeDisplay { get; }                        // Die formatierte, anzuzeigende Bedenkzeit für Spieler Schwarz.
        bool IsPvCGame { get; }                                 // Gibt an, ob es sich um ein Spiel gegen den Computer handelt (Player vs. Computer).
        bool IsGameRunning { get; }                             // Gibt an, ob das Spiel aktiv läuft (d.h. der Initialisierungs-Countdown ist vorbei).
        bool IsExtraTurnSequenceActive { get; }                 // Gibt an, ob gerade eine "Extrazug"-Sequenz aktiv ist.
        int ExtraTurnMovesMade { get; }                         // Zählt die Anzahl der Züge, die während einer "Extrazug"-Sequenz gemacht wurden.
        bool IsAwaitingMoveConfirmation { get; }                // Gibt an, ob der Client auf eine Bestätigung eines optimistischen Zugs vom Server wartet.
        MoveDto? PendingMove { get; }                           // Speichert den optimistisch ausgeführten Zug, während auf die Server-Antwort gewartet wird.
        void SetExtraTurnSequenceActive(bool isActive);         // Aktiviert oder deaktiviert den "Extrazug"-Modus.
        void IncrementExtraTurnMovesMade();                     // Erhöht den Zähler für gemachte Züge während einer "Extrazug"-Sequenz.
        void InitializeNewGame(CreateGameResultDto result, CreateGameDto args);                 // Initialisiert den Zustand für ein neu erstelltes Spiel.
        void InitializeJoinedGame(JoinGameResultDto result, Guid gameId, Player assignedColor); // Initialisiert den Zustand, nachdem der Spieler einem bestehenden Spiel beigetreten ist.
        void SetGameIdFromQuery(string? gameIdQuery, bool isValidAndExists);                    // Setzt die aus der URL-Query gelesene Spiel-ID und deren Gültigkeitsstatus.
        void UpdatePlayerNames(Dictionary<Player, string> names);                               // Aktualisiert die Namen beider Spieler.
        void SetPlayerName(Player color, string name);          // Setzt den Namen für einen spezifischen Spieler (Weiss oder Schwarz).
        void UpdateBoard(BoardDto newBoard);                    // Aktualisiert die Brettstellung mit einem neuen DTO vom Server.
        void SetCurrentTurnPlayer(Player? player);              // Setzt den Spieler, der aktuell am Zug ist.
        void SetOpponentJoined(bool joined);                    // Setzt den Status, ob ein Gegner dem Spiel beigetreten ist.
        void SetEndGameMessage(string message);                 // Setzt die Nachricht, die bei Spielende angezeigt wird.
        void ClearEndGameMessage();                             // Leert die Endspiel-Nachricht, z.B. wenn ein neues Spiel gestartet wird.
        void SetGameSpecificDataInitialized(bool initialized);  // Setzt, ob spiel-spezifische Daten wie der Gegnername initialisiert wurden.
        void UpdateDisplayedTimes(TimeSpan whiteTime, TimeSpan blackTime, Player? activeTimerPlayer);   // Aktualisiert die angezeigten Bedenkzeiten.
        void ResetForNewGame(int initialTimeMinutes = 15);      // Setzt den gesamten Spielzustand für ein neues Spiel zurück.
        void SetIsPvCGame(bool isPvC);                          // Setzt, ob es sich um ein Spiel gegen den Computer handelt.
        void SetGameRunning(bool isRunning);                    // Setzt, ob das Spiel gerade aktiv läuft (nach dem Countdown).
        void ApplyOptimisticMove(MoveDto move);                 // Führt einen Zug optimistisch auf dem Client aus, bevor die Server-Bestätigung eintrifft.
        void RevertOptimisticMove();                            // Macht einen optimistischen Zug rückgängig, falls der Server ihn ablehnt.
        void ConfirmOptimisticMove(BoardDto serverBoard);       // Bestätigt einen optimistischen Zug, indem der offizielle Zustand vom Server übernommen wird.
    }
}