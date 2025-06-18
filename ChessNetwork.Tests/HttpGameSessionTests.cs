using ChessLogic;
using ChessNetwork.DTOs;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ChessNetwork.Tests
{
    // Testklasse für HttpGameSession, die die Kommunikation mit der Server-API über HTTP abbildet.
    // Hier wird der HttpMessageHandler gemockt, um Server-Antworten zu simulieren, ohne einen echten Server zu benötigen.
    public class HttpGameSessionTests : IDisposable
    {
        private readonly Mock<HttpMessageHandler> mockMessageHandler;
        private readonly HttpClient httpClient;
        private readonly HttpGameSession gameSession;

        // Konstruktor: Richtet die Mocks und die zu testende gameSession-Instanz für jeden Testfall ein.
        public HttpGameSessionTests()
        {
            mockMessageHandler = new Mock<HttpMessageHandler>();
            httpClient = new HttpClient(mockMessageHandler.Object)
            {
                // Eine Basis-URL wird gesetzt, damit die relativen Pfade in den API-Aufrufen korrekt aufgelöst werden.
                BaseAddress = new Uri("http://testserver/")
            };
            gameSession = new HttpGameSession(httpClient);
        }

        // Testfall: Überprüft, ob CreateGameAsync bei einer erfolgreichen Server-Antwort (Status 201 Created) das korrekte DTO zurückgibt.
        [Fact]
        public async Task CreateGameAsyncOnSuccessReturnsResultDto()
        {
            // Arrange: Bereitet die zu sendenden Daten und die erwartete Antwort des Servers vor.
            var createGameDto = new CreateGameDto { PlayerName = "TestPlayer", Color = Player.White, InitialMinutes = 10 };
            var expectedResult = new CreateGameResultDto
            {
                GameId = Guid.NewGuid(),
                PlayerId = Guid.NewGuid(),
                Color = Player.White,
                Board = new BoardDto(new PieceDto?[8][])
            };

            // Konfiguriert den gemockten HttpMessageHandler, um eine erfolgreiche Antwort zu simulieren.
            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Created,
                Content = JsonContent.Create(expectedResult)
            };

            mockMessageHandler
                .Protected() // .Protected() wird benötigt, um die Methode "SendAsync" des Handlers zu mocken.
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(), // Akzeptiert jede Anfrage
                    ItExpr.IsAny<CancellationToken>()
                   )
                .ReturnsAsync(responseMessage); // Gibt die vorbereitete Antwort zurück.

            // Act: Ruft die zu testende Methode auf.
            var result = await gameSession.CreateGameAsync(createGameDto);

            // Assert: Vergleicht das Ergebnis mit den erwarteten Werten.
            Assert.NotNull(result);
            Assert.Equal(expectedResult.GameId, result.GameId);
            Assert.Equal(expectedResult.PlayerId, result.PlayerId);
        }

        // Testfall: Stellt sicher, dass bei einem API-Fehler (z.B. 400 Bad Request) eine HttpRequestException geworfen wird.
        [Fact]
        public async Task CreateGameAsyncWithApiErrorThrowsHttpRequestException()
        {
            // Arrange: Simuliert eine Fehlerantwort vom Server mit einer Fehlermeldung im Body.
            var createGameDto = new CreateGameDto { PlayerName = "TestPlayer", Color = Player.White, InitialMinutes = 10 };
            var errorContent = "Der Spielername ist ungültig.";

            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent(errorContent, Encoding.UTF8, "application/json")
            };

            mockMessageHandler
               .Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(responseMessage);

            // Act & Assert: Erwartet, dass der Aufruf eine HttpRequestException auslöst, die die Fehlermeldung und den Statuscode enthält.
            var exception = await Assert.ThrowsAsync<HttpRequestException>(async () => await gameSession.CreateGameAsync(createGameDto));
            Assert.Contains(errorContent, exception.Message);
            Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        }

        // Die folgenden Tests folgen demselben Muster wie die obigen:
        // Sie simulieren eine erfolgreiche oder fehlerhafte Server-Antwort für jede Methode von HttpGameSession
        // und überprüfen, ob das erwartete Ergebnis zurückgegeben oder die korrekte Ausnahme ausgelöst wird.

        [Fact]
        public async Task GetBoardAsyncOnSuccessReturnsBoardDto()
        {
            // Arrange
            var gameId = Guid.NewGuid();
            var squares = new PieceDto?[8][];
            for (int i = 0; i < 8; i++)
            {
                squares[i] = new PieceDto?[8];
            }
            var expectedBoard = new BoardDto(squares);
            var responseMessage = new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(expectedBoard) };

            mockMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri!.ToString().Contains($"/api/games/{gameId}/state")), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            // Act
            var result = await gameSession.GetBoardAsync(gameId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(8, result.Squares.Length);
        }

        [Fact]
        public async Task JoinGameAsyncOnSuccessReturnsJoinGameResultDto()
        {
            // Arrange
            var gameId = Guid.NewGuid();
            var playerName = "Hikaru";
            var expectedResult = new JoinGameResultDto { PlayerId = Guid.NewGuid(), Name = playerName, Color = Player.Black, Board = new BoardDto(new PieceDto?[8][]) };
            var responseMessage = new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(expectedResult) };

            mockMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri!.ToString().Contains($"api/games/{gameId}/join")), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            // Act
            var result = await gameSession.JoinGameAsync(gameId, playerName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResult.PlayerId, result.PlayerId);
            Assert.Equal(expectedResult.Name, result.Name);
        }

        [Fact]
        public async Task SendMoveAsyncWithValidMoveReturnsValidMoveResult()
        {
            // Arrange
            var gameId = Guid.NewGuid();
            var moveDto = new MoveDto("e2", "e4", Guid.NewGuid());
            var expectedResult = new MoveResultDto { IsValid = true, NewBoard = new BoardDto(new PieceDto?[8][]), IsYourTurn = false, Status = GameStatusDto.None };
            var responseMessage = new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(expectedResult) };

            mockMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri!.ToString().Contains($"api/games/{gameId}/move")), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            // Act
            var result = await gameSession.SendMoveAsync(gameId, moveDto);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsValid);
            Assert.NotNull(result.NewBoard);
        }

        // Testfall: Stellt sicher, dass bei einer 404-Antwort (z.B. wenn der Gegner noch nicht beigetreten ist) null zurückgegeben wird.
        [Fact]
        public async Task GetOpponentInfoAsyncWhenOpponentNotJoinedReturnsNull()
        {
            // Arrange: Simuliert eine 404 Not Found Antwort vom Server.
            var gameId = Guid.NewGuid();
            var playerId = Guid.NewGuid();
            var responseMessage = new HttpResponseMessage(HttpStatusCode.NotFound);

            mockMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri!.ToString().Contains($"api/games/{gameId}/player/{playerId}/opponentinfo")), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            // Act
            var result = await gameSession.GetOpponentInfoAsync(gameId, playerId);

            // Assert: Das Ergebnis sollte null sein, ohne eine Ausnahme auszulösen.
            Assert.Null(result);
        }

        // Testfall: Stellt sicher, dass die Methode eine leere Sammlung zurückgibt, wenn die API `null` liefert.
        [Fact]
        public async Task GetLegalMovesAsyncWhenApiReturnsNullReturnsEmptyCollection()
        {
            // Arrange: Simuliert, dass die API eine leere JSON-Antwort (`null`) zurückgibt.
            var gameId = Guid.NewGuid();
            var playerId = Guid.NewGuid();
            var responseMessage = new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create<IEnumerable<string>>(null!) };

            mockMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            // Act
            var result = await gameSession.GetLegalMovesAsync(gameId, "e2", playerId);

            // Assert: Die Methode sollte eine leere Sammlung zurückgeben, anstatt selbst null zu sein.
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        // Testfall: Simuliert einen Infrastruktur-Fehler (z.B. Server nicht erreichbar), bei dem kein JSON zurückkommt.
        [Fact]
        public async Task AnyApiCallWithInfrastructureErrorThrowsGenericException()
        {
            // Arrange: Simuliert einen Serverfehler, der eine HTML-Fehlerseite anstelle von JSON zurückgibt.
            var responseMessage = new HttpResponseMessage { StatusCode = HttpStatusCode.ServiceUnavailable, Content = new StringContent("<html><body>Service Error</body></html>", Encoding.UTF8, "text/html") };

            mockMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            // Act & Assert: Erwartet eine generische Fehlermeldung, die den Benutzer informiert, dass der Server nicht verfügbar ist.
            var exception = await Assert.ThrowsAsync<HttpRequestException>(() => gameSession.GetBoardAsync(Guid.NewGuid()));
            Assert.Contains("Der Server ist derzeit nicht verfügbar", exception.Message);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
        }

        [Fact]
        public async Task GetGameInfoAsyncOnSuccessReturnsGameInfo()
        {
            // Arrange
            var gameId = Guid.NewGuid();
            var expectedInfo = new GameInfoDto(Guid.NewGuid(), Player.White, true);
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(expectedInfo) };
            mockMessageHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(responseMessage);

            // Act
            var result = await gameSession.GetGameInfoAsync(gameId);

            // Assert
            Assert.Equal(expectedInfo, result);
        }

        [Fact]
        public async Task GetGameStatusAsyncOnSuccessReturnsGameStatus()
        {
            // Arrange
            var gameId = Guid.NewGuid();
            var playerId = Guid.NewGuid();
            var expectedStatus = GameStatusDto.Check;
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(expectedStatus) };
            mockMessageHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(responseMessage);

            // Act
            var result = await gameSession.GetGameStatusAsync(gameId, playerId);

            // Assert
            Assert.Equal(expectedStatus, result);
        }

        [Fact]
        public async Task GetCurrentTurnPlayerAsyncOnSuccessReturnsPlayer()
        {
            // Arrange
            var gameId = Guid.NewGuid();
            var expectedPlayer = Player.Black;
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(expectedPlayer) };
            mockMessageHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(responseMessage);

            // Act
            var result = await gameSession.GetCurrentTurnPlayerAsync(gameId);

            // Assert
            Assert.Equal(expectedPlayer, result);
        }

        [Fact]
        public async Task GetTimeUpdateAsyncOnSuccessReturnsTimeUpdate()
        {
            // Arrange
            var gameId = Guid.NewGuid();
            var expectedTime = new TimeUpdateDto(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(4.5), Player.White);
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(expectedTime) };
            mockMessageHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(responseMessage);

            // Act
            var result = await gameSession.GetTimeUpdateAsync(gameId);

            // Assert
            Assert.Equal(expectedTime, result);
        }

        [Fact]
        public async Task ActivateCardAsyncOnSuccessReturnsActivationResult()
        {
            // Arrange
            var gameId = Guid.NewGuid();
            var playerId = Guid.NewGuid();
            var requestDto = new ActivateCardRequestDto { CardInstanceId = Guid.NewGuid(), CardTypeId = "teleport" };
            var expectedResult = new ServerCardActivationResultDto { Success = true, CardId = requestDto.CardTypeId };
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(expectedResult) };
            mockMessageHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(responseMessage);

            // Act
            var result = await gameSession.ActivateCardAsync(gameId, playerId, requestDto);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(requestDto.CardTypeId, result.CardId);
        }

        [Fact]
        public async Task GetCapturedPiecesAsyncOnSuccessReturnsPieceCollection()
        {
            // Arrange
            var gameId = Guid.NewGuid();
            var playerId = Guid.NewGuid();
            var expectedPieces = new List<CapturedPieceTypeDto> { new(PieceType.Knight), new(PieceType.Pawn) };
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(expectedPieces) };
            mockMessageHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(responseMessage);

            // Act
            var result = await gameSession.GetCapturedPiecesAsync(gameId, playerId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Equal(PieceType.Knight, result.First().Type);
        }

        // Gibt die Ressourcen des HttpClient und des MessageHandlers frei.
        public void Dispose()
        {
            httpClient.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}