// In ChessServer.Tests/InMemoryGameManagerTests.cs

using Xunit;
using Moq;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using ChessServer.Services;
using ChessServer.Hubs;
using ChessLogic;
using ChessNetwork.DTOs;
using Chess.Logging;

namespace ChessServer.Tests;

public class InMemoryGameManagerTests
{
    // Mock-Objekte für die Abhängigkeiten, die der GameManager benötigt.
    // Wir simulieren diese, um uns nur auf das Testen des Managers zu konzentrieren.
    private readonly Mock<IHubContext<ChessHub>> _mockHubContext;
    private readonly Mock<IChessLogger> _mockLogger;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly InMemoryGameManager _gameManager;

    public InMemoryGameManagerTests()
    {
        // Initialisierung der Mock-Objekte im Konstruktor
        _mockHubContext = new Mock<IHubContext<ChessHub>>();
        _mockLogger = new Mock<IChessLogger>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();

        // Sicherstellen, dass der LoggerFactory einen gültigen (aber leeren) Logger zurückgibt,
        // da die GameSession-Klasse dies beim Erstellen erwartet.
        _mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);

        // Erstellen einer Instanz des InMemoryGameManager mit den simulierten Abhängigkeiten
        _gameManager = new InMemoryGameManager(
            _mockHubContext.Object,
            _mockLogger.Object,
            _mockLoggerFactory.Object,
            _mockHttpClientFactory.Object
        );
    }

    [Fact]
    public void CreateGameWithValidPlayerNameReturnsValidGameAndPlayerIds()
    {
        // Arrange
        string playerName = "max Mustermann";
        Player color = Player.White;
        int time = 10;

        // Act
        // Rufe die zu testende Methode auf
        var (gameId, playerId) = _gameManager.CreateGame(playerName, color, time);

        // Assert
        // Überprüfe die Ergebnisse
        Assert.NotEqual(Guid.Empty, gameId); // Die Spiel-ID sollte nicht leer sein
        Assert.NotEqual(Guid.Empty, playerId); // Die Spieler-ID sollte nicht leer sein
    }

    [Fact]
    public void CreateGameWithEmptyPlayerNameThrowsArgumentException()
    {
        // Arrange
        string playerName = ""; // Ungültiger Name
        Player color = Player.White;
        int time = 10;

        // Act & Assert
        // Überprüft, ob die Methode eine ArgumentException wirft, wenn der Name leer ist.
        // Das ist der erwartete, korrekte Fehlerfall.
        Assert.Throws<ArgumentException>(() => _gameManager.CreateGame(playerName, color, time));
    }

    [Fact]
    public void JoinGameWhenGameExistsAndIsNotFullReturnsValidPlayerIdAndColor()
    {
        // Arrange
        var (gameId, _) = _gameManager.CreateGame("Player1", Player.White, 10);

        // Act
        var (player2Id, player2Color) = _gameManager.JoinGame(gameId, "Player2");

        // Assert
        Assert.NotEqual(Guid.Empty, player2Id); // Der zweite Spieler sollte eine gültige ID bekommen
        Assert.Equal(Player.Black, player2Color); // Der zweite Spieler sollte die entgegengesetzte Farbe bekommen
    }

    [Fact]
    public void JoinGameWhenGameIsFullThrowsInvalidOperationException()
    {
        // Arrange
        var (gameId, _) = _gameManager.CreateGame("Player1", Player.White, 10);
        _gameManager.JoinGame(gameId, "Player2"); // Spiel ist jetzt voll

        // Act & Assert
        // Versucht, einem vollen Spiel beizutreten und erwartet eine Exception
        Assert.Throws<InvalidOperationException>(() => _gameManager.JoinGame(gameId, "Player3"));
    }

    [Fact]
    public void GetGameInfoAfterCreationReturnsCorrectCreatorInfo()
    {
        // Arrange
        string creatorName = "Gandalf";
        Player creatorColor = Player.White;
        var (gameId, creatorId) = _gameManager.CreateGame(creatorName, creatorColor, 10);

        // Act
        var gameInfo = _gameManager.GetGameInfo(gameId);

        // Assert
        Assert.NotNull(gameInfo);
        Assert.Equal(creatorId, gameInfo.CreatorId);
        Assert.Equal(creatorColor, gameInfo.CreatorColor);
        Assert.False(gameInfo.HasOpponent); // Zu diesem Zeitpunkt ist noch kein Gegner beigetreten
    }
}