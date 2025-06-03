// File: [SolutionDir]/ChessLogic.Tests/CastlingTests.cs
using Xunit;
using ChessLogic;
using ChessLogic.Utilities;
using System.Linq;
using System.Collections.Generic;

namespace ChessLogic.Tests
{
    // Testklasse für die Rochade-Funktionalität
    public class CastlingTests
    {
        // Testfall: Erfolgreiche kurze Rochade für Weiß
        [Fact]
        public void WhiteCanCastleKingSideWhenConditionsMet()
        {
            // Arrange
            Board board = Board.Initial();
            // Entferne Springer und Läufer zwischen König und Turm
            board[new Position(7, 5)] = null; // f1
            board[new Position(7, 6)] = null; // g1

            Piece? kingPiece = board[new Position(7, 4)]; // e1
            Assert.NotNull(kingPiece); // Sicherstellen, dass der König da ist
            Assert.IsType<King>(kingPiece); // Sicherstellen, dass es ein König ist
            King whiteKing = (King)kingPiece;

            IEnumerable<Move> kingMoves = whiteKing.GetMoves(new Position(7, 4), board);

            // Act
            Move? castleMove = kingMoves.FirstOrDefault(m => m.Type == MoveType.CastleKS);

            // Assert
            Assert.NotNull(castleMove);
            Assert.True(castleMove.IsLegal(board));
        }

        // Testfall: Erfolgreiche lange Rochade für Schwarz
        [Fact]
        public void BlackCanCastleQueenSideWhenConditionsMet()
        {
            // Arrange
            Board board = Board.Initial();
            // Entferne Figuren zwischen König und Turm
            board[new Position(0, 1)] = null; // b8
            board[new Position(0, 2)] = null; // c8
            board[new Position(0, 3)] = null; // d8

            Piece? kingPiece = board[new Position(0, 4)]; // e8
            Assert.NotNull(kingPiece);
            Assert.IsType<King>(kingPiece);
            King blackKing = (King)kingPiece;

            IEnumerable<Move> kingMoves = blackKing.GetMoves(new Position(0, 4), board);

            // Act
            Move? castleMove = kingMoves.FirstOrDefault(m => m.Type == MoveType.CastleQS);

            // Assert
            Assert.NotNull(castleMove);
            Assert.True(castleMove.IsLegal(board));
        }

        // Testfall: Keine Rochade, wenn König bereits gezogen hat
        [Fact]
        public void NoCastlingIfKingHasMoved()
        {
            // Arrange
            Board board = Board.Initial();
            board[new Position(7, 5)] = null;
            board[new Position(7, 6)] = null;

            Piece? kingPiece = board[new Position(7, 4)];
            Assert.NotNull(kingPiece);
            Assert.IsType<King>(kingPiece);
            King whiteKing = (King)kingPiece;
            whiteKing.HasMoved = true; // König hat bereits gezogen

            // Act
            IEnumerable<Move> kingMoves = whiteKing.GetMoves(new Position(7, 4), board);
            Move? castleMoveKS = kingMoves.FirstOrDefault(m => m.Type == MoveType.CastleKS);

            // Assert
            Assert.Null(castleMoveKS);
        }

        // Testfall: Keine Rochade, wenn der entsprechende Turm bereits gezogen hat
        [Fact]
        public void NoCastlingIfRookHasMoved()
        {
            // Arrange
            Board board = Board.Initial();
            board[new Position(7, 5)] = null;
            board[new Position(7, 6)] = null;

            Piece? rookPiece = board[new Position(7, 7)]; // h1
            Assert.NotNull(rookPiece);
            Assert.IsType<Rook>(rookPiece);
            Rook whiteRookKS = (Rook)rookPiece;
            whiteRookKS.HasMoved = true; // Turm hat bereits gezogen

            Piece? kingPiece = board[new Position(7, 4)];
            Assert.NotNull(kingPiece);
            Assert.IsType<King>(kingPiece);
            King whiteKing = (King)kingPiece;

            IEnumerable<Move> kingMoves = whiteKing.GetMoves(new Position(7, 4), board);

            // Act
            Move? castleMoveKS = kingMoves.FirstOrDefault(m => m.Type == MoveType.CastleKS);

            // Assert
            Assert.Null(castleMoveKS);
        }

        // Testfall: Keine Rochade, wenn Felder zwischen König und Turm blockiert sind
        [Fact]
        public void NoCastlingIfPathIsBlocked()
        {
            // Arrange
            Board board = Board.Initial();
            // Felder sind NICHT frei, Standardaufstellung (f1 ist Läufer)

            Piece? kingPiece = board[new Position(7, 4)];
            Assert.NotNull(kingPiece);
            Assert.IsType<King>(kingPiece);
            King whiteKing = (King)kingPiece;

            IEnumerable<Move> kingMoves = whiteKing.GetMoves(new Position(7, 4), board);

            // Act
            Move? castleMoveKS = kingMoves.FirstOrDefault(m => m.Type == MoveType.CastleKS);

            // Assert
            Assert.Null(castleMoveKS);
        }

        // Testfall: Keine Rochade, wenn König im Schach steht
        [Fact]
        public void NoCastlingIfKingIsInCheck()
        {
            // Arrange
            Board board = Board.Initial();
            board[new Position(7, 5)] = null; // f1 frei
            board[new Position(7, 6)] = null; // g1 frei
            board[new Position(6, 4)] = null; // e2 Bauer weg
            board[new Position(5, 4)] = new Rook(Player.Black); // Schwarzer Turm auf e3 setzt König auf e1 Schach

            Piece? kingPiece = board[new Position(7, 4)]; // e1
            Assert.NotNull(kingPiece);
            Assert.IsType<King>(kingPiece);
            King whiteKing = (King)kingPiece;

            Assert.True(board.IsInCheck(Player.White));

            // Act
            IEnumerable<Move> kingMoves = whiteKing.GetMoves(new Position(7, 4), board);
            Move? castleMoveKS = kingMoves.FirstOrDefault(m => m.Type == MoveType.CastleKS && m.IsLegal(board));

            // Assert
            Assert.Null(castleMoveKS);
        }
    }
}