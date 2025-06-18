using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System;
using ChessLogic;
using ChessNetwork.DTOs;
using ChessServer.Hubs;
using System.Collections.Generic;

namespace ChessServer.Tests
{
    // Integrationstests, die den gesamten Spielfluss von der Erstellung bis zu einem Zug testen.
    // Der Fokus liegt hierbei auf der korrekten Interaktion zwischen dem Controller und dem SignalR Hub.
    public class GameFlowIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> factory;
        private readonly JsonSerializerOptions jsonOptions;

        // Mocks für den SignalR Hub, um das Senden von Nachrichten zu simulieren und zu verifizieren.
        private readonly Mock<IHubContext<ChessHub>> mockHubContext;
        private readonly Mock<IHubClients> mockClients;
        private readonly Mock<IClientProxy> mockClientProxy;

        // Konstruktor: Richtet die WebApplicationFactory und die SignalR-Mocks ein.
        public GameFlowIntegrationTests(WebApplicationFactory<Program> factory)
        {
            mockHubContext = new Mock<IHubContext<ChessHub>>();
            mockClients = new Mock<IHubClients>();
            mockClientProxy = new Mock<IClientProxy>();

            // Richten Sie die Mock-Kette für SignalR ein: HubContext -> Clients -> Group -> SendAsync
            mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
            mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);

            // Konfiguriert die WebApplicationFactory so, dass sie unseren Mock anstelle des echten HubContext verwendet.
            // Dies ist ein Kernprinzip von Integrationstests: Externe Abhängigkeiten werden durch Mocks ersetzt.
            this.factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Ersetze die echte IHubContext-Registrierung durch unseren Mock.
                    services.AddSingleton(mockHubContext.Object);
                });
            });

            jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };
        }

        // Testfall: Simuliert einen kompletten Spielfluss und verifiziert, dass ein Zug eine SignalR-Nachricht auslöst.
        [Fact]
        public async Task FullGameFlowWithMoveTriggersSignalRBroadcast()
        {
            // Arrange: Führt die Schritte zum Aufsetzen eines spielbereiten Zustands aus.
            var client = factory.CreateClient();

            // 1. Spiel erstellen
            var createDto = new CreateGameDto { PlayerName = "Spieler 1 (Weiss)", Color = Player.White, InitialMinutes = 5 };
            var createResponse = await client.PostAsJsonAsync("/api/games", createDto, jsonOptions);
            createResponse.EnsureSuccessStatusCode();
            var createResult = await createResponse.Content.ReadFromJsonAsync<CreateGameResultDto>(jsonOptions);
            Assert.NotNull(createResult);

            var gameId = createResult.GameId;
            var player1Id = createResult.PlayerId;

            // 2. Gegner tritt bei
            var joinDto = new JoinDto("Spieler 2 (Schwarz)");
            await client.PostAsJsonAsync($"/api/games/{gameId}/join", joinDto, jsonOptions);

            // 3. Einen Zug vorbereiten
            var moveDto = new MoveDto("e2", "e4", player1Id, null);

            // Act: Führt den Zug aus.
            var moveResponse = await client.PostAsJsonAsync($"/api/games/{gameId}/move", moveDto, jsonOptions);
            moveResponse.EnsureSuccessStatusCode();

            // Assert: Überprüft das Kernverhalten.
            // Wir verifizieren, dass der Controller am Ende seiner Logik den Hub aufruft,
            // um die Clients über den neuen Zug zu informieren.
            mockClientProxy.Verify(
                x => x.SendCoreAsync(
                    "OnTurnChanged", // Der Name der SignalR-Methode auf dem Client.
                    It.Is<object?[]>(o =>
                        o != null &&
                        o.Length == 6 && // Wir erwarten 6 Argumente.
                        o[1]!.ToString() == Player.Black.ToString() // Das 2. Argument (nextPlayer) sollte Schwarz sein.
                    ),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once // Der Aufruf muss genau einmal erfolgt sein.
            );
        }
    }
}