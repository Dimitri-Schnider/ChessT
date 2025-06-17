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
using System.Collections.Generic;
using Xunit;

namespace ChessServer.Tests
{
    public class RebirthEffectTests
    {
        private readonly Mock<IChessLogger> mockLogger;
        private readonly Mock<GameSession> mockSession;
        private readonly Mock<ICardManager> mockCardManager;
        private readonly Mock<IHistoryManager> mockHistoryManager;
        private readonly Board board;
        private readonly Mock<GameTimerService> mockTimerService;

        public RebirthEffectTests()
        {
            mockLogger = new Mock<IChessLogger>();
            mockSession = new Mock<GameSession>();
            mockCardManager = new Mock<ICardManager>();
            mockHistoryManager = new Mock<IHistoryManager>();
            board = new Board();

            // Initialisiere den Mock für den GameTimerService
            mockTimerService = new Mock<GameTimerService>(Guid.NewGuid(), TimeSpan.FromMinutes(5), new Mock<ILogger<GameTimerService>>().Object);

            // Die gemockte GameSession so einrichten, dass sie unsere anderen Mocks zurückgibt
            var mockGameState = new Mock<GameState>();
            mockGameState.SetupGet(gs => gs.Board).Returns(board);
            mockSession.SetupGet(s => s.CurrentGameState).Returns(mockGameState.Object);
            mockSession.SetupGet(s => s.CardManager).Returns(mockCardManager.Object);

            // NEU & WICHTIG: Sage der GameSession, dass sie unseren gemockten TimerService zurückgeben soll
            mockSession.SetupGet(s => s.TimerService).Returns(mockTimerService.Object);
        }

        [Fact]
        public void ExecuteWithValidCapturedPieceAndEmptySquareRevivesPieceAndReturnsSuccess()
        {
            // ===== ARRANGE =====
            var rebirthEffect = new RebirthEffect(mockLogger.Object);
            var playerId = Guid.NewGuid();
            var playerColor = Player.White;
            var pieceToRevive = PieceType.Queen;
            var targetSquare = "d1";
            var targetPosition = GameSession.ParsePos(targetSquare);

            var capturedPieces = new List<CapturedPieceTypeDto> { new(PieceType.Queen) };
            mockCardManager.Setup(cm => cm.GetCapturedPieceTypesOfPlayer(playerColor)).Returns(capturedPieces);

            Assert.True(board.IsEmpty(targetPosition));

            board[new Position(7, 7)] = new King(playerColor);

            // ===== ACT =====
            var result = rebirthEffect.Execute(
                mockSession.Object,
                playerId,
                playerColor,
                mockHistoryManager.Object,
                CardConstants.Wiedergeburt,
                pieceToRevive.ToString(),
                targetSquare
            );

            // ===== ASSERT =====
            Assert.True(result.Success);
            Assert.True(result.BoardUpdatedByCardEffect);

            var pieceOnBoard = board[targetPosition];
            Assert.NotNull(pieceOnBoard);
            Assert.IsType<Queen>(pieceOnBoard);
            Assert.Equal(playerColor, pieceOnBoard.Color);

            mockCardManager.Verify(cm => cm.RemoveCapturedPieceOfType(playerColor, pieceToRevive), Times.Once);

            mockHistoryManager.Verify(hm => hm.AddMove(It.IsAny<PlayedMoveDto>()), Times.Once);
        }

        [Fact]
        public void ExecuteWhenTargetSquareIsOccupiedReturnsErrorAndConsumesCard()
        {
            // ===== ARRANGE =====
            var rebirthEffect = new RebirthEffect(mockLogger.Object);
            var playerId = Guid.NewGuid();
            var playerColor = Player.White;
            var pieceToRevive = PieceType.Queen;
            var targetSquare = "d1";
            var targetPosition = GameSession.ParsePos(targetSquare);

            // 1. Simulieren, dass eine Dame geschlagen wurde.
            var capturedPieces = new List<CapturedPieceTypeDto> { new(PieceType.Queen) };
            mockCardManager.Setup(cm => cm.GetCapturedPieceTypesOfPlayer(playerColor)).Returns(capturedPieces);

            // 2. WICHTIG: Platziere eine andere Figur auf dem Zielfeld, um es zu blockieren.
            var blockingPiece = new Pawn(Player.White);
            board[targetPosition] = blockingPiece;

            // ===== ACT =====
            var result = rebirthEffect.Execute(
                mockSession.Object,
                playerId,
                playerColor,
                mockHistoryManager.Object,
                CardConstants.Wiedergeburt,
                pieceToRevive.ToString(),
                targetSquare
            );

            // ===== ASSERT =====
            // 1. Laut deiner Logik in RebirthEffect.cs wird die Karte trotzdem als "verbraucht" angesehen.
            //    Der `Success` ist `true`, aber es wird eine Fehlermeldung zurückgegeben.
            Assert.True(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("besetzt", result.ErrorMessage);

            // 2. Das Brett darf nicht verändert worden sein. Die blockierende Figur muss noch da sein.
            var pieceOnBoard = board[targetPosition];
            Assert.Same(blockingPiece, pieceOnBoard); // Es muss immer noch der Bauer sein, nicht die Dame.

            // 3. WICHTIG: Die Figur darf NICHT aus der Liste der Geschlagenen entfernt worden sein.
            mockCardManager.Verify(cm => cm.RemoveCapturedPieceOfType(It.IsAny<Player>(), It.IsAny<PieceType>()), Times.Never());

            // 4. Der Zug darf NICHT zur Historie hinzugefügt worden sein.
            mockHistoryManager.Verify(hm => hm.AddMove(It.IsAny<PlayedMoveDto>()), Times.Never());
        }
    }
}