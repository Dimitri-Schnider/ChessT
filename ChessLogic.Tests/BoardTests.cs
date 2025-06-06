using ChessLogic.Utilities;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ChessLogic.Tests
{
    // Testklasse für die Funktionalität des Schachbretts (Board).
    public class BoardTests
    {
        // Testfall: Stellt sicher, dass Positionen korrekt als innerhalb oder außerhalb des 8x8-Bretts erkannt werden.
        [Theory]
        [InlineData(0, 0, true)]
        [InlineData(7, 7, true)]
        [InlineData(-1, 0, false)]
        [InlineData(7, 8, false)]
        public void IsInsideCorrectlyIdentifiesPositions(int row, int col, bool expectedIsInside)
        {
            // Arrange
            Position pos = new Position(row, col);
            // Act
            bool actualIsInside = Board.IsInside(pos);
            // Assert
            Assert.Equal(expectedIsInside, actualIsInside);
        }

        // Testfall: Prüft, ob leere und besetzte Felder in der Grundaufstellung korrekt identifiziert werden.
        [Fact]
        public void IsEmptyCorrectlyIdentifiesSquaresOnInitialBoard()
        {
            // Arrange
            Board board = Board.Initial();
            // Act & Assert
            Assert.False(board.IsEmpty(new Position(0, 0)));
            Assert.False(board.IsEmpty(new Position(6, 4)));
            Assert.True(board.IsEmpty(new Position(3, 3)));
        }

        // Testfall: Verifiziert die korrekte Positionierung von Schlüsselfiguren in der Grundaufstellung.
        [Fact]
        public void InitialBoardSetupHasCorrectKeyPieces()
        {
            // Arrange
            Board board = Board.Initial();
            // Act & Assert
            Assert.Equal(PieceType.Rook, board[new Position(7, 0)]?.Type);
            Assert.Equal(Player.White, board[new Position(7, 0)]?.Color);
            Assert.Equal(PieceType.King, board[new Position(7, 4)]?.Type);
            Assert.All(Enumerable.Range(0, 8), col => Assert.Equal(PieceType.Pawn, board[new Position(6, col)]?.Type));
            Assert.Equal(PieceType.Queen, board[new Position(0, 3)]?.Type);
            Assert.All(Enumerable.Range(0, 8), col => Assert.Equal(Player.Black, board[new Position(1, col)]?.Color));
        }

        // Testfall: Testet das Setzen und Abrufen der En-Passant-Sprungposition für beide Spieler.
        [Fact]
        public void GetAndSetPawnSkipPositionWorksCorrectly()
        {
            // Arrange
            Board board = new Board();
            Position skipPos = new Position(2, 3);
            // Act & Assert
            board.SetPawnSkipPosition(Player.White, skipPos);
            Assert.Equal(skipPos, board.GetPawnSkipPosition(Player.White));
            board.SetPawnSkipPosition(Player.White, null);
            Assert.Null(board.GetPawnSkipPosition(Player.White));
        }

        // Testfall: Stellt sicher, dass Board.Copy() eine tiefe, unabhängige Kopie des Bretts erstellt.
        [Fact]
        public void CopyCreatesDeepCloneOfBoard()
        {
            // Arrange: Erstellt ein Originalbrett und modifiziert es.
            Board originalBoard = Board.Initial();
            Position pawnPos = new Position(1, 0);
            originalBoard.SetPawnSkipPosition(Player.Black, new Position(2, 0));
            if (originalBoard[pawnPos] is Piece originalPawn) originalPawn.HasMoved = true;

            // Act: Erstellt eine Kopie.
            Board copiedBoard = originalBoard.Copy();

            // Assert: Überprüft, ob die Kopie identische Werte, aber unterschiedliche Referenzen hat.
            Assert.NotSame(originalBoard[pawnPos], copiedBoard[pawnPos]);
            Assert.Equal(originalBoard.GetPawnSkipPosition(Player.Black), copiedBoard.GetPawnSkipPosition(Player.Black));

            // Act: Modifiziert die Kopie.
            copiedBoard[pawnPos] = null;
            // Assert: Stellt sicher, dass das Original unverändert bleibt.
            Assert.NotNull(originalBoard[pawnPos]);
        }

        // Testfall: Überprüft, ob alle 32 Figurenpositionen in der Grundaufstellung gefunden werden.
        [Fact]
        public void PiecePositionsReturnsAll32PiecePositionsOnInitialBoard()
        {
            // Arrange
            Board board = Board.Initial();
            // Act
            int pieceCount = board.PiecePositions().Count();
            // Assert
            Assert.Equal(32, pieceCount);
        }

        // Testfall: Zählt die Figuren eines bestimmten Spielers in der Grundaufstellung.
        [Theory]
        [InlineData(Player.White, 16)]
        [InlineData(Player.Black, 16)]
        public void PiecePositionsForPlayerReturnsCorrectNumberOfPositions(Player player, int expectedCount)
        {
            // Arrange
            Board board = Board.Initial();
            // Act
            List<Position> playerPiecePositions = board.PiecePositionsFor(player).ToList();
            // Assert
            Assert.Equal(expectedCount, playerPiecePositions.Count);
            Assert.All(playerPiecePositions, pos => Assert.Equal(player, board[pos]?.Color));
        }

        // Testfall: Verifiziert die korrekte Zählung aller Figurentypen mit Board.CountPieces().
        [Fact]
        public void CountPiecesReturnsCorrectCountsForInitialBoard()
        {
            // Arrange
            Board board = Board.Initial();
            // Act
            Counting counts = board.CountPieces();
            // Assert
            Assert.Equal(8, counts.White(PieceType.Pawn));
            Assert.Equal(1, counts.White(PieceType.Queen));
            Assert.Equal(8, counts.Black(PieceType.Pawn));
            Assert.Equal(1, counts.Black(PieceType.King));
            Assert.Equal(32, counts.TotalCount);
        }

        // Testfall: Überprüft die initialen Rochaderechte für beide Spieler.
        [Theory]
        [InlineData(Player.White, true)]
        [InlineData(Player.Black, true)]
        public void InitialCastlingRightsAreCorrect(Player player, bool expectedCanCastle)
        {
            // Arrange
            Board board = Board.Initial();
            // Act
            bool canCastleKS = board.CastleRightKS(player);
            bool canCastleQS = board.CastleRightQS(player);
            // Assert
            Assert.Equal(expectedCanCastle, canCastleKS);
            Assert.Equal(expectedCanCastle, canCastleQS);
        }

        // Testfall: Stellt sicher, dass die Rochaderechte nach einer relevanten Figurenbewegung erlöschen.
        [Fact]
        public void CastlingRightsChangeAfterPieceMove()
        {
            // Arrange: Simuliert die Bewegung des weissen Königs.
            Board board = Board.Initial();
            if (board[new Position(7, 4)] is Piece king) king.HasMoved = true;
            // Assert
            Assert.False(board.CastleRightKS(Player.White));
            Assert.False(board.CastleRightQS(Player.White));

            // Arrange: Simuliert die Bewegung eines schwarzen Turms.
            board = Board.Initial();
            if (board[new Position(0, 0)] is Piece rook) rook.HasMoved = true;
            // Assert
            Assert.True(board.CastleRightKS(Player.Black));
            Assert.False(board.CastleRightQS(Player.Black));
        }

        // Testfall: Prüft, dass Matt mit Dame und König möglich ist (kein unzureichendes Material).
        [Fact]
        public void NotInsufficientMaterialKingAndQueenVsKing()
        {
            // Arrange
            Board board = new Board();
            board[new Position(0, 0)] = new King(Player.White);
            board[new Position(1, 0)] = new Queen(Player.White);
            board[new Position(7, 7)] = new King(Player.Black);
            // Act & Assert
            Assert.False(board.InsufficientMaterial());
        }

        // Testfall: Prüft, dass Matt mit Turm und König möglich ist (kein unzureichendes Material).
        [Fact]
        public void NotInsufficientMaterialKingAndRookVsKing()
        {
            // Arrange
            Board board = new Board();
            board[new Position(0, 0)] = new King(Player.White);
            board[new Position(1, 0)] = new Rook(Player.White);
            board[new Position(7, 7)] = new King(Player.Black);
            // Act & Assert
            Assert.False(board.InsufficientMaterial());
        }

        // Testfall: Prüft die korrekte Erkennung einer möglichen En-Passant-Situation.
        [Fact]
        public void CanCaptureEnPassantReturnsTrueWhenPossible()
        {
            // Arrange: Stellt eine En-Passant-Situation her.
            Board board = new Board();
            board[new Position(3, 3)] = new Pawn(Player.White);
            board[new Position(1, 2)] = new Pawn(Player.Black);
            Move blackDoubleStep = new DoublePawn(new Position(1, 2), new Position(3, 2));
            blackDoubleStep.Execute(board);
            // Act & Assert
            Assert.True(board.CanCaptureEnPassant(Player.White));
        }

        // Testfall: Stellt sicher, dass ohne gültigen En-Passant-Kontext keine Möglichkeit erkannt wird.
        [Fact]
        public void CanCaptureEnPassantReturnsFalseWhenNotPossible()
        {
            // Arrange
            Board board = Board.Initial();
            // Act & Assert
            Assert.False(board.CanCaptureEnPassant(Player.White));
            Assert.False(board.CanCaptureEnPassant(Player.Black));
        }

        // Testfall: Stellt sicher, dass die En-Passant-Möglichkeit nach einem Zug verfällt.
        [Fact]
        public void CanCaptureEnPassantReturnsFalseAfterOneMoreMove()
        {
            // Arrange
            Board board = new Board();
            board[new Position(3, 3)] = new Pawn(Player.White);
            board[new Position(1, 2)] = new Pawn(Player.Black);
            new DoublePawn(new Position(1, 2), new Position(3, 2)).Execute(board);
            // Simuliere einen anderen Zug, der die EP-Möglichkeit verfallen lässt.
            board.SetPawnSkipPosition(Player.Black, null);
            // Act & Assert
            Assert.False(board.CanCaptureEnPassant(Player.White));
        }
    }
}