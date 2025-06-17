using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ChessLogic;
using ChessNetwork.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Xunit.Abstractions;

namespace ChessServer.Tests
{
    // Die Klasse implementiert jetzt IClassFixture, um die Factory zu erhalten
    // und braucht IDisposable NICHT mehr.
    public class GamesControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly ITestOutputHelper _output;
        private readonly WebApplicationFactory<Program> _factory;
        private readonly JsonSerializerOptions _jsonOptions;

        // Der Konstruktor erhält jetzt die Factory vom Test-Framework
        public GamesControllerTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
        {
            _factory = factory;
            _output = output;

            // Der Client wird jetzt von der Factory erstellt
            _client = _factory.CreateClient();

            // JSON-Optionen konfigurieren, damit sie mit dem Server übereinstimmen
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };
        }

        [Fact]
        public async Task MakeMoveWhenCalledInParallelForSameTurnOnlyOneSucceeds()
        {

            // --- ARRANGE: Spiel aufsetzen ---
            _output.WriteLine("Phase 1: Spiel wird erstellt...");
            var createDto = new CreateGameDto { PlayerName = "Spieler A (Weiss)", Color = Player.White, InitialMinutes = 5 };
            var createResponse = await _client.PostAsJsonAsync("/api/games", createDto, _jsonOptions);

            if (!createResponse.IsSuccessStatusCode)
            {
                var error = await createResponse.Content.ReadAsStringAsync();
                _output.WriteLine($"Fehler beim Erstellen des Spiels: {error}");
                Assert.Fail("Test-Setup fehlgeschlagen: Konnte kein Spiel erstellen.");
            }

            var createResult = await createResponse.Content.ReadFromJsonAsync<CreateGameResultDto>(_jsonOptions);
            if (createResult == null)
            {
                Assert.Fail("Die Server-Antwort beim Erstellen des Spiels konnte nicht gelesen werden.");
                return;
            }

            var gameId = createResult.GameId;
            var playerAId = createResult.PlayerId;
            _output.WriteLine($"Spiel {gameId} erstellt. Spieler A (Weiss) hat die ID {playerAId}.");
            var joinDto = new JoinDto("Spieler B (Schwarz)");
            var joinResponse = await _client.PostAsJsonAsync($"/api/games/{gameId}/join", joinDto, _jsonOptions);
            joinResponse.EnsureSuccessStatusCode();
            _output.WriteLine("Spieler B (Schwarz) ist dem Spiel beigetreten.");

            var moveDto = new MoveDto("e2", "e4", playerAId, null);
            _output.WriteLine($"Bereite parallele Anfragen für den Zug e2-e4 vor...");

            // --- ACT: Züge parallel ausführen ---
            var task1 = _client.PostAsJsonAsync($"/api/games/{gameId}/move", moveDto, _jsonOptions);
            var task2 = _client.PostAsJsonAsync($"/api/games/{gameId}/move", moveDto, _jsonOptions);
            var responses = await Task.WhenAll(task1, task2);
            _output.WriteLine("Beide Anfragen wurden abgeschlossen.");
            // --- ASSERT: Ergebnisse prüfen ---
            _output.WriteLine("Phase 3: Ergebnisse werden überprüft...");
            var successfulResponses = responses.Count(r => r.IsSuccessStatusCode);
            _output.WriteLine($"Erfolgreiche Antworten (Status 2xx): {successfulResponses}");

            var failedResponses = responses.Count(r => !r.IsSuccessStatusCode);
            _output.WriteLine($"Fehlgeschlagene Antworten: {failedResponses}");
            var failedRequest = responses.FirstOrDefault(r => !r.IsSuccessStatusCode);
            if (failedRequest != null)
            {
                var errorBody = await failedRequest.Content.ReadFromJsonAsync<MoveResultDto>(_jsonOptions);
                _output.WriteLine($"Fehlermeldung des abgelehnten Requests: '{errorBody?.ErrorMessage}'");
                Assert.Equal("Nicht dein Zug.", errorBody?.ErrorMessage);
            }

            Assert.Equal(1, successfulResponses);
            Assert.Equal(1, failedResponses);
            _output.WriteLine("Test erfolgreich! Die Race Condition wurde korrekt behandelt.");
        }
    }
}