// File: [SolutionDir]\ChessNetwork\HttpGameSession.cs
using ChessLogic;
using ChessNetwork.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace ChessNetwork
{
    // Implementiert die IGameSession-Schnittstelle mittels HTTP-Anfragen an die Server-API.
    public class HttpGameSession : IGameSession
    {
        // Der HttpClient für die Kommunikation mit dem Server.
        private readonly HttpClient _http;

        // Wiederverwendbare JsonSerializerOptions für die Deserialisierung.
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

        // Konstruktor, der den HttpClient per Dependency Injection erhält.
        public HttpGameSession(HttpClient http) => _http = http;

        // Eine private Hilfsmethode, um HTTP-Antworten zentral und konsistent zu behandeln.
        // Wenn die Antwort keinen Erfolgsstatus-Code hat (z.B. 400 Bad Request),
        // wird der Fehlerinhalt aus dem Body der Antwort ausgelesen und eine aussagekräftige
        // HttpRequestException mit der spezifischen Server-Nachricht geworfen.
        private static async Task HandleResponseErrors(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(string.IsNullOrWhiteSpace(errorContent) ? response.ReasonPhrase : errorContent, null, response.StatusCode);
            }
        }

        // Sendet eine Anfrage zur Erstellung eines neuen Spiels an den Server.
        public async Task<CreateGameResultDto> CreateGameAsync(CreateGameDto createGameParameters)
        {
            var resp = await _http.PostAsJsonAsync("api/games", createGameParameters);
            await HandleResponseErrors(resp);
            var result = await resp.Content.ReadFromJsonAsync<CreateGameResultDto>(_jsonSerializerOptions);
            if (result is null)
                throw new InvalidOperationException("Kein CreateGameResultDto vom Server zurückgegeben.");
            return result;
        }

        // Sendet eine Anfrage, um einem bestehenden Spiel beizutreten.
        public async Task<JoinGameResultDto> JoinGameAsync(Guid gameId, string playerName)
        {
            var dto = new JoinDto(playerName);
            var resp = await _http.PostAsJsonAsync($"api/games/{gameId}/join", dto);
            await HandleResponseErrors(resp);
            var result = await resp.Content.ReadFromJsonAsync<JoinGameResultDto>(_jsonSerializerOptions);
            if (result is null)
                throw new InvalidOperationException("Kein JoinGameResultDto vom Server zurückgegeben.");
            return result;
        }

        // Ruft den aktuellen Zustand des Schachbretts vom Server ab.
        public async Task<BoardDto> GetBoardAsync(Guid gameId)
        {
            var resp = await _http.GetAsync($"api/games/{gameId}/state");
            await HandleResponseErrors(resp);
            var board = await resp.Content.ReadFromJsonAsync<BoardDto>(_jsonSerializerOptions);
            if (board is null)
                throw new InvalidOperationException("Kein Schachbrett (BoardDto) vom Server zurückgegeben.");
            return board;
        }

        // Sendet einen Zug an den Server zur Verarbeitung.
        public async Task<MoveResultDto> SendMoveAsync(Guid gameId, MoveDto move)
        {
            var resp = await _http.PostAsJsonAsync($"api/games/{gameId}/move", move);
            await HandleResponseErrors(resp);
            var result = await resp.Content.ReadFromJsonAsync<MoveResultDto>(_jsonSerializerOptions);
            if (result is null)
                throw new InvalidOperationException("Kein MoveResultDto vom Server zurückgegeben.");
            return result;
        }

        // Ruft grundlegende Informationen über ein Spiel vom Server ab.
        public async Task<GameInfoDto> GetGameInfoAsync(Guid gameId)
        {
            var resp = await _http.GetAsync($"api/games/{gameId}/info");
            await HandleResponseErrors(resp);
            var info = await resp.Content.ReadFromJsonAsync<GameInfoDto>(_jsonSerializerOptions);
            if (info is null)
                throw new InvalidOperationException("Kein GameInfoDto vom Server zurückgegeben.");
            return info;
        }

        // Ruft eine Liste der legalen Züge für eine Figur auf einem bestimmten Feld ab.
        public async Task<IEnumerable<string>> GetLegalMovesAsync(Guid gameId, string from, Guid playerId)
        {
            var resp = await _http.GetAsync($"api/games/{gameId}/legalmoves?from={from}&playerId={playerId}");
            await HandleResponseErrors(resp);
            var moves = await resp.Content.ReadFromJsonAsync<IEnumerable<string>>(_jsonSerializerOptions);
            return moves ?? Enumerable.Empty<string>();
        }

        // Ruft den aktuellen Spielstatus (Schach, Matt, etc.) für einen Spieler ab.
        public async Task<GameStatusDto> GetGameStatusAsync(Guid gameId, Guid playerId)
        {
            var resp = await _http.GetAsync($"api/games/{gameId}/status?playerId={playerId}");
            await HandleResponseErrors(resp);
            var status = await resp.Content.ReadFromJsonAsync<GameStatusDto?>(_jsonSerializerOptions);
            return status ?? GameStatusDto.None;
        }

        // Ruft den Spieler ab, der aktuell am Zug ist.
        public async Task<Player> GetCurrentTurnPlayerAsync(Guid gameId)
        {
            var resp = await _http.GetAsync($"api/games/{gameId}/currentplayer");
            await HandleResponseErrors(resp);
            var player = await resp.Content.ReadFromJsonAsync<Player?>(_jsonSerializerOptions);
            return player ?? Player.None;
        }

        // Ruft die aktuellen Bedenkzeiten beider Spieler ab.
        public async Task<TimeUpdateDto> GetTimeUpdateAsync(Guid gameId)
        {
            var resp = await _http.GetAsync($"api/games/{gameId}/time");
            await HandleResponseErrors(resp);
            var timeUpdate = await resp.Content.ReadFromJsonAsync<TimeUpdateDto>(_jsonSerializerOptions);
            if (timeUpdate is null)
                throw new InvalidOperationException("Kein TimeUpdateDto vom Server für GetTimeUpdateAsync zurückgegeben.");
            return timeUpdate;
        }

        // Sendet eine Anfrage zur Aktivierung einer Karte an den Server.
        public async Task<ServerCardActivationResultDto> ActivateCardAsync(Guid gameId, Guid playerId, ActivateCardRequestDto cardActivationRequest)
        {
            var resp = await _http.PostAsJsonAsync($"api/games/{gameId}/player/{playerId}/activatecard", cardActivationRequest);
            await HandleResponseErrors(resp);
            var result = await resp.Content.ReadFromJsonAsync<ServerCardActivationResultDto>(_jsonSerializerOptions);
            if (result is null)
                throw new InvalidOperationException("Kein ServerCardActivationResultDto vom Server zurückgegeben.");
            return result;
        }

        // Ruft die geschlagenen Figuren eines Spielers ab (nützlich für den "Wiedergeburt"-Effekt).
        public async Task<IEnumerable<CapturedPieceTypeDto>> GetCapturedPiecesAsync(Guid gameId, Guid playerId)
        {
            var resp = await _http.GetAsync($"api/games/{gameId}/player/{playerId}/capturedpieces");
            await HandleResponseErrors(resp);
            var capturedPieces = await resp.Content.ReadFromJsonAsync<IEnumerable<CapturedPieceTypeDto>>(_jsonSerializerOptions);
            return capturedPieces ?? Enumerable.Empty<CapturedPieceTypeDto>();
        }

        // Ruft Informationen über den Gegner ab.
        public async Task<OpponentInfoDto?> GetOpponentInfoAsync(Guid gameId, Guid playerId)
        {
            var resp = await _http.GetAsync($"api/games/{gameId}/player/{playerId}/opponentinfo");

            // Ein 404 Not Found ist hier ein erwartetes Szenario (wenn kein Gegner beigetreten ist) und kein Fehler.
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            // Für alle anderen Fehler wird die Standardbehandlung verwendet.
            await HandleResponseErrors(resp);
            return await resp.Content.ReadFromJsonAsync<OpponentInfoDto?>(_jsonSerializerOptions);
        }
    }
}