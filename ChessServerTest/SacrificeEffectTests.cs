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
    // Testklasse spezifisch für den SacrificeEffect (Opfergabe).
    public class SacrificeEffectTests
    {
        private readonly Mock<IChessLogger> mockLogger;
        private readonly Mock<GameSession> mockSession;
        private readonly Mock<IHistoryManager> mockHistoryManager;
        private readonly Board board;

        // Konstruktor: Richtet die Testumgebung mit Mocks ein.
        public SacrificeEffectTests()
        {
            mockLogger = new Mock<IChessLogger>();
            mockSession = new Mock<GameSession>();
            mockHistoryManager = new Mock<IHistoryManager>();
            board = new Board();

            var mockGameState = new Mock<GameState>();
            mockGameState.SetupGet(gs => gs.Board).Returns(board);
            mockSession.SetupGet(s => s.CurrentGameState).Returns(mockGameState.Object);

            var mockTimerService = new Mock<GameTimerService>(Guid.NewGuid(), TimeSpan.FromMinutes(5), mockLogger.Object);
            mockSession.SetupGet(s => s.TimerService).Returns(mockTimerService.Object);
        }

        // Testfall: Überprüft den Erfolgsfall, bei dem ein eigener Bauer geopfert wird.
        [Fact]
        public void ExecuteWithOwnPawnAndValidMoveReturnsSuccessAndSignalsDraw()
        {
            // Arrange: Platziert einen opferbaren Bauern auf dem Brett.
            var effect = new SacrificeEffect(mockLogger.Object);
            var playerId = Guid.NewGuid();
            var playerColor = Player.White;
            var pawnSquare = "e2";
            board[PositionParser.ParsePos(pawnSquare)] = new Pawn(playerColor);
            board[new Position(7, 4)] = new King(playerColor); // König für Legalitätsprüfung.

            var requestDto = new ActivateCardRequestDto { CardTypeId = CardConstants.SacrificeEffect, FromSquare = pawnSquare };
            var context = new CardExecutionContext(mockSession.Object, playerId, playerColor, mockHistoryManager.Object, requestDto);

            // Act: Führt den Opfer-Effekt aus.
            var result = effect.Execute(context);

            // Assert: Die Aktion muss erfolgreich sein und die richtigen Nebeneffekte auslösen.
            Assert.True(result.Success);
            Assert.True(result.BoardUpdatedByCardEffect);
            Assert.Equal(playerId, result.PlayerIdToSignalDraw);                                // Wichtig: Der Effekt soll ein Kartenziehen signalisieren.
            Assert.True(board.IsEmpty(PositionParser.ParsePos(pawnSquare)));                    // Der Bauer muss vom Brett entfernt worden sein.
            mockHistoryManager.Verify(hm => hm.AddMove(It.IsAny<PlayedMoveDto>()), Times.Once); // Überprüft die Protokollierung.
        }

        // Testfall: Stellt sicher, dass eine andere Figur als ein Bauer nicht geopfert werden kann.
        [Fact]
        public void ExecuteWithNonPawnPieceReturnsError()
        {
            // Arrange: Platziert einen Turm anstelle eines Bauern.
            var effect = new SacrificeEffect(mockLogger.Object);
            var playerId = Guid.NewGuid();
            var playerColor = Player.White;
            var rookSquare = "a1";
            board[PositionParser.ParsePos(rookSquare)] = new Rook(playerColor);

            var requestDto = new ActivateCardRequestDto { CardTypeId = CardConstants.SacrificeEffect, FromSquare = rookSquare };
            var context = new CardExecutionContext(mockSession.Object, playerId, playerColor, mockHistoryManager.Object, requestDto);

            // Act: Versucht, den Turm zu opfern.
            var result = effect.Execute(context);

            // Assert: Die Aktion muss fehlschlagen und das Brett unverändert lassen.
            Assert.False(result.Success);
            Assert.Contains("ist kein Bauer", result.ErrorMessage);
            Assert.NotNull(board[PositionParser.ParsePos(rookSquare)]); // Der Turm muss noch da sein.
        }
    }
}