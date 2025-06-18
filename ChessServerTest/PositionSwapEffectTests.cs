using Chess.Logging;
using ChessLogic;
using ChessLogic.Utilities;
using ChessNetwork.Configuration;
using ChessNetwork.DTOs;
using ChessServer.Services;
using ChessServer.Services.CardEffects;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using Xunit;

namespace ChessServer.Tests
{
    // Testklasse spezifisch für den PositionSwapEffect.
    public class PositionSwapEffectTests
    {
        private readonly Mock<IChessLogger> mockLogger;
        private readonly Mock<GameSession> mockSession;
        private readonly Mock<IHistoryManager> mockHistoryManager;
        private readonly Board board;

        // Konstruktor: Richtet eine kontrollierte Testumgebung für jeden Test ein.
        public PositionSwapEffectTests()
        {
            mockLogger = new Mock<IChessLogger>();
            mockSession = new Mock<GameSession>();
            mockHistoryManager = new Mock<IHistoryManager>();
            board = new Board();

            // Erstellt ein gemocktes GameState-Objekt, das unser Test-Board zurückgibt.
            var mockGameState = new Mock<GameState>();
            mockGameState.SetupGet(gs => gs.Board).Returns(board);
            mockSession.SetupGet(s => s.CurrentGameState).Returns(mockGameState.Object);

            // Mockt den TimerService, da der HistoryManager darauf zugreift.
            var mockTimerService = new Mock<GameTimerService>(Guid.NewGuid(), TimeSpan.FromMinutes(5), mockLogger.Object);
            mockSession.SetupGet(s => s.TimerService).Returns(mockTimerService.Object);
        }

        // Testfall: Überprüft den Erfolgsfall, bei dem zwei eigene Figuren ihre Plätze tauschen.
        [Fact]
        public void ExecuteWithTwoOwnPiecesSwapsPiecesAndReturnsSuccess()
        {
            // Arrange: Platziert zwei eigene Figuren und einen König auf dem Brett.
            var effect = new PositionSwapEffect(mockLogger.Object);
            var playerColor = Player.White;
            var pos1 = "a1";
            var pos2 = "h1";
            var piece1 = new Rook(playerColor);
            var piece2 = new Rook(playerColor);
            board[GameSession.ParsePos(pos1)] = piece1;
            board[GameSession.ParsePos(pos2)] = piece2;
            board[new Position(7, 4)] = new King(playerColor); // König wird für die Legalitätsprüfung benötigt.

            // Act: Führt den Effekt aus.
            var result = effect.Execute(mockSession.Object, Guid.NewGuid(), playerColor, mockHistoryManager.Object, CardConstants.Positionstausch, pos1, pos2);

            // Assert: Das Ergebnis muss erfolgreich sein und die Figuren müssen getauscht worden sein.
            Assert.True(result.Success);
            Assert.True(result.BoardUpdatedByCardEffect);
            Assert.Same(piece2, board[GameSession.ParsePos(pos1)]);                             // Piece2 ist jetzt auf pos1.
            Assert.Same(piece1, board[GameSession.ParsePos(pos2)]);                             // Piece1 ist jetzt auf pos2.
            mockHistoryManager.Verify(hm => hm.AddMove(It.IsAny<PlayedMoveDto>()), Times.Once); // Überprüft, ob der Zug protokolliert wurde.
        }

        // Testfall: Stellt sicher, dass der Tausch fehlschlägt, wenn eines der Felder leer ist.
        [Fact]
        public void ExecuteWhenOneSquareIsEmptyReturnsError()
        {
            // Arrange: Eines der Felder bleibt leer.
            var effect = new PositionSwapEffect(mockLogger.Object);
            var playerColor = Player.White;
            var pos1 = "a1";
            var pos2 = "h1"; // Dieses Feld bleibt leer.
            board[GameSession.ParsePos(pos1)] = new Rook(playerColor);

            // Act: Führt den Effekt aus.
            var result = effect.Execute(mockSession.Object, Guid.NewGuid(), playerColor, mockHistoryManager.Object, CardConstants.Positionstausch, pos1, pos2);

            // Assert: Die Aktion muss fehlschlagen.
            Assert.False(result.Success);
            Assert.Contains("beide Felder für Positionstausch sind leer", result.ErrorMessage);
        }

        // Testfall: Verhindert den Tausch von Figuren unterschiedlicher Farben.
        [Fact]
        public void ExecuteWhenPiecesAreOfDifferentColorReturnsError()
        {
            // Arrange: Platziert eine eigene und eine gegnerische Figur.
            var effect = new PositionSwapEffect(mockLogger.Object);
            var playerColor = Player.White;
            var pos1 = "a1";
            var pos2 = "h1";
            board[GameSession.ParsePos(pos1)] = new Rook(playerColor);
            board[GameSession.ParsePos(pos2)] = new Rook(playerColor.Opponent()); // Gegnerische Figur.

            // Act: Führt den Effekt aus.
            var result = effect.Execute(mockSession.Object, Guid.NewGuid(), playerColor, mockHistoryManager.Object, CardConstants.Positionstausch, pos1, pos2);

            // Assert: Die Aktion muss fehlschlagen.
            Assert.False(result.Success);
            Assert.Contains("Nicht beide Figuren für Positionstausch gehören dem Spieler", result.ErrorMessage);
        }
    }
}