using ChessLogic.Utilities;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ChessLogic.Tests
{
    // Testklasse für die Funktionalität der Rochade.
    public class CastlingTests
    {
        // Testfall: Testet die kurze Rochade für Weiß bei freien Feldern.
        [Fact]
        public void WhiteCanCastleKingSideWhenConditionsMet()
        {
            // Arrange: Initialisiert ein Brett und macht den Weg für die Rochade frei.
            Board board = Board.Initial();
            board[new Position(7, 5)] = null; // f1
            board[new Position(7, 6)] = null; // g1
            Piece? kingPiece = board[new Position(7, 4)];
            Assert.IsType<King>(kingPiece);
            King whiteKing = (King)kingPiece!;

            // Act: Ruft die möglichen Züge für den König ab.
            IEnumerable<Move> kingMoves = whiteKing.GetMoves(new Position(7, 4), board);
            Move? castleMove = kingMoves.FirstOrDefault(m => m.Type == MoveType.CastleKS);

            // Assert: Stellt sicher, dass die Rochade als legaler Zug angeboten wird.
            Assert.NotNull(castleMove);
            Assert.True(castleMove.IsLegal(board));
        }

        // Testfall: Testet die lange Rochade für Schwarz bei freien Feldern.
        [Fact]
        public void BlackCanCastleQueenSideWhenConditionsMet()
        {
            // Arrange: Initialisiert ein Brett und macht den Weg für die Rochade frei.
            Board board = Board.Initial();
            board[new Position(0, 1)] = null;
            board[new Position(0, 2)] = null;
            board[new Position(0, 3)] = null;
            Piece? kingPiece = board[new Position(0, 4)];
            Assert.IsType<King>(kingPiece);
            King blackKing = (King)kingPiece!;

            // Act: Ruft die möglichen Züge ab.
            IEnumerable<Move> kingMoves = blackKing.GetMoves(new Position(0, 4), board);
            Move? castleMove = kingMoves.FirstOrDefault(m => m.Type == MoveType.CastleQS);

            // Assert: Stellt sicher, dass die Rochade als legaler Zug angeboten wird.
            Assert.NotNull(castleMove);
            Assert.True(castleMove.IsLegal(board));
        }

        // Testfall: Stellt sicher, dass keine Rochade möglich ist, wenn der König bereits gezogen hat.
        [Fact]
        public void NoCastlingIfKingHasMoved()
        {
            // Arrange: Simuliert, dass der König bereits gezogen hat.
            Board board = Board.Initial();
            board[new Position(7, 5)] = null;
            board[new Position(7, 6)] = null;
            Piece? kingPiece = board[new Position(7, 4)];
            Assert.IsType<King>(kingPiece);
            King whiteKing = (King)kingPiece!;
            whiteKing.HasMoved = true;

            // Act: Ruft die Züge ab.
            IEnumerable<Move> kingMoves = whiteKing.GetMoves(new Position(7, 4), board);

            // Assert: Die Rochade darf nicht unter den Zügen sein.
            Assert.Null(kingMoves.FirstOrDefault(m => m.Type == MoveType.CastleKS));
        }

        // Testfall: Stellt sicher, dass keine Rochade möglich ist, wenn der relevante Turm bereits gezogen hat.
        [Fact]
        public void NoCastlingIfRookHasMoved()
        {
            // Arrange: Simuliert, dass der Turm bereits gezogen hat.
            Board board = Board.Initial();
            board[new Position(7, 5)] = null;
            board[new Position(7, 6)] = null;
            Piece? rookPiece = board[new Position(7, 7)];
            Assert.IsType<Rook>(rookPiece);
            Rook whiteRookKS = (Rook)rookPiece!;
            whiteRookKS.HasMoved = true;

            // Act
            Piece? kingPiece = board[new Position(7, 4)];
            Assert.IsType<King>(kingPiece);
            King whiteKing = (King)kingPiece!;
            IEnumerable<Move> kingMoves = whiteKing.GetMoves(new Position(7, 4), board);

            // Assert
            Assert.Null(kingMoves.FirstOrDefault(m => m.Type == MoveType.CastleKS));
        }

        // Testfall: Prüft, ob die Rochade durch blockierte Felder verhindert wird.
        [Fact]
        public void NoCastlingIfPathIsBlocked()
        {
            // Arrange: Verwendet die Grundaufstellung, in der die Felder blockiert sind.
            Board board = Board.Initial();
            Piece? kingPiece = board[new Position(7, 4)];
            Assert.IsType<King>(kingPiece);
            King whiteKing = (King)kingPiece!;

            // Act
            IEnumerable<Move> kingMoves = whiteKing.GetMoves(new Position(7, 4), board);

            // Assert
            Assert.Null(kingMoves.FirstOrDefault(m => m.Type == MoveType.CastleKS));
        }

        // Testfall: Verhindert die Rochade, wenn der König im Schach steht.
        [Fact]
        public void NoCastlingIfKingIsInCheck()
        {
            // Arrange: Stellt eine Schachsituation her.
            Board board = Board.Initial();
            board[new Position(7, 5)] = null;
            board[new Position(7, 6)] = null;
            board[new Position(6, 4)] = null;
            board[new Position(5, 4)] = new Rook(Player.Black);
            Assert.True(board.IsInCheck(Player.White));

            // Act
            Piece? kingPiece = board[new Position(7, 4)];
            Assert.IsType<King>(kingPiece);
            King whiteKing = (King)kingPiece!;
            IEnumerable<Move> kingMoves = whiteKing.GetMoves(new Position(7, 4), board);

            // Assert
            Assert.Null(kingMoves.FirstOrDefault(m => m.Type == MoveType.CastleKS && m.IsLegal(board)));
        }
    }
}