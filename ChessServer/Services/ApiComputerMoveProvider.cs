using Chess.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ChessServer.Services
{
    public class ApiComputerMoveProvider : IComputerMoveProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IChessLogger _logger;

        // DTO für die Deserialisierung der API-Antwort
        private sealed class ChessApiResponseDto
        {
            public string? Move { get; set; }
        }

        public ApiComputerMoveProvider(IHttpClientFactory httpClientFactory, IChessLogger logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<string?> GetNextMoveAsync(Guid gameId, string fen, int depth)
        {
            var client = _httpClientFactory.CreateClient("ChessApi");
            var requestBody = new { fen, depth };

            try
            {
                _logger.LogComputerFetchingMove(gameId, fen, depth);
                HttpResponseMessage response = await client.PostAsJsonAsync("https://chess-api.com/v1", requestBody);

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = await response.Content.ReadFromJsonAsync<ChessApiResponseDto>();
                    if (apiResponse != null && !string.IsNullOrEmpty(apiResponse.Move))
                    {
                        _logger.LogComputerReceivedMove(gameId, apiResponse.Move, fen, depth);
                        return apiResponse.Move;
                    }
                    _logger.LogComputerMoveError(gameId, fen, depth, "API-Antwort erfolgreich, aber kein Zug gefunden.");
                    return null;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogComputerMoveError(gameId, fen, depth, $"API-Anfrage fehlgeschlagen mit Status {response.StatusCode}: {errorContent}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogComputerMoveError(gameId, fen, depth, $"Exception während API-Aufruf: {ex.Message}");
                return null;
            }
        }
    }
}