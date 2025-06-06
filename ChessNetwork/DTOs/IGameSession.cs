using ChessLogic;
using ChessNetwork.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChessNetwork
{
    // Definiert den Vertrag für die Interaktion eines Clients mit einer Spielsitzung auf dem Server.
    public interface IGameSession
    {
        // Erstellt ein neues Spiel mit den angegebenen Parametern.
        Task<CreateGameResultDto> CreateGameAsync(CreateGameDto createGameParameters);

        // Tritt einem bestehenden Spiel bei.
        Task<JoinGameResultDto> JoinGameAsync(Guid gameId, string playerName);

        // Ruft den aktuellen Zustand des Schachbretts ab.
        Task<BoardDto> GetBoardAsync(Guid gameId);

        // Sendet einen Zug an den Server zur Verarbeitung.
        Task<MoveResultDto> SendMoveAsync(Guid gameId, MoveDto move);

        // Ruft grundlegende Informationen über ein Spiel ab.
        Task<GameInfoDto> GetGameInfoAsync(Guid gameId);

        // Ruft eine Liste der legalen Züge für eine Figur auf einem bestimmten Feld ab.
        Task<IEnumerable<string>> GetLegalMovesAsync(Guid gameId, string from, Guid playerId);

        // Ruft den aktuellen Spielstatus (Schach, Matt, etc.) für einen Spieler ab.
        Task<GameStatusDto> GetGameStatusAsync(Guid gameId, Guid playerId);

        // Ruft den Spieler ab, der aktuell am Zug ist.
        Task<Player> GetCurrentTurnPlayerAsync(Guid gameId);

        // Ruft die aktuellen Bedenkzeiten beider Spieler ab.
        Task<TimeUpdateDto> GetTimeUpdateAsync(Guid gameId);

        // Sendet eine Anfrage zur Aktivierung einer Karte.
        Task<ServerCardActivationResultDto> ActivateCardAsync(Guid gameId, Guid playerId, ActivateCardRequestDto cardActivationRequest);

        // Ruft die geschlagenen Figuren eines Spielers ab (für den "Wiedergeburt"-Effekt).
        Task<IEnumerable<CapturedPieceTypeDto>> GetCapturedPiecesAsync(Guid gameId, Guid playerId);

        // Ruft Informationen über den Gegner ab.
        Task<OpponentInfoDto?> GetOpponentInfoAsync(Guid gameId, Guid playerId);
    }
}