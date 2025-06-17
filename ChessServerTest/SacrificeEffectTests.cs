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
    public class SacrificeEffectTests
    {
        private readonly Mock<IChessLogger> mockLogger;
        private readonly Mock<GameSession> mockSession;
        private readonly Mock<IHistoryManager> mockHistoryManager;
        private readonly Board board;

        public SacrificeEffectTests()
        {
            mockLogger = new Mock<IChessLogger>();
            mockSession = new Mock<GameSession>();
            mockHistoryManager = new Mock<IHistoryManager>();
            board = new Board();

            var mockGameState = new Mock<GameState>();
            mockGameState.SetupGet(gs => gs.Board).Returns(board);
            mockSession.SetupGet(s => s.CurrentGameState).Returns(mockGameState.Object);

            var mockTimerService = new Mock<GameTimerService>(Guid.NewGuid(), TimeSpan.FromMinutes(5), new Mock<ILogger<GameTimerService>>().Object);
            mockSession.SetupGet(s => s.TimerService).Returns(mockTimerService.Object);
        }

        [Fact]
        public void ExecuteWithOwnPawnAndValidMoveReturnsSuccessAndSignalsDraw()
        {
            // Arrange
            var effect = new SacrificeEffect(mockLogger.Object);
            var playerId = Guid.NewGuid();
            var playerColor = Player.White;
            var pawnSquare = "e2";
            board[GameSession.ParsePos(pawnSquare)] = new Pawn(playerColor);
            board[new Position(7, 4)] = new King(playerColor); // König für Legalitätsprüfung

            // Act
            var result = effect.Execute(mockSession.Object, playerId, playerColor, mockHistoryManager.Object, CardConstants.SacrificeEffect, pawnSquare, null);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.BoardUpdatedByCardEffect);
            Assert.Equal(playerId, result.PlayerIdToSignalDraw); // Wichtig: Der Effekt soll ein Kartenziehen signalisieren
            Assert.True(board.IsEmpty(GameSession.ParsePos(pawnSquare))); // Der Bauer muss weg sein
            mockHistoryManager.Verify(hm => hm.AddMove(It.IsAny<PlayedMoveDto>()), Times.Once);
        }

        [Fact]
        public void ExecuteWithNonPawnPieceReturnsError()
        {
            // Arrange
            var effect = new SacrificeEffect(mockLogger.Object);
            var playerId = Guid.NewGuid();
            var playerColor = Player.White;
            var rookSquare = "a1";
            board[GameSession.ParsePos(rookSquare)] = new Rook(playerColor); // Ein Turm statt eines Bauern

            // Act
            var result = effect.Execute(mockSession.Object, playerId, playerColor, mockHistoryManager.Object, CardConstants.SacrificeEffect, rookSquare, null);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("ist kein Bauer", result.ErrorMessage);
            Assert.NotNull(board[GameSession.ParsePos(rookSquare)]); // Der Turm muss noch da sein
        }
    }
}