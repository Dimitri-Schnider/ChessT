using ChessLogic.Utilities;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ChessLogic.Tests
{
    // Testklasse für die Funktionalität der Dame.
    public class QueenTests
    {
        // Testfall: Prüft die maximale Bewegungsreichweite einer Dame von der Brettmitte aus.
        [Fact]
        public void QueenOnEmptyBoardFromCenterHasMaxMoves()
        {
            // Arrange
            Board board = new Board();
            Queen queen = new Queen(Player.White);
            Position queenPos = new Position(3, 3);
            board[queenPos] = queen;
            // Act
            IEnumerable<Move> moves = queen.GetMoves(queenPos, board);
            // Assert
            Assert.Equal(27, moves.Count());
        }

        // Testfall: Überprüft das Verhalten einer Dame, die sowohl blockiert wird als auch schlagen kann.
        [Fact]
        public void QueenIsBlockedAndCanCapture()
        {
            // Arrange
            Board board = new Board();
            Queen whiteQueen = new Queen(Player.White);
            Position queenPos = new Position(3, 3);
            board[queenPos] = whiteQueen;
            board[new Position(3, 5)] = new Pawn(Player.White);
            board[new Position(1, 3)] = new Pawn(Player.Black);
            board[new Position(1, 1)] = new Pawn(Player.White);
            board[new Position(5, 1)] = new Pawn(Player.Black);

            // Act
            IEnumerable<Move> moves = whiteQueen.GetMoves(queenPos, board);

            // Assert
            Assert.Equal(20, moves.Count());
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(1, 3)));
            Assert.DoesNotContain(moves, m => m.ToPos.Equals(new Position(0, 0)));
        }

        // Testfall: Testet, ob die Methode zur Erkennung eines Königsangriffs korrekt funktioniert.
        [Fact]
        public void QueenCanCaptureOpponentKing()
        {
            // Arrange
            Board board = new Board();
            Queen whiteQueen = new Queen(Player.White);
            King blackKing = new King(Player.Black);
            Position queenPos = new Position(3, 3);
            Position kingPos = new Position(0, 3);
            board[queenPos] = whiteQueen;
            board[kingPos] = blackKing;

            // Act & Assert (Vertikal)
            Assert.True(whiteQueen.CanCaptureOpponentKing(queenPos, board));

            // Arrange (Diagonal)
            board[kingPos] = null;
            kingPos = new Position(0, 6);
            board[kingPos] = blackKing;

            // Act & Assert (Diagonal)
            Assert.True(whiteQueen.CanCaptureOpponentKing(queenPos, board));
        }
    }
}