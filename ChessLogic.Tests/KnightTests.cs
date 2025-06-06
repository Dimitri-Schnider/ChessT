using ChessLogic.Utilities;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ChessLogic.Tests
{
    // Testklasse für die Funktionalität der Springer-Figur.
    public class KnightTests
    {
        // Testfall: Prüft die 8 L-förmigen Züge eines Springers von der Brettmitte aus.
        [Fact]
        public void KnightOnEmptyBoardFromCenterHas8Moves()
        {
            // Arrange
            Board board = new Board();
            Knight knight = new Knight(Player.White);
            Position knightPos = new Position(3, 3);
            board[knightPos] = knight;

            // Act
            IEnumerable<Move> moves = knight.GetMoves(knightPos, board);

            // Assert
            Assert.Equal(8, moves.Count());
        }

        // Testfall: Prüft die 2 möglichen Züge eines Springers aus einer Ecke.
        [Fact]
        public void KnightOnEmptyBoardFromCornerA1Has2Moves()
        {
            // Arrange
            Board board = new Board();
            Knight knight = new Knight(Player.White);
            Position knightPos = new Position(7, 0);
            board[knightPos] = knight;

            // Act
            IEnumerable<Move> moves = knight.GetMoves(knightPos, board);

            // Assert
            Assert.Equal(2, moves.Count());
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(5, 1))); // nach b3
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(6, 2))); // nach c2
        }

        // Testfall: Stellt sicher, dass ein Springer eine gegnerische Figur schlagen, aber nicht auf ein von einer eigenen Figur besetztes Feld springen kann.
        [Fact]
        public void KnightCanCaptureOpponentPieces()
        {
            // Arrange
            Board board = new Board();
            Knight whiteKnight = new Knight(Player.White);
            Position knightPos = new Position(3, 3);
            board[knightPos] = whiteKnight;
            board[new Position(1, 2)] = new Pawn(Player.Black);
            board[new Position(2, 5)] = new Rook(Player.White);

            // Act
            IEnumerable<Move> moves = whiteKnight.GetMoves(knightPos, board);

            // Assert
            Assert.Equal(7, moves.Count());
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(1, 2)));
            Assert.DoesNotContain(moves, m => m.ToPos.Equals(new Position(2, 5)));
        }

        // Testfall: Stellt sicher, dass ein Springer durch eigene Figuren blockiert wird.
        [Fact]
        public void KnightIsBlockedByOwnPieces()
        {
            // Arrange
            Board board = new Board();
            Knight whiteKnight = new Knight(Player.White);
            Position knightPos = new Position(3, 3);
            board[knightPos] = whiteKnight;
            board[new Position(1, 2)] = new Pawn(Player.White);

            // Act
            IEnumerable<Move> moves = whiteKnight.GetMoves(knightPos, board);

            // Assert
            Assert.Equal(7, moves.Count());
            Assert.DoesNotContain(moves, m => m.ToPos.Equals(new Position(1, 2)));
        }

        // Testfall: Testet, ob die Methode zur Erkennung eines Königsangriffs korrekt funktioniert.
        [Fact]
        public void KnightCanCaptureOpponentKing()
        {
            // Arrange
            Board board = new Board();
            Knight whiteKnight = new Knight(Player.White);
            King blackKing = new King(Player.Black);
            Position knightPos = new Position(5, 5);
            Position kingPos = new Position(7, 4);
            board[knightPos] = whiteKnight;
            board[kingPos] = blackKing;

            // Act
            bool canCaptureKing = whiteKnight.CanCaptureOpponentKing(knightPos, board);

            // Assert
            Assert.True(canCaptureKing);
        }
    }
}