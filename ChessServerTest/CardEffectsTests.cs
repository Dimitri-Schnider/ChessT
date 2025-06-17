using Chess.Logging;
using ChessLogic;
using ChessNetwork.Configuration;
using ChessServer.Services;
using ChessServer.Services.CardEffects;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using Xunit;

namespace ChessServer.Tests
{
    public class CardEffectsTests
    {
        private readonly Mock<IChessLogger> mockLogger;
        private readonly Mock<GameSession> mockSession;
        private readonly Mock<GameTimerService> mockTimerService;

        public CardEffectsTests()
        {
            mockLogger = new Mock<IChessLogger>();

            // TimerService mocken
            mockTimerService = new Mock<GameTimerService>(Guid.NewGuid(), TimeSpan.FromMinutes(5), new Mock<ILogger<GameTimerService>>().Object);

            // GameSession mocken. Da sie jetzt nicht mehr sealed ist, funktioniert das.
            // Wir verwenden den parameterlosen Konstruktor und richten nur die benötigten Properties ein.
            mockSession = new Mock<GameSession>();

            // Richte die gemockte TimerService-Eigenschaft ein.
            mockSession.SetupGet(s => s.TimerService).Returns(mockTimerService.Object);
            mockSession.SetupGet(s => s.GameId).Returns(Guid.NewGuid());
        }

        [Fact]
        public void ExecuteWhenConditionsAreMetAddsTimeAndReturnsSuccess()
        {
            // Arrange
            var addTimeEffect = new AddTimeEffect(mockLogger.Object);
            var playerId = Guid.NewGuid();
            var playerColor = Player.White;

            // Konfiguriere den Mock so, dass AddTime erfolgreich ist
            mockTimerService.Setup(t => t.AddTime(playerColor, TimeSpan.FromMinutes(2))).Returns(true);

            // Act
            var result = addTimeEffect.Execute(mockSession.Object, playerId, playerColor, null!, CardConstants.AddTime, null, null);

            // Assert
            Assert.True(result.Success);
            Assert.False(result.BoardUpdatedByCardEffect); // Diese Karte ändert das Brett nicht
            mockTimerService.Verify(t => t.AddTime(playerColor, TimeSpan.FromMinutes(2)), Times.Once); // Überprüfen, ob die Methode aufgerufen wurde
        }

        [Fact]
        public void ExecuteWhenTimerServiceFailsReturnsFailure()
        {
            // Arrange
            var addTimeEffect = new AddTimeEffect(mockLogger.Object);
            var playerId = Guid.NewGuid();
            var playerColor = Player.White;

            // Konfiguriere den Mock so, dass AddTime fehlschlägt
            mockTimerService.Setup(t => t.AddTime(playerColor, TimeSpan.FromMinutes(2))).Returns(false);

            // Act
            var result = addTimeEffect.Execute(mockSession.Object, playerId, playerColor, null!, CardConstants.AddTime, null, null);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Timer-Fehler", result.ErrorMessage);
        }
    }
}