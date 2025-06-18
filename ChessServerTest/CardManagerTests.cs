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
    // Testklasse für den CardManager, der die gesamte Kartenlogik innerhalb einer GameSession verwaltet.
    public class CardManagerTests : IDisposable
    {
        // Mock-Objekte für die Abhängigkeiten des CardManager.
        private readonly Mock<GameSession> mockSession;
        private readonly Mock<IHistoryManager> mockHistoryManager;
        private readonly Mock<IChessLogger> mockLogger;
        private readonly Mock<ILoggerFactory> mockLoggerFactory;
        private readonly CardManager cardManager; // Die zu testende Instanz.

        // Konstruktor: Richtet die Mocks und die CardManager-Instanz für jeden Testfall ein.
        public CardManagerTests()
        {
            // Mocks für alle Abhängigkeiten des CardManager erstellen.
            mockSession = new Mock<GameSession>();
            mockHistoryManager = new Mock<IHistoryManager>();
            mockLogger = new Mock<IChessLogger>();
            mockLoggerFactory = new Mock<ILoggerFactory>();

            // Sicherstellen, dass der LoggerFactory einen gültigen Logger zurückgibt.
            mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>()))
                           .Returns(new Mock<ILogger>().Object);

            // Die zu testende Instanz des CardManager erstellen.
            cardManager = new CardManager(
                mockSession.Object,
                new object(), // sessionLock (ein einfaches Objekt reicht für den Test)
                mockHistoryManager.Object,
                mockLogger.Object,
                mockLoggerFactory.Object
            );
        }

        // Dispose-Methode, um die IDisposable-Schnittstelle zu implementieren und Ressourcen freizugeben.
        public void Dispose()
        {
            cardManager.Dispose();
            GC.SuppressFinalize(this);
        }

        // Testfall: Überprüft, ob das Aktivieren einer Karte fehlschlägt, wenn der Spieler nicht am Zug ist.
        [Fact]
        public async Task ActivateCardPlayerNotOnTurnReturnsError()
        {
            // Arrange: Richtet das Szenario ein, in dem der Spieler nicht am Zug ist.
            var activatingPlayerId = Guid.NewGuid();
            var activatingPlayerColor = Player.White;
            var requestDto = new ActivateCardRequestDto { CardTypeId = "any_card", CardInstanceId = Guid.NewGuid() };
            // Spieler ist Weiss, aber Schwarz ist am Zug.
            mockSession.Setup(s => s.GetPlayerColor(activatingPlayerId)).Returns(activatingPlayerColor);
            mockSession.Setup(s => s.CurrentGameState.CurrentPlayer).Returns(Player.Black);

            // Act: Versucht, die Karte zu aktivieren.
            var result = await cardManager.ActivateCard(activatingPlayerId, requestDto);

            // Assert: Die Aktion muss fehlschlagen und die korrekte Fehlermeldung zurückgeben.
            Assert.False(result.Success);
            Assert.Equal("Nicht dein Zug.", result.ErrorMessage);
        }

        // Testfall: Überprüft, ob das Aktivieren einer nicht-brettverändernden Karte fehlschlägt, wenn der Spieler im Schach steht.
        [Fact]
        public async Task ActivateCardPlayerInCheckWithNonBoardAlteringCardReturnsError()
        {
            // Arrange: Richtet ein Szenario ein, in dem der Spieler im Schach steht.
            var activatingPlayerId = Guid.NewGuid();
            var activatingPlayerColor = Player.White;
            // Eine Zeit-Karte, die das Brett nicht verändert.
            var requestDto = new ActivateCardRequestDto { CardTypeId = CardConstants.AddTime, CardInstanceId = Guid.NewGuid() };

            // Spieler ist Weiss und am Zug.
            mockSession.Setup(s => s.GetPlayerColor(activatingPlayerId)).Returns(activatingPlayerColor);
            mockSession.Setup(s => s.CurrentGameState.CurrentPlayer).Returns(activatingPlayerColor);

            // WICHTIG: Simuliert, dass der Spieler im Schach steht.
            var mockBoard = new Mock<Board>();
            mockBoard.Setup(b => b.IsInCheck(activatingPlayerColor)).Returns(true);
            mockSession.Setup(s => s.CurrentGameState.Board).Returns(mockBoard.Object);

            // Act: Versucht, die Karte zu aktivieren.
            var result = await cardManager.ActivateCard(activatingPlayerId, requestDto);

            // Assert: Die Aktion muss fehlschlagen.
            Assert.False(result.Success);
            Assert.Equal("Du stehst im Schach! Nur Karten, die Figuren bewegen, sind jetzt erlaubt.", result.ErrorMessage);
        }
    }
}