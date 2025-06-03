using Xunit;
using ChessLogic;
using ChessLogic.Utilities;
using System.Linq;
using System.Collections.Generic;

namespace ChessLogic.Tests
{
    public class KingTests
    {
        [Fact]
        public void KingOnEmptyBoardFromCenterHas8Moves()
        {
            // Arrange
            Board board = new Board();
            King king = new King(Player.White);
            Position kingPos = new Position(3, 3); // d5
            board[kingPos] = king;

            // Act
            IEnumerable<Move> moves = king.GetMoves(kingPos, board).Where(m => m.Type == MoveType.Normal);

            // Assert
            Assert.Equal(8, moves.Count());
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(2, 2))); // c6
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(2, 3))); // d6
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(2, 4))); // e6
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(3, 2))); // c5
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(3, 4))); // e5
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(4, 2))); // c4
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(4, 3))); // d4
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(4, 4))); // e4
        }

        [Fact]
        public void KingOnEmptyBoardFromA1Has3Moves()
        {
            // Arrange
            Board board = new Board();
            King king = new King(Player.White);
            Position kingPos = new Position(7, 0); // a1
            board[kingPos] = king;

            // Act
            IEnumerable<Move> moves = king.GetMoves(kingPos, board).Where(m => m.Type == MoveType.Normal);

            // Assert
            Assert.Equal(3, moves.Count());
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(6, 0))); // a2
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(6, 1))); // b2
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(7, 1))); // b1
        }

        [Fact]
        public void KingCanCaptureAdjacentOpponent()
        {
            // Arrange
            Board board = new Board();
            King whiteKing = new King(Player.White);
            Position kingPos = new Position(3, 3); // d5
            board[kingPos] = whiteKing;
            board[new Position(2, 2)] = new Pawn(Player.Black); // c6 (gegnerisch)
            board[new Position(2, 3)] = new Pawn(Player.White); // d6 (eigen)

            // Act
            IEnumerable<Move> moves = whiteKing.GetMoves(kingPos, board).Where(m => m.Type == MoveType.Normal);

            // Assert
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(2, 2))); // Kann c6 schlagen
            Assert.DoesNotContain(moves, m => m.ToPos.Equals(new Position(2, 3))); // Kann nicht auf d6 ziehen (blockiert)
            Assert.Equal(7, moves.Count()); // 8 mögliche - 1 blockiert
        }

        [Fact]
        public void KingCannotMoveIntoCheck()
        {
            // Arrange
            Board board = new Board();
            King whiteKing = new King(Player.White);
            Position kingPos = new Position(7, 4); // e1
            board[kingPos] = whiteKing;
            board[new Position(0, 3)] = new Rook(Player.Black); // Schwarzer Turm auf d8 (kontrolliert d-Linie)
            board[new Position(0, 5)] = new Rook(Player.Black); // Schwarzer Turm auf f8 (kontrolliert f-Linie)


            GameState gameState = new GameState(Player.White, board);

            // Act
            // Züge von e1: d1, d2, e2, f2, f1
            // d1, d2 sind von Rd8 bedroht
            // f1, f2 sind von Rf8 bedroht
            // Nur e2 sollte legal sein (ohne Rochade)
            IEnumerable<Move> legalMoves = gameState.LegalMovesForPiece(kingPos)
                                                  .Where(m => m.Type == MoveType.Normal);

            // Assert
            Assert.DoesNotContain(legalMoves, m => m.ToPos.Equals(new Position(7, 3))); // d1
            Assert.DoesNotContain(legalMoves, m => m.ToPos.Equals(new Position(6, 3))); // d2
            Assert.DoesNotContain(legalMoves, m => m.ToPos.Equals(new Position(7, 5))); // f1
            Assert.DoesNotContain(legalMoves, m => m.ToPos.Equals(new Position(6, 5))); // f2
            Assert.Contains(legalMoves, m => m.ToPos.Equals(new Position(6, 4)));    // e2
            Assert.Single(legalMoves);
        }
        [Fact]
        public void KingCanCaptureOpponentKingTheoretically() // Für IsInCheck Logik
        {
            // Arrange
            Board board = new Board();
            King whiteKing = new King(Player.White);
            King blackKing = new King(Player.Black);
            Position whiteKingPos = new Position(3, 3); // d5
            Position blackKingPos = new Position(2, 2); // c6 (angrenzend)
            board[whiteKingPos] = whiteKing;
            board[blackKingPos] = blackKing;

            // Act
            // Diese Methode prüft, ob der König das Feld des anderen Königs *bedroht*, nicht ob der Zug legal ist.
            bool canCapture = whiteKing.CanCaptureOpponentKing(whiteKingPos, board);

            // Assert
            Assert.True(canCapture);
        }
    }
}