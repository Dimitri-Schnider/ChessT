using Xunit;
using ChessLogic;
using ChessLogic.Utilities;
using System.Linq;
using System.Collections.Generic;

namespace ChessLogic.Tests
{
    public class BishopTests
    {
        [Fact]
        public void BishopOnEmptyBoardFromCenterHasUpTo13Moves() // Max ist 13 (c3,c6,f3,f6 -> 7+7+9+9 min-Diagonalen 7, max-Diagonalen 13)
        {
            // Arrange
            Board board = new Board();
            Bishop bishop = new Bishop(Player.White);
            Position bishopPos = new Position(3, 3); // d5
            board[bishopPos] = bishop;

            // Act
            IEnumerable<Move> moves = bishop.GetMoves(bishopPos, board);

            // Assert
            Assert.Equal(13, moves.Count());
        }

        [Fact]
        public void BishopOnEmptyBoardFromA1Has7Moves()
        {
            // Arrange
            Board board = new Board();
            Bishop bishop = new Bishop(Player.White);
            Position bishopPos = new Position(7, 0); // a1
            board[bishopPos] = bishop;

            // Act
            IEnumerable<Move> moves = bishop.GetMoves(bishopPos, board);

            // Assert
            Assert.Equal(7, moves.Count()); // Diagonale a1-h8
        }

        [Fact]
        public void BishopIsBlockedByOwnPieces()
        {
            // Arrange
            Board board = new Board();
            Bishop whiteBishop = new Bishop(Player.White);
            Position bishopPos = new Position(3, 3); // d5
            board[bishopPos] = whiteBishop;
            board[new Position(1, 1)] = new Pawn(Player.White); // Eigener Bauer auf b7 (blockiert NordWest)
            board[new Position(5, 5)] = new Pawn(Player.White); // Eigener Bauer auf f3 (blockiert SüdOst)

            // Act
            IEnumerable<Move> moves = whiteBishop.GetMoves(bishopPos, board);

            // Assert
            // Von d5: NW-Diagonale (c6,b7) -> b7 ist Blocker, nur c6 ist frei (1 Zug)
            // Von d5: NE-Diagonale (e6,f7,g8) -> (3 Züge)
            // Von d5: SW-Diagonale (c4,b3,a2) -> (3 Züge)
            // Von d5: SO-Diagonale (e4,f3) -> f3 ist Blocker, nur e4 ist frei (1 Zug)
            // Total = 1 + 3 + 3 + 1 = 8
            Assert.Equal(8, moves.Count());
            Assert.DoesNotContain(moves, m => m.ToPos.Equals(new Position(0, 0))); // a8 nicht erreichbar
            Assert.DoesNotContain(moves, m => m.ToPos.Equals(new Position(6, 6))); // g2 nicht erreichbar
        }

        [Fact]
        public void BishopCanCaptureOpponentAndIsBlocked()
        {
            // Arrange
            Board board = new Board();
            Bishop whiteBishop = new Bishop(Player.White);
            Position bishopPos = new Position(3, 3); // d5
            board[bishopPos] = whiteBishop;
            board[new Position(1, 1)] = new Pawn(Player.Black); // Gegnerischer Bauer auf b7 (kann geschlagen werden)
            board[new Position(5, 5)] = new Pawn(Player.White); // Eigener Bauer auf f3 (blockiert)

            // Act
            IEnumerable<Move> moves = whiteBishop.GetMoves(bishopPos, board);

            // Assert
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(1, 1))); // Schlag auf b7
            Assert.DoesNotContain(moves, m => m.ToPos.Equals(new Position(0, 0))); // a8 nicht erreichbar (hinter b7)
            Assert.DoesNotContain(moves, m => m.ToPos.Equals(new Position(6, 6))); // g2 nicht erreichbar
            // Von d5: NW-Diagonale (c6,b7) -> b7 ist Schlag (2 Züge)
            // Von d5: NE-Diagonale (e6,f7,g8) -> (3 Züge)
            // Von d5: SW-Diagonale (c4,b3,a2) -> (3 Züge)
            // Von d5: SO-Diagonale (e4,f3) -> f3 ist Blocker, nur e4 ist frei (1 Zug)
            // Total = 2 + 3 + 3 + 1 = 9
            Assert.Equal(9, moves.Count());
        }
        [Fact]
        public void BishopCanCaptureOpponentKing()
        {
            // Arrange
            Board board = new Board();
            Bishop whiteBishop = new Bishop(Player.White);
            King blackKing = new King(Player.Black);
            Position bishopPos = new Position(2, 2); // c6
            Position kingPos = new Position(5, 5);   // f3
            board[bishopPos] = whiteBishop;
            board[kingPos] = blackKing;

            // Act
            bool canCaptureKing = whiteBishop.CanCaptureOpponentKing(bishopPos, board);

            // Assert
            Assert.True(canCaptureKing);
        }
    }
}