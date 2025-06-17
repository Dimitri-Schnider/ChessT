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
    public class PositionSwapEffectTests
    {
        private readonly Mock<IChessLogger> mockLogger;
        private readonly Mock<GameSession> mockSession;
        private readonly Mock<IHistoryManager> mockHistoryManager;
        private readonly Board board;

        public PositionSwapEffectTests()
        {
            mockLogger = new Mock<IChessLogger>();
            mockSession = new Mock<GameSession>();
            mockHistoryManager = new Mock<IHistoryManager>();
            board = new Board();

            var mockGameState = new Mock<GameState>();
            mockGameState.SetupGet(gs => gs.Board).Returns(board);
            mockSession.SetupGet(s => s.CurrentGameState).Returns(mockGameState.Object);

            // TimerService mocken, da der HistoryManager darauf zugreift
            var mockTimerService = new Mock<GameTimerService>(Guid.NewGuid(), TimeSpan.FromMinutes(5), new Mock<ILogger<GameTimerService>>().Object);
            mockSession.SetupGet(s => s.TimerService).Returns(mockTimerService.Object);
        }

        [Fact]
        public void ExecuteWithTwoOwnPiecesSwapsPiecesAndReturnsSuccess()
        {
            // Arrange
            var effect = new PositionSwapEffect(mockLogger.Object);
            var playerColor = Player.White;
            var pos1 = "a1";
            var pos2 = "h1";
            var piece1 = new Rook(playerColor);
            var piece2 = new Rook(playerColor);
            board[GameSession.ParsePos(pos1)] = piece1;
            board[GameSession.ParsePos(pos2)] = piece2;
            board[new Position(7, 4)] = new King(playerColor); // König für Legalitätsprüfung

            // Act
            var result = effect.Execute(mockSession.Object, Guid.NewGuid(), playerColor, mockHistoryManager.Object, CardConstants.Positionstausch, pos1, pos2);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.BoardUpdatedByCardEffect);
            Assert.Same(piece2, board[GameSession.ParsePos(pos1)]); // Piece2 ist jetzt auf pos1
            Assert.Same(piece1, board[GameSession.ParsePos(pos2)]); // Piece1 ist jetzt auf pos2
            mockHistoryManager.Verify(hm => hm.AddMove(It.IsAny<PlayedMoveDto>()), Times.Once);
        }

        [Fact]
        public void ExecuteWhenOneSquareIsEmptyReturnsError()
        {
            // Arrange
            var effect = new PositionSwapEffect(mockLogger.Object);
            var playerColor = Player.White;
            var pos1 = "a1";
            var pos2 = "h1"; // Dieses Feld bleibt leer
            board[GameSession.ParsePos(pos1)] = new Rook(playerColor);

            // Act
            var result = effect.Execute(mockSession.Object, Guid.NewGuid(), playerColor, mockHistoryManager.Object, CardConstants.Positionstausch, pos1, pos2);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("beide Felder für Positionstausch sind leer", result.ErrorMessage);
        }

        [Fact]
        public void ExecuteWhenPiecesAreOfDifferentColorReturnsError()
        {
            // Arrange
            var effect = new PositionSwapEffect(mockLogger.Object);
            var playerColor = Player.White;
            var pos1 = "a1";
            var pos2 = "h1";
            board[GameSession.ParsePos(pos1)] = new Rook(playerColor);
            board[GameSession.ParsePos(pos2)] = new Rook(playerColor.Opponent()); // Gegnerische Figur

            // Act
            var result = effect.Execute(mockSession.Object, Guid.NewGuid(), playerColor, mockHistoryManager.Object, CardConstants.Positionstausch, pos1, pos2);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Nicht beide Figuren für Positionstausch gehören dem Spieler", result.ErrorMessage);
        }
    }
}