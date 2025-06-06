using ChessLogic.Utilities;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ChessLogic.Tests
{
    // Testklasse für die Funktionalität der Turm-Figur.
    public class RookTests
    {
        // Testfall: Prüft die 14 möglichen Züge eines Turms von der Brettmitte aus.
        [Fact]
        public void RookOnEmptyBoardFromCenterHas14Moves()
        {
            // Arrange
            Board board = new Board();
            Rook rook = new Rook(Player.White);
            Position rookPos = new Position(3, 3);
            board[rookPos] = rook;
            // Act
            IEnumerable<Move> moves = rook.GetMoves(rookPos, board);
            // Assert
            Assert.Equal(14, moves.Count());
        }

        // Testfall: Stellt sicher, dass ein Turm durch eigene Figuren blockiert wird.
        [Fact]
        public void RookIsBlockedByOwnPieces()
        {
            // Arrange
            Board board = new Board();
            Rook whiteRook = new Rook(Player.White);
            Position rookPos = new Position(3, 3);
            board[rookPos] = whiteRook;
            board[new Position(3, 5)] = new Pawn(Player.White);
            board[new Position(1, 3)] = new Pawn(Player.White);
            // Act
            IEnumerable<Move> moves = whiteRook.GetMoves(rookPos, board);
            // Assert
            Assert.Equal(9, moves.Count());
            Assert.DoesNotContain(moves, m => m.ToPos.Equals(new Position(3, 6)));
            Assert.DoesNotContain(moves, m => m.ToPos.Equals(new Position(0, 3)));
        }

        // Testfall: Überprüft, ob ein Turm eine gegnerische Figur schlagen kann und dahinter blockiert wird.
        [Fact]
        public void RookCanCaptureOpponentAndIsBlocked()
        {
            // Arrange
            Board board = new Board();
            Rook whiteRook = new Rook(Player.White);
            Position rookPos = new Position(7, 0);
            board[rookPos] = whiteRook;
            board[new Position(5, 0)] = new Pawn(Player.Black);
            board[new Position(7, 2)] = new Pawn(Player.Black);
            // Act
            IEnumerable<Move> moves = whiteRook.GetMoves(rookPos, board);
            // Assert
            Assert.Equal(4, moves.Count());
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(5, 0)));
            Assert.DoesNotContain(moves, m => m.ToPos.Equals(new Position(4, 0)));
        }
    }
}