using Xunit;
using Moq;
using ChessServer.Services;
using ChessLogic;
using Chess.Logging;
using System;

namespace ChessServer.Tests
{
    public class PlayerManagerTests
    {
        private readonly Mock<IChessLogger> mockLogger;

        public PlayerManagerTests()
        {
            mockLogger = new Mock<IChessLogger>();
        }

        [Fact]
        public void JoinFirstPlayerAssignsCorrectColorAndId()
        {
            // Arrange
            var playerManager = new PlayerManager(Guid.NewGuid(), "Human", "Medium", mockLogger.Object);
            var playerName = "Player One";
            var preferredColor = Player.White;

            // Act
            var (playerId, assignedColor) = playerManager.Join(playerName, preferredColor);

            // Assert
            Assert.NotEqual(Guid.Empty, playerId);
            Assert.Equal(preferredColor, assignedColor);
            Assert.Equal(1, playerManager.PlayerCount);
            Assert.Equal(playerName, playerManager.GetPlayerName(playerId));
        }

        [Fact]
        public void JoinSecondPlayerAssignsOppositeColor()
        {
            // Arrange
            var playerManager = new PlayerManager(Guid.NewGuid(), "Human", "Medium", mockLogger.Object);
            playerManager.Join("Player One", Player.White); // Erster Spieler tritt bei

            // Act
            var (playerTwoId, playerTwoColor) = playerManager.Join("Player Two", null);

            // Assert
            Assert.NotEqual(Guid.Empty, playerTwoId);
            Assert.Equal(Player.Black, playerTwoColor); // Der zweite Spieler muss Schwarz sein
            Assert.Equal(2, playerManager.PlayerCount);
        }

        [Fact]
        public void JoinGameIsFullThrowsInvalidOperationException()
        {
            // Arrange
            var playerManager = new PlayerManager(Guid.NewGuid(), "Human", "Medium", mockLogger.Object);
            playerManager.Join("Player One", Player.White);
            playerManager.Join("Player Two", Player.Black);

            // Act & Assert
            // Versucht, einem vollen Spiel beizutreten und erwartet eine Exception
            var exception = Assert.Throws<InvalidOperationException>(() => playerManager.Join("Player Three", null));
            Assert.Equal("Spiel ist bereits voll.", exception.Message); 
        }

        [Fact]
        public void JoinComputerOpponentIsCreatedAutomaticallyWithOppositeColor()
        {
            // Arrange: Ein PvC-Spiel wird konfiguriert
            var playerManager = new PlayerManager(Guid.NewGuid(), "Computer", "Hard", mockLogger.Object);
            var humanPlayerName = "Human Player";
            var humanPlayerColor = Player.Black;

            // Act: Der menschliche Spieler tritt bei. Dies sollte den Computer-Gegner triggern.
            var (humanPlayerId, assignedColor) = playerManager.Join(humanPlayerName, humanPlayerColor);

            // Assert
            Assert.Equal(2, playerManager.PlayerCount); // Es sollten jetzt 2 Spieler sein (Mensch + Computer)
            Assert.True(playerManager.HasOpponent);
            Assert.NotNull(playerManager.ComputerPlayerId); // Die ID des Computers sollte gesetzt sein

            // Überprüfe, ob der Computer die korrekte, entgegengesetzte Farbe hat
            var computerColor = playerManager.GetPlayerColor(playerManager.ComputerPlayerId.Value);
            Assert.Equal(Player.White, computerColor);
            Assert.Equal(humanPlayerColor, assignedColor); // Sicherstellen, dass der Mensch seine Wunschfarbe behält
        }
    }
}