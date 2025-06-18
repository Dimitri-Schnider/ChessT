using ChessLogic;
using ChessNetwork.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChessServer.Services.Management
{
    // Definiert den Vertrag für einen Dienst, der alle laufenden Spielsitzungen verwaltet.
    public interface IGameManager
    {
        // Aktiviert den Effekt einer ausgewählten Karte.
        Task<ServerCardActivationResultDto> ActivateCardEffect(Guid gameId, Guid playerId, ActivateCardRequestDto cardActivationRequestDto);

        // Wendet einen Zug auf eine bestimmte Spielsitzung an.
        MoveResultDto ApplyMove(Guid gameId, MoveDto move, Guid playerId);

        // Erstellt ein neues Spiel und gibt die IDs für Spiel und Spieler zurück.
        (Guid GameId, Guid PlayerId) CreateGame(string playerName, Player color, int initialMinutes, OpponentType opponentType = OpponentType.Human, ComputerDifficulty computerDifficulty = ComputerDifficulty.Medium);

        // Ruft alle geschlagenen Figuren für einen bestimmten Spieler ab.
        Task<IEnumerable<CapturedPieceTypeDto>> GetCapturedPieces(Guid gameId, Guid playerId);

        // Ruft den Spieler ab, der aktuell am Zug ist.
        Player GetCurrentTurnPlayer(Guid gameId);

        // Ruft den detaillierten Spielverlauf ab.
        GameHistoryDto GetGameHistory(Guid gameId);

        // Ruft grundlegende Informationen über ein Spiel ab.
        GameInfoDto GetGameInfo(Guid gameId);

        // Ruft den Spielstatus (z.B. Schach, Matt) für einen bestimmten Spieler ab.
        GameStatusDto GetGameStatus(Guid gameId, Guid playerId);

        // Ruft den Spielstatus für den Gegner des zuletzt ziehenden Spielers ab.
        GameStatusDto GetGameStatusForOpponentOf(Guid gameId, Guid lastPlayerId);

        // Ruft alle legalen Züge für eine Figur auf einem bestimmten Feld ab.
        IEnumerable<string> GetLegalMoves(Guid gameId, Guid playerId, string from);

        // Ruft Informationen über den Gegner des aktuellen Spielers ab.
        OpponentInfoDto? GetOpponentInfo(Guid gameId, Guid currentPlayerId);

        // Ruft die Farbe eines Spielers anhand seiner ID ab.
        Player GetPlayerColor(Guid gameId, Guid playerId);

        // Ruft den Namen eines Spielers anhand seiner ID ab.
        string? GetPlayerName(Guid gameId, Guid playerId);

        // Ruft die ID eines Spielers anhand seiner Farbe ab.
        Guid? GetPlayerIdByColor(Guid gameId, Player color);

        // Ruft die Anzahl der Karten im Nachziehstapel eines Spielers ab.
        int GetDrawPileCount(Guid gameId, Guid playerId);

        // Ruft die Handkarten eines Spielers ab.
        List<CardDto> GetPlayerHand(Guid gameId, Guid playerId);

        // Ruft den aktuellen Brettzustand (Figurenpositionen) ab.
        BoardDto GetState(Guid gameId);

        // Ruft die aktuellen Bedenkzeiten beider Spieler ab.
        TimeUpdateDto GetTimeUpdate(Guid gameId);

        // Ermöglicht einem Spieler, einem bestehenden Spiel beizutreten.
        (Guid PlayerId, Player Color) JoinGame(Guid gameId, string playerName);

        // Registriert die SignalR-Verbindung eines Spielers.
        void RegisterPlayerHubConnection(Guid gameId, Guid playerId, string connectionId);

        // Deregistriert die SignalR-Verbindung eines Spielers.
        void UnregisterPlayerHubConnection(Guid gameId, string connectionId);

        // Startet ein bestehendes Spiel, wenn beide Spieler bereit sind.
        void StartGame(Guid gameId);
    }
}