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
    public class TeleportEffectTests
    {
        private readonly Mock<IChessLogger> mockLogger;
        private readonly Mock<GameSession> mockSession;
        private readonly Mock<IHistoryManager> mockHistoryManager;
        private readonly Board board;

        public TeleportEffectTests()
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
        public void ExecuteWhenTargetSquareIsOccupiedReturnsError()
        {
            // Arrange
            var effect = new TeleportEffect(mockLogger.Object);
            var playerColor = Player.White;
            var fromSquare = "a1";
            var toSquare = "h8";

            var pieceToMove = new Rook(playerColor);
            var blockingPiece = new Pawn(playerColor);

            board[GameSession.ParsePos(fromSquare)] = pieceToMove;
            board[GameSession.ParsePos(toSquare)] = blockingPiece; // Zielfeld blockieren

            // Act
            var result = effect.Execute(mockSession.Object, Guid.NewGuid(), playerColor, mockHistoryManager.Object, CardConstants.Teleport, fromSquare, toSquare);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("ist nicht leer", result.ErrorMessage);
            Assert.Same(pieceToMove, board[GameSession.ParsePos(fromSquare)]); // Figur wurde nicht bewegt
            Assert.Same(blockingPiece, board[GameSession.ParsePos(toSquare)]); // Blockierende Figur ist noch da
        }
    }
}