using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using ChessLogic;
using ChessNetwork.DTOs;

namespace ChessNetwork
{
    public class HttpGameSession : IGameSession
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        public HttpGameSession(HttpClient http) => _http = http;

        public async Task<ServerCardActivationResultDto> ActivateCardAsync(Guid gameId, Guid playerId, ActivateCardRequestDto cardActivationRequest)
        {
            var resp = await _http.PostAsJsonAsync($"api/games/{gameId}/player/{playerId}/activatecard", cardActivationRequest);
            string rawResponseContent = string.Empty;
            try
            {
                rawResponseContent = await resp.Content.ReadAsStringAsync();
                Console.WriteLine($"[HttpGameSession.ActivateCardAsync] GameID: {gameId}, HTTP Status: {resp.StatusCode}, Raw Response: {rawResponseContent}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HttpGameSession.ActivateCardAsync] GameID: {gameId}, HTTP Status: {resp.StatusCode}, Error reading raw response: {ex.Message}");
            }

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
                    catch (JsonException)
                    {
                    }
                }
                throw new HttpRequestException(string.IsNullOrWhiteSpace(rawResponseContent) ? $"Fehler bei Kartenaktivierung (HTTP): {resp.StatusCode}" : rawResponseContent, null, resp.StatusCode);
            }

            if (string.IsNullOrWhiteSpace(rawResponseContent))
            {
                Console.WriteLine($"[HttpGameSession.ActivateCardAsync] GameID: {gameId}, Raw response content was empty for successful status code. Throwing error.");
                throw new InvalidOperationException("Server sendete eine leere Erfolgsantwort für Kartenaktivierung.");
            }

            try
            {
                var result = JsonSerializer.Deserialize<ServerCardActivationResultDto>(rawResponseContent, _jsonSerializerOptions);
                if (result is null)
                {
                    Console.WriteLine($"[HttpGameSession.ActivateCardAsync] GameID: {gameId}, Deserialization to ServerCardActivationResultDto resulted in null. Raw content was: {rawResponseContent}");
                    throw new InvalidOperationException("Kein ServerCardActivationResultDto vom Server zurückgegeben oder Deserialisierung fehlgeschlagen (Ergebnis war null).");
                }
                Console.WriteLine($"[HttpGameSession.ActivateCardAsync] GameID: {gameId}, Deserialized Success: {result.Success}, Msg: {result.ErrorMessage}");
                return result;
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"[HttpGameSession.ActivateCardAsync] GameID: {gameId}, JSON Deserialization Error: {jsonEx.Message}. Raw content was: {rawResponseContent}");
                throw new InvalidOperationException($"Fehler beim Deserialisieren der Server-Antwort: {jsonEx.Message}", jsonEx);
            }
        }
        public async Task<CreateGameResultDto> CreateGameAsync(string playerName, Player color, int initialMinutes)
        {
            var dto = new CreateGameDto { PlayerName = playerName, Color = color, InitialMinutes = initialMinutes };
            var resp = await _http.PostAsJsonAsync("api/games", dto);
            resp.EnsureSuccessStatusCode();
            var result = await resp.Content.ReadFromJsonAsync<CreateGameResultDto>();
            if (result is null)
                throw new InvalidOperationException("Kein CreateGameResultDto vom Server zurückgegeben.");
            return result;
        }

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

        public async Task<BoardDto> GetBoardAsync(Guid gameId)
        {
            var board = await _http.GetFromJsonAsync<BoardDto>($"api/games/{gameId}/state");
            if (board is null)
                throw new InvalidOperationException("Kein Schachbrett (BoardDto) vom Server zurückgegeben.");
            return board;
        }

        public async Task<MoveResultDto> SendMoveAsync(Guid gameId, MoveDto move)
        {
            var resp = await _http.PostAsJsonAsync($"api/games/{gameId}/move", move);
            string jsonResponse = "Error reading response";
            try
            {
                jsonResponse = await resp.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HttpGameSession] Error reading response string: {ex.Message}");
            }

            resp.EnsureSuccessStatusCode();
            var result = await resp.Content.ReadFromJsonAsync<MoveResultDto>();
            if (result is null)
                throw new InvalidOperationException("Kein MoveResultDto vom Server zurückgegeben.");
            return result;
        }

        public async Task<GameInfoDto> GetGameInfoAsync(Guid gameId)
        {
            var info = await _http.GetFromJsonAsync<GameInfoDto>($"api/games/{gameId}/info");
            if (info is null)
                throw new InvalidOperationException("Kein GameInfoDto vom Server zurückgegeben.");
            return info;
        }

        public async Task<IEnumerable<string>> GetLegalMovesAsync(Guid gameId, string from, Guid playerId)
        {
            var moves = await _http.GetFromJsonAsync<IEnumerable<string>>($"api/games/{gameId}/legalmoves?from={from}&playerId={playerId}");
            return moves ?? Enumerable.Empty<string>();
        }

        public async Task<GameStatusDto> GetGameStatusAsync(Guid gameId, Guid playerId)
        {
            var status = await _http.GetFromJsonAsync<GameStatusDto?>($"api/games/{gameId}/status?playerId={playerId}");
            return status ?? GameStatusDto.None;
        }

        public async Task<Player> GetCurrentTurnPlayerAsync(Guid gameId)
        {
            var player = await _http.GetFromJsonAsync<Player?>($"api/games/{gameId}/currentplayer");
            return player ?? Player.None;
        }

        public async Task<TimeUpdateDto> GetTimeUpdateAsync(Guid gameId)
        {
            var timeUpdate = await _http.GetFromJsonAsync<TimeUpdateDto>($"api/games/{gameId}/time");
            if (timeUpdate is null)
                throw new InvalidOperationException("Kein TimeUpdateDto vom Server für GetTimeUpdateAsync zurückgegeben.");
            return timeUpdate;
        }

        public async Task<IEnumerable<CapturedPieceTypeDto>> GetCapturedPiecesAsync(Guid gameId, Guid playerId)
        {
            var capturedPieces = await _http.GetFromJsonAsync<IEnumerable<CapturedPieceTypeDto>>($"api/games/{gameId}/player/{playerId}/capturedpieces");
            return capturedPieces ?? Enumerable.Empty<CapturedPieceTypeDto>();
        }

        public async Task<OpponentInfoDto?> GetOpponentInfoAsync(Guid gameId, Guid playerId)
        {
            try
            {
                var opponentInfo = await _http.GetFromJsonAsync<OpponentInfoDto?>($"api/games/{gameId}/player/{playerId}/opponentinfo");
                return opponentInfo;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine($"Opponent info not found for game {gameId}, player {playerId}: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching opponent info for game {gameId}, player {playerId}: {ex.Message}");
                throw;
            }
        }
    }
}