using ChessLogic.Moves;
using ChessLogic.Utilities;
using Xunit;

namespace ChessLogic.Tests
{
    // Testklasse für die Ausführung und Legalitätsprüfung verschiedener Zugtypen.
    public class MoveExecutionAndLegalityTests
    {
        // Testfall: Prüft, ob Execute für einen Bauernzug oder Schlagzug true zurückgibt, sonst false.
        [Theory]
        [InlineData(PieceType.Pawn, true)]
        [InlineData(PieceType.Knight, false)]
        public void NormalMoveExecuteReturnsCorrectBoolForPawnOrCapture(PieceType pieceType, bool expectedReturn)
        {
            // Arrange
            Board board = new Board();
            Position from = new Position(6, 0);
            Position to = new Position(5, 0);
            Piece piece = pieceType == PieceType.Pawn ? new Pawn(Player.White) : new Knight(Player.White);
            board[from] = piece;
            Move move = new NormalMove(from, to);

            // Act
            bool result = move.Execute(board);

            // Assert
            Assert.Equal(expectedReturn, result);
            Assert.Null(board[from]);
            Assert.Same(piece, board[to]);
            Assert.True(piece.HasMoved);
        }

        // Testfall: Verifiziert, dass Execute bei einem Schlagzug true zurückgibt.
        [Fact]
        public void NormalMoveExecuteReturnsTrueOnCapture()
        {
            // Arrange
            Board board = new Board();
            board[new Position(3, 3)] = new Rook(Player.White);
            board[new Position(3, 4)] = new Pawn(Player.Black);
            Move move = new NormalMove(new Position(3, 3), new Position(3, 4));
            // Act & Assert
            Assert.True(move.Execute(board));
        }

        // Testfall: Testet die Ausführung eines Bauern-Doppelschritts und die korrekte Setzung der En-Passant-Position.
        [Fact]
        public void DoublePawnExecuteReturnsTrue()
        {
            // Arrange
            Board board = new Board();
            board[new Position(6, 0)] = new Pawn(Player.White);
            Move move = new DoublePawn(new Position(6, 0), new Position(4, 0));
            // Act & Assert
            Assert.True(move.Execute(board));
            Assert.Equal(new Position(5, 0), board.GetPawnSkipPosition(Player.White));
        }

        // Testfall: Testet die korrekte Ausführung eines En-Passant-Schlags.
        [Fact]
        public void EnPassantExecuteReturnsTrue()
        {
            // Arrange
            Board board = new Board();
            Position whitePawnPos = new Position(3, 3);
            Position blackSkippedPos = new Position(2, 4);
            Position blackActualCapturePos = new Position(3, 4);
            board[whitePawnPos] = new Pawn(Player.White);
            board[blackActualCapturePos] = new Pawn(Player.Black);
            board.SetPawnSkipPosition(Player.Black, blackSkippedPos);
            Move move = new EnPassant(whitePawnPos, blackSkippedPos);
            // Act & Assert
            Assert.True(move.Execute(board));
            Assert.Null(board[blackActualCapturePos]);
        }

        // Testfall: Prüft die korrekte Bewegung von König und Turm bei einer Rochade.
        [Fact]
        public void CastleExecuteReturnsFalse()
        {
            // Arrange
            Board board = Board.Initial();
            board[new Position(7, 5)] = null;
            board[new Position(7, 6)] = null;
            Move move = new Castle(MoveType.CastleKS, new Position(7, 4));
            // Act & Assert
            Assert.False(move.Execute(board));
            Assert.Equal(PieceType.King, board[new Position(7, 6)]?.Type);
            Assert.Equal(PieceType.Rook, board[new Position(7, 5)]?.Type);
        }

        // Testfall: Testet die korrekte Ausführung einer Bauernumwandlung.
        [Fact]
        public void PawnPromotionExecuteReturnsTrue()
        {
            // Arrange
            Board board = new Board();
            board[new Position(1, 0)] = new Pawn(Player.White);
            Move move = new PawnPromotion(new Position(1, 0), new Position(0, 0), PieceType.Queen);
            // Act & Assert
            Assert.True(move.Execute(board));
            Assert.Equal(PieceType.Queen, board[new Position(0, 0)]?.Type);
        }

        // Testfall: Überprüft die Bewegung einer Figur bei einem Teleport-Spezialzug.
        [Fact]
        public void TeleportMoveExecuteReturnsFalseAndMovesPiece()
        {
            // Arrange
            Board board = new Board();
            Piece rook = new Rook(Player.White);
            board[new Position(7, 0)] = rook;
            Move move = new TeleportMove(new Position(7, 0), new Position(3, 3));
            // Act & Assert
            Assert.False(move.Execute(board));
            Assert.Null(board[new Position(7, 0)]);
            Assert.Same(rook, board[new Position(3, 3)]);
        }

        // Testfall: Stellt sicher, dass ein Teleport auf ein besetztes Feld nicht legal ist.
        [Fact]
        public void TeleportMoveIsLegalTargetMustBeEmpty()
        {
            // Arrange
            Board board = new Board();
            board[new Position(7, 0)] = new Rook(Player.White);
            board[new Position(3, 3)] = new Pawn(Player.Black);
            Move move = new TeleportMove(new Position(7, 0), new Position(3, 3));
            // Act & Assert
            Assert.False(move.IsLegal(board));
        }

        // Testfall: Prüft die korrekte Vertauschung zweier Figuren bei einem Positionstausch-Spezialzug.
        [Fact]
        public void PositionSwapMoveExecuteReturnsFalseAndSwapsPieces()
        {
            // Arrange
            Board board = new Board();
            Piece rook = new Rook(Player.White);
            Piece knight = new Knight(Player.White);
            board[new Position(7, 0)] = rook;
            board[new Position(7, 1)] = knight;
            Move move = new PositionSwapMove(new Position(7, 0), new Position(7, 1));
            // Act & Assert
            Assert.False(move.Execute(board));
            Assert.Same(knight, board[new Position(7, 0)]);
            Assert.Same(rook, board[new Position(7, 1)]);
        }

        // Testfall: Stellt sicher, dass der Tausch von Figuren unterschiedlicher Farben nicht legal ist.
        [Fact]
        public void PositionSwapMoveIsLegalPiecesMustBeSameColor()
        {
            // Arrange
            Board board = new Board();
            board[new Position(7, 0)] = new Rook(Player.White);
            board[new Position(0, 0)] = new Rook(Player.Black);
            Move move = new PositionSwapMove(new Position(7, 0), new Position(0, 0));
            // Act & Assert
            Assert.False(move.IsLegal(board));
        }

        // Testfall: Verhindert den Tausch einer Figur mit sich selbst.
        [Fact]
        public void PositionSwapMoveIsLegalPositionsMustBeDifferent()
        {
            // Arrange
            Board board = new Board();
            board[new Position(7, 0)] = new Rook(Player.White);
            Move move = new PositionSwapMove(new Position(7, 0), new Position(7, 0));
            // Act & Assert
            Assert.False(move.IsLegal(board));
        }
    }
}