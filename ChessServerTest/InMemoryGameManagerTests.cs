using Xunit;
using Moq;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using ChessServer.Hubs;
using ChessLogic;
using ChessNetwork.DTOs;
using Chess.Logging;
using ChessServer.Services.Connectivity;
using ChessServer.Services.ComputerPlayer;
using ChessServer.Services.Management;
using ChessServer.Services.Session;

namespace ChessServer.Tests
{
    // Testklasse für den InMemoryGameManager, die zentrale Verwaltungsinstanz für alle Spiele.
    public class InMemoryGameManagerTests
    {
        // Mock-Objekte für die Abhängigkeiten, die der GameManager benötigt.
        // Wir simulieren diese, um uns nur auf das Testen der Manager-Logik zu konzentrieren.
        private readonly Mock<IHubContext<ChessHub>> _mockHubContext;
        private readonly Mock<IChessLogger> _mockLogger;
        private readonly Mock<ILoggerFactory> _mockLoggerFactory;
        private readonly Mock<IComputerMoveProvider> _mockComputerMoveProvider;
        private readonly Mock<IConnectionMappingService> _mockConnectionMappingService;
        private readonly Mock<IMoveExecutionService> _mockMoveExecutionService;
        private readonly InMemoryGameManager _gameManager;

        // Konstruktor: Initialisiert die Mock-Objekte und die zu testende Instanz des Managers.
        public InMemoryGameManagerTests()
        {
            _mockHubContext = new Mock<IHubContext<ChessHub>>();
            _mockLogger = new Mock<IChessLogger>();
            _mockLoggerFactory = new Mock<ILoggerFactory>();
            _mockComputerMoveProvider = new Mock<IComputerMoveProvider>();
            _mockConnectionMappingService = new Mock<IConnectionMappingService>();
            _mockMoveExecutionService = new Mock<IMoveExecutionService>();

            // Sicherstellen, dass der LoggerFactory einen gültigen (aber leeren) Logger zurückgibt,
            // da die GameSession-Klasse dies beim Erstellen erwartet.
            _mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>()))
                .Returns(new Mock<ILogger>().Object);

            // Erstellen einer Instanz des InMemoryGameManager mit den simulierten Abhängigkeiten.
            _gameManager = new InMemoryGameManager(
                _mockHubContext.Object,
                _mockLogger.Object,
                _mockLoggerFactory.Object,
                _mockComputerMoveProvider.Object,
                _mockConnectionMappingService.Object,
                _mockMoveExecutionService.Object
            );
        }

        // Testfall: Überprüft, ob das Erstellen eines Spiels mit gültigen Daten eine Spiel- und Spieler-ID zurückgibt.
        [Fact]
        public void CreateGameWithValidPlayerNameReturnsValidGameAndPlayerIds()
        {
            // Arrange: Definiert die Parameter für das neue Spiel.
            string playerName = "max Mustermann";
            Player color = Player.White;
            int time = 10;

            // Act: Ruft die zu testende Methode auf.
            var (gameId, playerId) = _gameManager.CreateGame(playerName, color, time);

            // Assert: Überprüft die Ergebnisse.
            Assert.NotEqual(Guid.Empty, gameId); // Die Spiel-ID sollte nicht leer sein.
            Assert.NotEqual(Guid.Empty, playerId); // Die Spieler-ID sollte nicht leer sein.
        }

        // Testfall: Stellt sicher, dass das Erstellen eines Spiels mit einem leeren Namen eine Ausnahme auslöst.
        [Fact]
        public void CreateGameWithEmptyPlayerNameThrowsArgumentException()
        {
            // Arrange: Definiert ungültige Parameter.
            string playerName = ""; // Ungültiger Name.
            Player color = Player.White;
            int time = 10;

            // Act & Assert: Überprüft, ob die Methode eine ArgumentException wirft. Dies ist der erwartete, korrekte Fehlerfall.
            Assert.Throws<ArgumentException>(() => _gameManager.CreateGame(playerName, color, time));
        }

        // Testfall: Simuliert das Beitreten eines zweiten Spielers zu einem existierenden Spiel.
        [Fact]
        public void JoinGameWhenGameExistsAndIsNotFullReturnsValidPlayerIdAndColor()
        {
            // Arrange: Erstellt zuerst ein Spiel.
            var (gameId, _) = _gameManager.CreateGame("Player1", Player.White, 10);

            // Act: Lässt einen zweiten Spieler beitreten.
            var (player2Id, player2Color) = _gameManager.JoinGame(gameId, "Player2");

            // Assert: Überprüft, ob der zweite Spieler eine gültige ID und die korrekte (entgegengesetzte) Farbe erhält.
            Assert.NotEqual(Guid.Empty, player2Id);
            Assert.Equal(Player.Black, player2Color);
        }

        // Testfall: Prüft das Verhalten, wenn ein dritter Spieler versucht, einem bereits vollen Spiel beizutreten.
        [Fact]
        public void JoinGameWhenGameIsFullThrowsInvalidOperationException()
        {
            // Arrange: Erstellt ein Spiel und lässt einen zweiten Spieler beitreten, um es zu füllen.
            var (gameId, _) = _gameManager.CreateGame("Player1", Player.White, 10);
            _gameManager.JoinGame(gameId, "Player2"); // Spiel ist jetzt voll.

            // Act & Assert: Versucht, einem vollen Spiel beizutreten und erwartet eine InvalidOperationException.
            Assert.Throws<InvalidOperationException>(() => _gameManager.JoinGame(gameId, "Player3"));
        }

        // Testfall: Überprüft, ob die Informationen über den Spieler, der das Spiel erstellt hat, korrekt abgerufen werden können.
        [Fact]
        public void GetGameInfoAfterCreationReturnsCorrectCreatorInfo()
        {
            // Arrange: Erstellt ein neues Spiel.
            string creatorName = "Gandalf";
            Player creatorColor = Player.White;
            var (gameId, creatorId) = _gameManager.CreateGame(creatorName, creatorColor, 10);

            // Act: Ruft die Spielinformationen ab.
            var gameInfo = _gameManager.GetGameInfo(gameId);

            // Assert: Verifiziert die zurückgegebenen Informationen.
            Assert.NotNull(gameInfo);
            Assert.Equal(creatorId, gameInfo.CreatorId);
            Assert.Equal(creatorColor, gameInfo.CreatorColor);
            Assert.False(gameInfo.HasOpponent); // Zu diesem Zeitpunkt ist noch kein Gegner beigetreten.
        }
    }
}