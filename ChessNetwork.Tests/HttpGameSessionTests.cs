// File: [SolutionDir]\ChessNetwork.Tests\HttpGameSessionTests.cs
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
    public class HttpGameSessionTests : IDisposable
    {
        private readonly Mock<HttpMessageHandler> mockMessageHandler;
        private readonly HttpClient httpClient;
        private readonly HttpGameSession gameSession;

        public HttpGameSessionTests()
        {
            mockMessageHandler = new Mock<HttpMessageHandler>();
            httpClient = new HttpClient(mockMessageHandler.Object)
            {
                BaseAddress = new Uri("http://testserver/")
            };
            gameSession = new HttpGameSession(httpClient);
        }

        [Fact]
        public async Task CreateGameAsyncOnSuccessReturnsResultDto()
        {
            // Arrange
            var createGameDto = new CreateGameDto { PlayerName = "TestPlayer", Color = Player.White, InitialMinutes = 10 };
            var expectedResult = new CreateGameResultDto
            {
                GameId = Guid.NewGuid(),
                PlayerId = Guid.NewGuid(),
                Color = Player.White,
                Board = new BoardDto(new PieceDto?[8][])
            };

            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Created,
                Content = JsonContent.Create(expectedResult)
            };

            mockMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                   )
                .ReturnsAsync(responseMessage);

            // Act
            var result = await gameSession.CreateGameAsync(createGameDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResult.GameId, result.GameId);
            Assert.Equal(expectedResult.PlayerId, result.PlayerId);
        }

        [Fact]
        public async Task CreateGameAsyncWithApiErrorThrowsHttpRequestException()
        {
            // Arrange
            var createGameDto = new CreateGameDto { PlayerName = "TestPlayer", Color = Player.White, InitialMinutes = 10 };
            var errorContent = "Der Spielername ist ungültig.";

            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent(errorContent, Encoding.UTF8, "application/json")
            };

            mockMessageHandler
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                   "SendAsync",
                   ItExpr.IsAny<HttpRequestMessage>(),
                   ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(responseMessage);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpRequestException>(async () => await gameSession.CreateGameAsync(createGameDto));
            Assert.Contains(errorContent, exception.Message);
            Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        }

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

            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(expectedBoard)
            };

            mockMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri!.ToString().Contains($"/api/games/{gameId}/state")),
                    ItExpr.IsAny<CancellationToken>()
                )
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
            var expectedResult = new JoinGameResultDto
            {
                PlayerId = Guid.NewGuid(),
                Name = playerName,
                Color = Player.Black,
                Board = new BoardDto(new PieceDto?[8][])
            };

            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(expectedResult)
            };

            mockMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri!.ToString().Contains($"api/games/{gameId}/join")),
                    ItExpr.IsAny<CancellationToken>()
                )
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
            var expectedResult = new MoveResultDto
            {
                IsValid = true,
                NewBoard = new BoardDto(new PieceDto?[8][]),
                IsYourTurn = false,
                Status = GameStatusDto.None
            };

            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(expectedResult)
            };

            mockMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri!.ToString().Contains($"api/games/{gameId}/move")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            // Act
            var result = await gameSession.SendMoveAsync(gameId, moveDto);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsValid);
            Assert.NotNull(result.NewBoard);
        }

        [Fact]
        public async Task GetOpponentInfoAsyncWhenOpponentNotJoinedReturnsNull()
        {
            // Arrange
            var gameId = Guid.NewGuid();
            var playerId = Guid.NewGuid();
            var responseMessage = new HttpResponseMessage(HttpStatusCode.NotFound);

            mockMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri!.ToString().Contains($"api/games/{gameId}/player/{playerId}/opponentinfo")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            // Act
            var result = await gameSession.GetOpponentInfoAsync(gameId, playerId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetLegalMovesAsyncWhenApiReturnsNullReturnsEmptyCollection()
        {
            // Arrange
            var gameId = Guid.NewGuid();
            var playerId = Guid.NewGuid();

            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create<IEnumerable<string>>(null!)
            };

            mockMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            // Act
            var result = await gameSession.GetLegalMovesAsync(gameId, "e2", playerId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task AnyApiCallWithInfrastructureErrorThrowsGenericException()
        {
            // Arrange
            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Content = new StringContent("<html><body>Service Error</body></html>", Encoding.UTF8, "text/html")
            };

            mockMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            // Act & Assert
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

        public void Dispose()
        {
            httpClient.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}