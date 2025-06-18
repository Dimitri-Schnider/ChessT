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
    // Testklasse spezifisch für den SubtractTimeEffect (Zeitdiebstahl).
    public class SubtractTimeEffectTests
    {
        private readonly Mock<IChessLogger> mockLogger;
        private readonly Mock<GameSession> mockSession;
        private readonly Mock<GameTimerService> mockTimerService;

        // Konstruktor: Richtet die Mocks für die Testumgebung ein.
        public SubtractTimeEffectTests()
        {
            mockLogger = new Mock<IChessLogger>();
            mockSession = new Mock<GameSession>();

            // TimerService wird gemockt, um das Zeitverhalten zu kontrollieren.
            mockTimerService = new Mock<GameTimerService>(Guid.NewGuid(), TimeSpan.FromMinutes(10), mockLogger.Object);

            // Den Session-Mock so einrichten, dass er unseren gemockten TimerService zurückgibt.
            mockSession.SetupGet(s => s.TimerService).Returns(mockTimerService.Object);
        }

        // Testfall: Überprüft den Erfolgsfall, wenn der Gegner genügend Zeit hat.
        [Fact]
        public void ExecuteWhenOpponentHasEnoughTimeSubtractsTimeAndReturnsSuccess()
        {
            // ARRANGE
            var subtractTimeEffect = new SubtractTimeEffect(mockLogger.Object);
            var playerId = Guid.NewGuid();
            var playerColor = Player.White;
            var opponentColor = Player.Black;
            var opponentId = Guid.NewGuid();

            // 1. Simulieren, dass der Gegner existiert.
            mockSession.Setup(s => s.GetPlayerIdByColor(opponentColor)).Returns(opponentId);
            // 2. WICHTIG: Simulieren, dass der Gegner MEHR als 3 Minuten Zeit hat.
            mockTimerService.Setup(t => t.GetCurrentTimeForPlayer(opponentColor)).Returns(TimeSpan.FromMinutes(5));
            // 3. Simulieren, dass das Abziehen der Zeit erfolgreich ist.
            mockTimerService.Setup(t => t.SubtractTime(opponentColor, TimeSpan.FromMinutes(2))).Returns(true);

            // ACT
            var result = subtractTimeEffect.Execute(mockSession.Object, playerId, playerColor, null!, CardConstants.SubtractTime, null, null);

            // ASSERT
            // 1. Die Operation sollte erfolgreich sein.
            Assert.True(result.Success);
            Assert.Null(result.ErrorMessage);

            // 2. Überprüfen, ob die Zeit des Gegners korrekt abgezogen wurde.
            mockTimerService.Verify(t => t.SubtractTime(opponentColor, TimeSpan.FromMinutes(2)), Times.Once);
        }

        // Testfall: Überprüft den Fehlerfall, wenn der Gegner zu wenig Zeit hat.
        [Fact]
        public void ExecuteWhenOpponentHasTooLittleTimeReturnsError()
        {
            // ARRANGE
            var subtractTimeEffect = new SubtractTimeEffect(mockLogger.Object);
            var playerId = Guid.NewGuid();
            var playerColor = Player.White;
            var opponentColor = Player.Black;
            var opponentId = Guid.NewGuid();

            // 1. Simulieren, dass der Gegner existiert.
            mockSession.Setup(s => s.GetPlayerIdByColor(opponentColor)).Returns(opponentId);
            // 2. WICHTIG: Simulieren, dass der Gegner WENIGER als 3 Minuten Zeit hat.
            mockTimerService.Setup(t => t.GetCurrentTimeForPlayer(opponentColor)).Returns(TimeSpan.FromMinutes(2).Add(TimeSpan.FromSeconds(59)));

            // ACT
            var result = subtractTimeEffect.Execute(mockSession.Object, playerId, playerColor, null!, CardConstants.SubtractTime, null, null);

            // ASSERT
            // 1. Die Operation sollte fehlschlagen.
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("wenn der Gegner 3 Minuten oder mehr Zeit hat", result.ErrorMessage);

            // 2. Sicherstellen, dass die Zeit NICHT abgezogen wurde. 
            mockTimerService.Verify(t => t.SubtractTime(It.IsAny<Player>(), It.IsAny<TimeSpan>()), Times.Never());
        }
    }
}