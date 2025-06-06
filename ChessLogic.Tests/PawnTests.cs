using ChessLogic.Utilities;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ChessLogic.Tests
{
    // Testklasse für die Funktionalität der Bauern-Figur.
    public class PawnTests
    {
        // Testfall: Testet den normalen und den doppelten Vorwärtszug eines Bauern von seiner Startposition.
        [Fact]
        public void WhitePawnInitialForwardMoves()
        {
            // Arrange
            Board board = new Board();
            board[new Position(6, 4)] = new Pawn(Player.White);
            // Act
            IEnumerable<Move> moves = board[new Position(6, 4)]!.GetMoves(new Position(6, 4), board);
            // Assert
            Assert.Equal(2, moves.Count());
            Assert.Contains(moves, m => m.Type == MoveType.Normal && m.ToPos.Equals(new Position(5, 4)));
            Assert.Contains(moves, m => m.Type == MoveType.DoublePawn && m.ToPos.Equals(new Position(4, 4)));
        }

        // Testfall: Stellt sicher, dass ein Bauer nach seinem ersten Zug nur noch einen Schritt vorwärts ziehen kann.
        [Fact]
        public void WhitePawnForwardMoveAfterInitial()
        {
            // Arrange
            Board board = new Board();
            board[new Position(5, 4)] = new Pawn(Player.White) { HasMoved = true };
            // Act
            IEnumerable<Move> moves = board[new Position(5, 4)]!.GetMoves(new Position(5, 4), board);
            // Assert
            Assert.Single(moves);
            Assert.Contains(moves, m => m.Type == MoveType.Normal && m.ToPos.Equals(new Position(4, 4)));
        }

        // Testfall: Verifiziert, dass ein Bauer beim Erreichen der letzten Reihe Umwandlungszüge generiert.
        [Fact]
        public void WhitePawnGeneratesPromotionMovesOnLastRank()
        {
            // Arrange
            Board board = new Board();
            board[new Position(1, 4)] = new Pawn(Player.White);
            // Act
            IEnumerable<Move> moves = board[new Position(1, 4)]!.GetMoves(new Position(1, 4), board);
            // Assert
            Assert.Equal(4, moves.Count(m => m is PawnPromotion));
            Assert.Contains(moves, m => m is PawnPromotion p && p.PromotionTo == PieceType.Queen);
        }

        // Testfall: Testet das diagonale Schlagen eines Bauern.
        [Fact]
        public void WhitePawnDiagonalCapture()
        {
            // Arrange
            Board board = new Board();
            board[new Position(3, 3)] = new Pawn(Player.White) { HasMoved = true };
            board[new Position(2, 2)] = new Pawn(Player.Black);
            board[new Position(2, 4)] = new Rook(Player.Black);
            // Act
            IEnumerable<Move> moves = board[new Position(3, 3)]!.GetMoves(new Position(3, 3), board);
            // Assert
            Assert.Equal(3, moves.Count());
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(2, 2))); // Schlag
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(2, 4))); // Schlag
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(2, 3))); // Vorwärts
        }

        // Testfall: Stellt sicher, dass ein Bauern-Doppelschritt die korrekte En-Passant-Position setzt.
        [Fact]
        public void DoublePawnMoveCorrectlySetsPawnSkipPosition()
        {
            // Arrange
            Board board = new Board();
            board[new Position(6, 4)] = new Pawn(Player.White);
            Move doublePawnMove = new DoublePawn(new Position(6, 4), new Position(4, 4));
            // Act
            doublePawnMove.Execute(board);
            // Assert
            Assert.Equal(new Position(5, 4), board.GetPawnSkipPosition(Player.White));
        }

        // Testfall: Prüft, ob ein Bauer einen En-Passant-Schlag ausführen kann.
        [Fact]
        public void PawnCanPerformEnPassantCapture()
        {
            // Arrange
            Board board = new Board();
            board[new Position(3, 3)] = new Pawn(Player.White);
            board[new Position(1, 2)] = new Pawn(Player.Black);
            new DoublePawn(new Position(1, 2), new Position(3, 2)).Execute(board);
            // Act
            IEnumerable<Move> moves = board[new Position(3, 3)]!.GetMoves(new Position(3, 3), board);
            // Assert
            Assert.Contains(moves, m => m.Type == MoveType.EnPassant && m.ToPos.Equals(new Position(2, 2)));
        }
    }
}