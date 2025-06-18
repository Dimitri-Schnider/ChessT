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
    // Testklasse für die verschiedenen Karteneffekte.
    // Jeder Effekt wird isoliert getestet, um seine spezifische Logik zu verifizieren.
    public class CardEffectsTests
    {
        // Mock-Objekte für die Abhängigkeiten der Karteneffekte.
        private readonly Mock<IChessLogger> mockLogger;
        private readonly Mock<GameSession> mockSession;
        private readonly Mock<GameTimerService> mockTimerService;
        // Konstruktor: Richtet die Mocks für jeden Test ein.
        public CardEffectsTests()
        {
            mockLogger = new Mock<IChessLogger>();
            // TimerService wird gemockt, da viele Effekte die Zeit beeinflussen.
            mockTimerService = new Mock<GameTimerService>(Guid.NewGuid(), TimeSpan.FromMinutes(5), mockLogger.Object);
            // GameSession wird gemockt, um eine kontrollierte Umgebung für die Effekte bereitzustellen.
            mockSession = new Mock<GameSession>();
            // Richte die gemockte TimerService-Eigenschaft im GameSession-Mock ein.
            mockSession.SetupGet(s => s.TimerService).Returns(mockTimerService.Object);
            mockSession.SetupGet(s => s.GameId).Returns(Guid.NewGuid());
        }

        // Testfall: Überprüft, ob der AddTimeEffect bei Erfolg die Zeit hinzufügt.
        [Fact]
        public void ExecuteWhenConditionsAreMetAddsTimeAndReturnsSuccess()
        {
            // Arrange: Initialisiert den Effekt und konfiguriert die Mocks für den Erfolgsfall.
            var addTimeEffect = new AddTimeEffect(mockLogger.Object);
            var playerId = Guid.NewGuid();
            var playerColor = Player.White;
            // Konfiguriere den Mock so, dass AddTime erfolgreich ist.
            mockTimerService.Setup(t => t.AddTime(playerColor, TimeSpan.FromMinutes(2))).Returns(true);

            // Act: Führt den Karteneffekt aus.
            var result = addTimeEffect.Execute(mockSession.Object, playerId, playerColor, null!, CardConstants.AddTime, null, null);
            // Assert: Überprüft, ob das Ergebnis erfolgreich ist und die erwarteten Aktionen ausgeführt wurden.
            Assert.True(result.Success);
            Assert.False(result.BoardUpdatedByCardEffect);
            // Diese Karte ändert das Brett nicht.
            // Überprüft, ob die AddTime-Methode auf dem TimerService genau einmal aufgerufen wurde.
            mockTimerService.Verify(t => t.AddTime(playerColor, TimeSpan.FromMinutes(2)), Times.Once);
        }

        // Testfall: Stellt sicher, dass der Effekt fehlschlägt, wenn der TimerService dies signalisiert.
        [Fact]
        public void ExecuteWhenTimerServiceFailsReturnsFailure()
        {
            // Arrange: Initialisiert den Effekt und konfiguriert den TimerService-Mock für einen Fehlschlag.
            var addTimeEffect = new AddTimeEffect(mockLogger.Object);
            var playerId = Guid.NewGuid();
            var playerColor = Player.White;
            // Konfiguriere den Mock so, dass AddTime fehlschlägt.
            mockTimerService.Setup(t => t.AddTime(playerColor, TimeSpan.FromMinutes(2))).Returns(false);

            // Act: Führt den Effekt aus.
            var result = addTimeEffect.Execute(mockSession.Object, playerId, playerColor, null!, CardConstants.AddTime, null, null);
            // Assert: Das Ergebnis muss fehlschlagen und eine passende Fehlermeldung enthalten.
            Assert.False(result.Success);
            Assert.Contains("Timer-Fehler", result.ErrorMessage);
        }
    }
}