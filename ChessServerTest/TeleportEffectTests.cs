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
    // Testklasse spezifisch für den TeleportEffect.
    public class TeleportEffectTests
    {
        private readonly Mock<IChessLogger> mockLogger;
        private readonly Mock<GameSession> mockSession;
        private readonly Mock<IHistoryManager> mockHistoryManager;
        private readonly Board board;

        // Konstruktor: Richtet die Mocks für die Testumgebung ein.
        public TeleportEffectTests()
        {
            mockLogger = new Mock<IChessLogger>();
            mockSession = new Mock<GameSession>();
            mockHistoryManager = new Mock<IHistoryManager>();
            board = new Board();

            // Richte die Mocks für GameState und TimerService ein, die von der GameSession benötigt werden.
            var mockGameState = new Mock<GameState>();
            mockGameState.SetupGet(gs => gs.Board).Returns(board);
            mockSession.SetupGet(s => s.CurrentGameState).Returns(mockGameState.Object);

            var mockTimerService = new Mock<GameTimerService>(Guid.NewGuid(), TimeSpan.FromMinutes(5), mockLogger.Object);
            mockSession.SetupGet(s => s.TimerService).Returns(mockTimerService.Object);
        }

        // Testfall: Stellt sicher, dass ein Teleport fehlschlägt, wenn das Zielfeld besetzt ist.
        [Fact]
        public void ExecuteWhenTargetSquareIsOccupiedReturnsError()
        {
            // Arrange: Bereitet eine Situation vor, in der das Zielfeld blockiert ist.
            var effect = new TeleportEffect(mockLogger.Object);
            var playerColor = Player.White;
            var fromSquare = "a1";
            var toSquare = "h8";

            var pieceToMove = new Rook(playerColor);
            var blockingPiece = new Pawn(playerColor);

            board[GameSession.ParsePos(fromSquare)] = pieceToMove;
            board[GameSession.ParsePos(toSquare)] = blockingPiece; // Zielfeld blockieren.

            // Act: Führt den Teleport-Effekt aus.
            var result = effect.Execute(mockSession.Object, Guid.NewGuid(), playerColor, mockHistoryManager.Object, CardConstants.Teleport, fromSquare, toSquare);

            // Assert: Die Aktion muss fehlschlagen und das Brett unverändert bleiben.
            Assert.False(result.Success);
            Assert.Contains("ist nicht leer", result.ErrorMessage);
            Assert.Same(pieceToMove, board[GameSession.ParsePos(fromSquare)]); // Figur wurde nicht bewegt.
            Assert.Same(blockingPiece, board[GameSession.ParsePos(toSquare)]); // Blockierende Figur ist noch da.
        }
    }
}