using Xunit;
using ChessLogic;
using ChessLogic.Utilities;
using System.Linq;
using System.Collections.Generic;

namespace ChessLogic.Tests
{
    public class QueenTests
    {
        [Fact]
        public void QueenOnEmptyBoardFromCenterHasMaxMoves() // (7 horizontal + 7 vertikal + 7 NW-SO + 9 NO-SW) Max=27
        {
            // Arrange
            Board board = new Board();
            Queen queen = new Queen(Player.White);
            Position queenPos = new Position(3, 3); // d5
            board[queenPos] = queen;

            // Act
            IEnumerable<Move> moves = queen.GetMoves(queenPos, board);

            // Assert
            // Horizontal: 7, Vertikal: 7
            // Diagonal 1 (d5-a8, d5-h1): (3+4) = 7
            // Diagonal 2 (d5-a2, d5-g8): (3+4) = 7  (e.g. from d5 to h1 = 4 moves; from d5 to a2 = 3 moves)
            // From d5 (3,3):
            // Horizontal: (3,0)..(3,2) (3) + (3,4)..(3,7) (4) = 7
            // Vertikal: (0,3)..(2,3) (3) + (4,3)..(7,3) (4) = 7
            // Diag NW-SE: (0,0)..(2,2) (3) + (4,4)..(7,7) (4) = 7
            // Diag NE-SW: (0,6)..(2,4) (3) + (4,2)..(6,0) (3) = 6 (Fehler in meiner manuellen Zählung oben)
            // d5 (3,3) to a2 (6,0) = 3 moves
            // d5 (3,3) to h7 (1,7) = 4 moves
            // total 7 + 7 + 7 + 7 = 28-1 = 27 (da ein Feld mehrfach gezählt wird)
            // Felder auf d-Linie (ohne d5): (0,3),(1,3),(2,3), (4,3),(5,3),(6,3),(7,3) -> 7
            // Felder auf 5-Reihe (ohne d5): (3,0),(3,1),(3,2), (3,4),(3,5),(3,6),(3,7) -> 7
            // Diagonale d5-a8: c6, b7, a8 -> 3
            // Diagonale d5-h1: e4, f3, g2, h1 -> 4
            // Diagonale d5-a2: c4, b3, a2 -> 3
            // Diagonale d5-h7: e6, f7, g8 -> 3 (nicht h7, sondern g8) e6,f7,g8
            // D(3,3) -> (0,6) g8
            Assert.Equal((8 - 1) + (8 - 1) + System.Math.Min(3, 3) + System.Math.Min(3, 7 - 3) + System.Math.Min(7 - 3, 3) + System.Math.Min(7 - 3, 7 - 3), moves.Count());
            // = 7 + 7 + 3+3 + 3+4 = 14 + 6 + 7 = 27
            Assert.Equal(27, moves.Count());
        }

        [Fact]
        public void QueenIsBlockedAndCanCapture()
        {
            // Arrange
            Board board = new Board();
            Queen whiteQueen = new Queen(Player.White);
            Position queenPos = new Position(3, 3); // d5
            board[queenPos] = whiteQueen;

            board[new Position(3, 5)] = new Pawn(Player.White); // Eigener Bauer auf f5 (blockiert rechts)
            board[new Position(1, 3)] = new Pawn(Player.Black); // Gegnerischer Bauer auf d7 (kann geschlagen werden, blockiert oben)
            board[new Position(1, 1)] = new Pawn(Player.White); // Eigener Bauer auf b7 (blockiert NW)
            board[new Position(5, 1)] = new Pawn(Player.Black); // Gegnerischer Bauer auf b3 (kann geschlagen werden, blockiert SW)

            // Act
            IEnumerable<Move> moves = whiteQueen.GetMoves(queenPos, board);

            // Assert
            // Horizontal nach links: c5,b5,a5 (3)
            // Horizontal nach rechts: e5 (1) (f5 blockiert)
            // Vertikal nach unten: d4,d3,d2,d1 (4)
            // Vertikal nach oben: d6, d7(Schlag) (2)
            // NW: c6 (1) (b7 blockiert)
            // NE: e6,f7,g8 (3)
            // SW: c4, b3(Schlag) (2)
            // SE: e4,f3,g2,h1 (4)
            // Total = 3 + 1 + 4 + 2 + 1 + 3 + 2 + 4 = 20
            Assert.Equal(20, moves.Count());
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(1, 3))); // Schlag auf d7
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(5, 1))); // Schlag auf b3
            Assert.DoesNotContain(moves, m => m.ToPos.Equals(new Position(3, 6))); // g5 nicht erreichbar
            Assert.DoesNotContain(moves, m => m.ToPos.Equals(new Position(0, 0))); // a8 nicht erreichbar
        }
        [Fact]
        public void QueenCanCaptureOpponentKing()
        {
            // Arrange
            Board board = new Board();
            Queen whiteQueen = new Queen(Player.White);
            King blackKing = new King(Player.Black);
            Position queenPos = new Position(3, 3); // d5
            Position kingPos = new Position(0, 3);   // d8 (vertikal)
            board[queenPos] = whiteQueen;
            board[kingPos] = blackKing;

            // Act
            bool canCaptureKing = whiteQueen.CanCaptureOpponentKing(queenPos, board);

            // Assert
            Assert.True(canCaptureKing);

            // Diagonal
            board[kingPos] = null;
            kingPos = new Position(0, 6); // g8
            board[kingPos] = blackKing;
            canCaptureKing = whiteQueen.CanCaptureOpponentKing(queenPos, board);
            Assert.True(canCaptureKing);
        }
    }
}