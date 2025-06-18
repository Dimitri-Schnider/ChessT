using Xunit;
using Moq;
using ChessServer.Services.Session;
using ChessLogic;
using Chess.Logging;
using System;
using ChessNetwork.DTOs;

namespace ChessServer.Tests
{
    // Testklasse für den PlayerManager, der die Spielerlogik innerhalb einer Session kapselt.
    public class PlayerManagerTests
    {
        private readonly Mock<IChessLogger> mockLogger;

        // Konstruktor: Initialisiert die Mocks für jeden Test.
        public PlayerManagerTests()
        {
            mockLogger = new Mock<IChessLogger>();
        }

        // Testfall: Überprüft, ob der erste Spieler, der einem Spiel beitritt, korrekt zugewiesen wird.
        [Fact]
        public void JoinFirstPlayerAssignsCorrectColorAndId()
        {
            // Arrange: Erstellt eine neue Instanz des PlayerManager.
            var playerManager = new PlayerManager(Guid.NewGuid(), OpponentType.Human, ComputerDifficulty.Medium, mockLogger.Object);
            var playerName = "Player One";
            var preferredColor = Player.White;

            // Act: Lässt den ersten Spieler beitreten.
            var (playerId, assignedColor) = playerManager.Join(playerName, preferredColor);

            // Assert: Überprüft, ob ID, Farbe und Name korrekt gesetzt wurden.
            Assert.NotEqual(Guid.Empty, playerId);
            Assert.Equal(preferredColor, assignedColor);
            Assert.Equal(1, playerManager.PlayerCount);
            Assert.Equal(playerName, playerManager.GetPlayerName(playerId));
        }

        // Testfall: Stellt sicher, dass der zweite Spieler automatisch die entgegengesetzte Farbe erhält.
        [Fact]
        public void JoinSecondPlayerAssignsOppositeColor()
        {
            // Arrange: Erstellt einen Manager und lässt den ersten Spieler beitreten.
            var playerManager = new PlayerManager(Guid.NewGuid(), OpponentType.Human, ComputerDifficulty.Medium, mockLogger.Object);
            playerManager.Join("Player One", Player.White);

            // Act: Lässt den zweiten Spieler beitreten.
            var (playerTwoId, playerTwoColor) = playerManager.Join("Player Two", null);

            // Assert: Der zweite Spieler muss die schwarze Farbe erhalten.
            Assert.NotEqual(Guid.Empty, playerTwoId);
            Assert.Equal(Player.Black, playerTwoColor);
            Assert.Equal(2, playerManager.PlayerCount);
        }

        // Testfall: Prüft, ob der Versuch, einem vollen Spiel beizutreten, eine Ausnahme auslöst.
        [Fact]
        public void JoinGameIsFullThrowsInvalidOperationException()
        {
            // Arrange: Füllt das Spiel mit zwei Spielern.
            var playerManager = new PlayerManager(Guid.NewGuid(), OpponentType.Human, ComputerDifficulty.Medium, mockLogger.Object);
            playerManager.Join("Player One", Player.White);
            playerManager.Join("Player Two", Player.Black);

            // Act & Assert: Erwartet eine InvalidOperationException beim Beitrittsversuch eines dritten Spielers.
            var exception = Assert.Throws<InvalidOperationException>(() => playerManager.Join("Player Three", null));
            Assert.Equal("Spiel ist bereits voll.", exception.Message);
        }

        // Testfall: Verifiziert, dass bei einem Spiel gegen den Computer der KI-Gegner automatisch erstellt wird.
        [Fact]
        public void JoinComputerOpponentIsCreatedAutomaticallyWithOppositeColor()
        {
            // Arrange: Konfiguriert den Manager für ein Spiel gegen den Computer.
            var playerManager = new PlayerManager(Guid.NewGuid(), OpponentType.Computer, ComputerDifficulty.Hard, mockLogger.Object);
            var humanPlayerName = "Human Player";
            var humanPlayerColor = Player.Black;

            // Act: Der menschliche Spieler tritt bei. Dies sollte die Erstellung des Computer-Gegners auslösen.
            var (humanPlayerId, assignedColor) = playerManager.Join(humanPlayerName, humanPlayerColor);

            // Assert: Überprüft, ob der Computer korrekt mit der entgegengesetzten Farbe erstellt wurde.
            Assert.Equal(2, playerManager.PlayerCount); // Es sollten jetzt 2 Spieler sein (Mensch + Computer).
            Assert.True(playerManager.HasOpponent);
            Assert.NotNull(playerManager.ComputerPlayerId); // Die ID des Computers sollte gesetzt sein.

            // Überprüft, ob der Computer die korrekte, entgegengesetzte Farbe hat.
            var computerColor = playerManager.GetPlayerColor(playerManager.ComputerPlayerId.Value);
            Assert.Equal(Player.White, computerColor);
            Assert.Equal(humanPlayerColor, assignedColor); // Sicherstellen, dass der Mensch seine Wunschfarbe behält.
        }
    }
}