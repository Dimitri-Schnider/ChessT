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

        // Sendet eine Anfrage zur Erstellung eines neuen Spiels an den Server.
        public async Task<CreateGameResultDto> CreateGameAsync(CreateGameDto createGameParameters)
        {
            var resp = await _http.PostAsJsonAsync("api/games", createGameParameters);
            resp.EnsureSuccessStatusCode();
            var result = await resp.Content.ReadFromJsonAsync<CreateGameResultDto>();
            if (result is null)
                throw new InvalidOperationException("Kein CreateGameResultDto vom Server zurückgegeben.");
            return result;
        }

        // Sendet eine Anfrage, um einem bestehenden Spiel beizutreten.
        public async Task<JoinGameResultDto> JoinGameAsync(Guid gameId, string playerName)
        {
            var dto = new JoinDto(playerName);
            var resp = await _http.PostAsJsonAsync($"api/games/{gameId}/join", dto);
            resp.EnsureSuccessStatusCode();
            var result = await resp.Content.ReadFromJsonAsync<JoinGameResultDto>();
            if (result is null)
                throw new InvalidOperationException("Kein JoinGameResultDto vom Server zurückgegeben.");
            return result;
        }

        // Ruft den aktuellen Zustand des Schachbretts vom Server ab.
        public async Task<BoardDto> GetBoardAsync(Guid gameId)
        {
            var board = await _http.GetFromJsonAsync<BoardDto>($"api/games/{gameId}/state");
            if (board is null)
                throw new InvalidOperationException("Kein Schachbrett (BoardDto) vom Server zurückgegeben.");
            return board;
        }

        // Sendet einen Zug an den Server zur Verarbeitung.
        public async Task<MoveResultDto> SendMoveAsync(Guid gameId, MoveDto move)
        {
            var resp = await _http.PostAsJsonAsync($"api/games/{gameId}/move", move);
            resp.EnsureSuccessStatusCode();
            var result = await resp.Content.ReadFromJsonAsync<MoveResultDto>();
            if (result is null)
                throw new InvalidOperationException("Kein MoveResultDto vom Server zurückgegeben.");
            return result;
        }

        // Ruft grundlegende Informationen über ein Spiel vom Server ab.
        public async Task<GameInfoDto> GetGameInfoAsync(Guid gameId)
        {
            var info = await _http.GetFromJsonAsync<GameInfoDto>($"api/games/{gameId}/info");
            if (info is null)
                throw new InvalidOperationException("Kein GameInfoDto vom Server zurückgegeben.");
            return info;
        }

        // Ruft eine Liste der legalen Züge für eine Figur auf einem bestimmten Feld ab.
        public async Task<IEnumerable<string>> GetLegalMovesAsync(Guid gameId, string from, Guid playerId)
        {
            var moves = await _http.GetFromJsonAsync<IEnumerable<string>>($"api/games/{gameId}/legalmoves?from={from}&playerId={playerId}");
            return moves ?? Enumerable.Empty<string>();
        }

        // Ruft den aktuellen Spielstatus (Schach, Matt, etc.) für einen Spieler ab.
        public async Task<GameStatusDto> GetGameStatusAsync(Guid gameId, Guid playerId)
        {
            var status = await _http.GetFromJsonAsync<GameStatusDto?>($"api/games/{gameId}/status?playerId={playerId}");
            return status ?? GameStatusDto.None;
        }

        // Ruft den Spieler ab, der aktuell am Zug ist.
        public async Task<Player> GetCurrentTurnPlayerAsync(Guid gameId)
        {
            var player = await _http.GetFromJsonAsync<Player?>($"api/games/{gameId}/currentplayer");
            return player ?? Player.None;
        }

        // Ruft die aktuellen Bedenkzeiten beider Spieler ab.
        public async Task<TimeUpdateDto> GetTimeUpdateAsync(Guid gameId)
        {
            var timeUpdate = await _http.GetFromJsonAsync<TimeUpdateDto>($"api/games/{gameId}/time");
            if (timeUpdate is null)
                throw new InvalidOperationException("Kein TimeUpdateDto vom Server für GetTimeUpdateAsync zurückgegeben.");
            return timeUpdate;
        }

        // Sendet eine Anfrage zur Aktivierung einer Karte an den Server.
        public async Task<ServerCardActivationResultDto> ActivateCardAsync(Guid gameId, Guid playerId, ActivateCardRequestDto cardActivationRequest)
        {
            var resp = await _http.PostAsJsonAsync($"api/games/{gameId}/player/{playerId}/activatecard", cardActivationRequest);
            string rawResponseContent = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                if (!string.IsNullOrWhiteSpace(rawResponseContent))
                {
                    try
                    {
                        var errorResult = JsonSerializer.Deserialize<ServerCardActivationResultDto>(rawResponseContent, _jsonSerializerOptions);
                        if (errorResult != null && !string.IsNullOrEmpty(errorResult.ErrorMessage))
                        {
                            throw new HttpRequestException(errorResult.ErrorMessage, null, resp.StatusCode);
                        }
                    }
                    catch (JsonException) { /* Ignorieren */ }
                }
                throw new HttpRequestException(string.IsNullOrWhiteSpace(rawResponseContent) ? $"Fehler bei Kartenaktivierung (HTTP): {resp.StatusCode}" : rawResponseContent, null, resp.StatusCode);
            }

            try
            {
                var result = JsonSerializer.Deserialize<ServerCardActivationResultDto>(rawResponseContent, _jsonSerializerOptions);
                if (result is null)
                    throw new InvalidOperationException("Kein ServerCardActivationResultDto vom Server zurückgegeben.");
                return result;
            }
            catch (JsonException jsonEx)
            {
                throw new InvalidOperationException($"Fehler beim Deserialisieren der Server-Antwort: {jsonEx.Message}", jsonEx);
            }
        }

        // Ruft die geschlagenen Figuren eines Spielers ab (nützlich für den "Wiedergeburt"-Effekt).
        public async Task<IEnumerable<CapturedPieceTypeDto>> GetCapturedPiecesAsync(Guid gameId, Guid playerId)
        {
            var capturedPieces = await _http.GetFromJsonAsync<IEnumerable<CapturedPieceTypeDto>>($"api/games/{gameId}/player/{playerId}/capturedpieces");
            return capturedPieces ?? Enumerable.Empty<CapturedPieceTypeDto>();
        }

        // Ruft Informationen über den Gegner ab.
        public async Task<OpponentInfoDto?> GetOpponentInfoAsync(Guid gameId, Guid playerId)
        {
            try
            {
                return await _http.GetFromJsonAsync<OpponentInfoDto?>($"api/games/{gameId}/player/{playerId}/opponentinfo");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Es ist ein erwartbares Szenario, dass kein Gegner gefunden wird (z.B. wenn noch keiner beigetreten ist).
                return null;
            }
        }
    }
}