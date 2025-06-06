using ChessLogic.Utilities;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ChessLogic.Tests
{
    // Testklasse für die Funktionalität der König-Figur.
    public class KingTests
    {
        // Testfall: Prüft die 8 möglichen Züge eines Königs von der Brettmitte aus.
        [Fact]
        public void KingOnEmptyBoardFromCenterHas8Moves()
        {
            // Arrange
            Board board = new Board();
            King king = new King(Player.White);
            Position kingPos = new Position(3, 3);
            board[kingPos] = king;

            // Act
            IEnumerable<Move> moves = king.GetMoves(kingPos, board).Where(m => m.Type == MoveType.Normal);

            // Assert
            Assert.Equal(8, moves.Count());
        }

        // Testfall: Prüft die 3 möglichen Züge eines Königs aus einer Ecke.
        [Fact]
        public void KingOnEmptyBoardFromA1Has3Moves()
        {
            // Arrange
            Board board = new Board();
            King king = new King(Player.White);
            Position kingPos = new Position(7, 0);
            board[kingPos] = king;

            // Act
            IEnumerable<Move> moves = king.GetMoves(kingPos, board).Where(m => m.Type == MoveType.Normal);

            // Assert
            Assert.Equal(3, moves.Count());
        }

        // Testfall: Stellt sicher, dass ein König eine gegnerische Figur schlagen, aber keine eigene Figur besetzen kann.
        [Fact]
        public void KingCanCaptureAdjacentOpponent()
        {
            // Arrange
            Board board = new Board();
            King whiteKing = new King(Player.White);
            Position kingPos = new Position(3, 3);
            board[kingPos] = whiteKing;
            board[new Position(2, 2)] = new Pawn(Player.Black);
            board[new Position(2, 3)] = new Pawn(Player.White);

            // Act
            IEnumerable<Move> moves = whiteKing.GetMoves(kingPos, board).Where(m => m.Type == MoveType.Normal);

            // Assert
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(2, 2)));
            Assert.DoesNotContain(moves, m => m.ToPos.Equals(new Position(2, 3)));
            Assert.Equal(7, moves.Count());
        }

        // Testfall: Verhindert, dass der König auf ein Feld zieht, das von einem Gegner angegriffen wird.
        [Fact]
        public void KingCannotMoveIntoCheck()
        {
            // Arrange: Baut eine Stellung auf, in der die meisten Königsfelder bedroht sind.
            Board board = new Board();
            King whiteKing = new King(Player.White);
            Position kingPos = new Position(7, 4);
            board[kingPos] = whiteKing;
            board[new Position(0, 3)] = new Rook(Player.Black);
            board[new Position(0, 5)] = new Rook(Player.Black);
            GameState gameState = new GameState(Player.White, board);

            // Act: Ruft die legalen Züge ab.
            IEnumerable<Move> legalMoves = gameState.LegalMovesForPiece(kingPos)
                                                     .Where(m => m.Type == MoveType.Normal);

            // Assert: Nur ein Zug (nach e2) sollte legal sein.
            Assert.Single(legalMoves);
            Assert.Contains(legalMoves, m => m.ToPos.Equals(new Position(6, 4)));
        }

        // Testfall: Testet, ob die Methode zur Erkennung eines Königsangriffs korrekt funktioniert.
        [Fact]
        public void KingCanCaptureOpponentKingTheoretically()
        {
            // Arrange: Positioniert zwei Könige nebeneinander.
            Board board = new Board();
            King whiteKing = new King(Player.White);
            King blackKing = new King(Player.Black);
            Position whiteKingPos = new Position(3, 3);
            Position blackKingPos = new Position(2, 2);
            board[whiteKingPos] = whiteKing;
            board[blackKingPos] = blackKing;

            // Act: Prüft, ob der weisse König den schwarzen bedroht.
            bool canCapture = whiteKing.CanCaptureOpponentKing(whiteKingPos, board);

            // Assert: Die Bedrohung sollte erkannt werden.
            Assert.True(canCapture);
        }
    }
}