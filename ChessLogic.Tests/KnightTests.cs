using Xunit;
using ChessLogic;
using ChessLogic.Utilities;
using System.Linq;
using System.Collections.Generic;

namespace ChessLogic.Tests
{
    public class KnightTests
    {
        [Fact]
        public void KnightOnEmptyBoardFromCenterHas8Moves()
        {
            // Arrange
            Board board = new Board();
            Knight knight = new Knight(Player.White);
            Position knightPos = new Position(3, 3); // d5
            board[knightPos] = knight;

            // Act
            IEnumerable<Move> moves = knight.GetMoves(knightPos, board);

            // Assert
            Assert.Equal(8, moves.Count());
            // Erwartete Zielfelder von d5 (3,3):
            // (1,2) c7, (1,4) e7
            // (2,1) b6, (2,5) f6
            // (4,1) b4, (4,5) f4
            // (5,2) c3, (5,4) e3
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(1, 2)));
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(1, 4)));
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(2, 1)));
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(2, 5)));
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(4, 1)));
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(4, 5)));
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(5, 2)));
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(5, 4)));
        }

        [Fact]
        public void KnightOnEmptyBoardFromCornerA1Has2Moves()
        {
            // Arrange
            Board board = new Board();
            Knight knight = new Knight(Player.White);
            Position knightPos = new Position(7, 0); // a1
            board[knightPos] = knight;

            // Act
            IEnumerable<Move> moves = knight.GetMoves(knightPos, board);

            // Assert
            Assert.Equal(2, moves.Count());
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(5, 1))); // b3
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(6, 2))); // c2
        }

        [Fact]
        public void KnightCanCaptureOpponentPieces()
        {
            // Arrange
            Board board = new Board();
            Knight whiteKnight = new Knight(Player.White);
            Position knightPos = new Position(3, 3); // d5
            board[knightPos] = whiteKnight;
            board[new Position(1, 2)] = new Pawn(Player.Black); // c7 (gegnerischer Bauer)
            board[new Position(2, 5)] = new Rook(Player.White); // f6 (eigene Figur)

            // Act
            IEnumerable<Move> moves = whiteKnight.GetMoves(knightPos, board);

            // Assert
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(1, 2))); // Kann c7 schlagen
            Assert.DoesNotContain(moves, m => m.ToPos.Equals(new Position(2, 5))); // Kann nicht auf f6 ziehen (blockiert)
            Assert.Equal(7, moves.Count()); // 8 mögliche Felder - 1 blockiertes
        }

        [Fact]
        public void KnightIsBlockedByOwnPieces()
        {
            // Arrange
            Board board = new Board();
            Knight whiteKnight = new Knight(Player.White);
            Position knightPos = new Position(3, 3); // d5
            board[knightPos] = whiteKnight;
            board[new Position(1, 2)] = new Pawn(Player.White); // Eigener Bauer auf c7

            // Act
            IEnumerable<Move> moves = whiteKnight.GetMoves(knightPos, board);

            // Assert
            Assert.DoesNotContain(moves, m => m.ToPos.Equals(new Position(1, 2))); // Kann nicht auf c7 ziehen
            Assert.Equal(7, moves.Count());
        }

        [Fact]
        public void KnightCanCaptureOpponentKing()
        {
            // Arrange
            Board board = new Board();
            Knight whiteKnight = new Knight(Player.White);
            King blackKing = new King(Player.Black);
            Position knightPos = new Position(5, 5); // f3
            Position kingPos = new Position(7, 4);   // e1
            board[knightPos] = whiteKnight;
            board[kingPos] = blackKing;

            // Act
            bool canCaptureKing = whiteKnight.CanCaptureOpponentKing(knightPos, board);

            // Assert
            Assert.True(canCaptureKing);
        }
    }
}