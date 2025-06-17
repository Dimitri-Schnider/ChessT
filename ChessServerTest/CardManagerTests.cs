using Chess.Logging;
using ChessLogic;
using ChessNetwork.Configuration;
using ChessNetwork.DTOs;
using ChessServer.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ChessServer.Tests
{
    public class CardManagerTests : IDisposable
    {
        private readonly Mock<GameSession> mockSession;
        private readonly Mock<IHistoryManager> mockHistoryManager;
        private readonly Mock<IChessLogger> mockLogger;
        private readonly Mock<ILoggerFactory> mockLoggerFactory;
        private readonly CardManager cardManager;

        public CardManagerTests()
        {
            // Mocks für alle Abhängigkeiten des CardManager erstellen
            mockSession = new Mock<GameSession>();
            mockHistoryManager = new Mock<IHistoryManager>();
            mockLogger = new Mock<IChessLogger>();
            mockLoggerFactory = new Mock<ILoggerFactory>();

            // Sicherstellen, dass der LoggerFactory einen gültigen Logger zurückgibt
            mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>()))
                           .Returns(new Mock<ILogger>().Object);

            // Die zu testende Instanz des CardManager erstellen
            cardManager = new CardManager(
                mockSession.Object,
                new object(), // sessionLock
                mockHistoryManager.Object,
                mockLogger.Object,
                mockLoggerFactory.Object
            );
        }

        // Dispose-Methode, um die IDisposable-Schnittstelle zu implementieren.
        public void Dispose()
        {
            cardManager.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task ActivateCardPlayerNotOnTurnReturnsError()
        {
            // Arrange
            var activatingPlayerId = Guid.NewGuid();
            var activatingPlayerColor = Player.White;
            var requestDto = new ActivateCardRequestDto { CardTypeId = "any_card", CardInstanceId = Guid.NewGuid() };

            // Spieler ist Weiss, aber Schwarz ist am Zug
            mockSession.Setup(s => s.GetPlayerColor(activatingPlayerId)).Returns(activatingPlayerColor);
            mockSession.Setup(s => s.CurrentGameState.CurrentPlayer).Returns(Player.Black);

            // Act
            var result = await cardManager.ActivateCard(activatingPlayerId, requestDto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Nicht dein Zug.", result.ErrorMessage);
        }

        [Fact]
        public async Task ActivateCardPlayerInCheckWithNonBoardAlteringCardReturnsError()
        {
            // Arrange
            var activatingPlayerId = Guid.NewGuid();
            var activatingPlayerColor = Player.White;
            // Eine Zeit-Karte, die das Brett nicht verändert
            var requestDto = new ActivateCardRequestDto { CardTypeId = CardConstants.AddTime, CardInstanceId = Guid.NewGuid() };

            // Spieler ist Weiss und am Zug
            mockSession.Setup(s => s.GetPlayerColor(activatingPlayerId)).Returns(activatingPlayerColor);
            mockSession.Setup(s => s.CurrentGameState.CurrentPlayer).Returns(activatingPlayerColor);

            // WICHTIG: Der Spieler steht im Schach
            var mockBoard = new Mock<Board>();
            mockBoard.Setup(b => b.IsInCheck(activatingPlayerColor)).Returns(true);
            mockSession.Setup(s => s.CurrentGameState.Board).Returns(mockBoard.Object);

            // Act
            var result = await cardManager.ActivateCard(activatingPlayerId, requestDto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Du stehst im Schach! Nur Karten, die Figuren bewegen, sind jetzt erlaubt.", result.ErrorMessage);
        }
    }
}