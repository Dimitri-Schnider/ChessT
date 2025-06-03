using Xunit;
using ChessLogic;
using ChessLogic.Utilities;
using ChessLogic.Moves; // Für TeleportMove, PositionSwapMove

namespace ChessLogic.Tests
{
    public class MoveExecutionAndLegalityTests
    {
        [Theory]
        [InlineData(PieceType.Pawn, true)] // Bauernzug
        [InlineData(PieceType.Knight, false)] // Springerzug (kein Bauernzug)
        public void NormalMoveExecuteReturnsCorrectBoolForPawnOrCapture(PieceType pieceType, bool expectedReturn)
        {
            // Arrange
            Board board = new Board();
            Position from = new Position(6, 0); // a2
            Position to = new Position(5, 0);   // a3
            Piece piece;
            if (pieceType == PieceType.Pawn) piece = new Pawn(Player.White);
            else piece = new Knight(Player.White); // Jede andere Figur außer Bauer
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

        [Fact]
        public void NormalMoveExecuteReturnsTrueOnCapture()
        {
            // Arrange
            Board board = new Board();
            Position from = new Position(3, 3); // d5
            Position to = new Position(3, 4);   // e5
            board[from] = new Rook(Player.White);
            board[to] = new Pawn(Player.Black); // Gegnerische Figur auf Zielfeld

            Move move = new NormalMove(from, to);

            // Act
            bool result = move.Execute(board);

            // Assert
            Assert.True(result); // Es war ein Schlagzug
        }

        [Fact]
        public void DoublePawnExecuteReturnsTrue()
        {
            // Arrange
            Board board = new Board();
            Position from = new Position(6, 0); // a2
            Position to = new Position(4, 0);   // a4
            board[from] = new Pawn(Player.White);
            Move move = new DoublePawn(from, to);

            // Act
            bool result = move.Execute(board);

            // Assert
            Assert.True(result); // Ist ein Bauernzug
            Assert.Equal(new Position(5, 0), board.GetPawnSkipPosition(Player.White));
        }

        [Fact]
        public void EnPassantExecuteReturnsTrue()
        {
            // Arrange
            Board board = new Board();
            Position whitePawnPos = new Position(3, 3); // d5 (schlagender Bauer)
            Position blackSkippedPos = new Position(2, 4); // e6 (wo der geschlagene Bauer übersprungen wurde)
            Position blackActualCapturePos = new Position(3, 4); // e5 (wo der geschlagene Bauer stand)

            board[whitePawnPos] = new Pawn(Player.White);
            board[blackActualCapturePos] = new Pawn(Player.Black); // Geschlagener Bauer
            board.SetPawnSkipPosition(Player.Black, blackSkippedPos); // En Passant Möglichkeit einrichten

            Move move = new EnPassant(whitePawnPos, blackSkippedPos);

            // Act
            bool result = move.Execute(board);

            // Assert
            Assert.True(result); // Ist ein Schlagzug (und Bauernzug)
            Assert.Null(board[blackActualCapturePos]); // Geschlagener Bauer ist weg
        }

        [Fact]
        public void CastleExecuteReturnsFalse()
        {
            // Arrange
            Board board = Board.Initial();
            // Felder für weiße kurze Rochade freimachen
            board[new Position(7, 5)] = null;
            board[new Position(7, 6)] = null;
            Move move = new Castle(MoveType.CastleKS, new Position(7, 4));

            // Act
            bool result = move.Execute(board);

            // Assert
            Assert.False(result); // Rochade ist weder Bauern- noch Schlagzug
            Assert.NotNull(board[new Position(7, 6)]); // König auf g1
            Assert.Equal(PieceType.King, board[new Position(7, 6)]?.Type);
            Assert.NotNull(board[new Position(7, 5)]); // Turm auf f1
            Assert.Equal(PieceType.Rook, board[new Position(7, 5)]?.Type);
        }

        [Fact]
        public void PawnPromotionExecuteReturnsTrue()
        {
            // Arrange
            Board board = new Board();
            Position from = new Position(1, 0); // a7
            Position to = new Position(0, 0);   // a8
            board[from] = new Pawn(Player.White);
            Move move = new PawnPromotion(from, to, PieceType.Queen);

            // Act
            bool result = move.Execute(board);

            // Assert
            Assert.True(result); // Ist ein Bauernzug
            Assert.NotNull(board[to]);
            Assert.Equal(PieceType.Queen, board[to]?.Type);
        }

        [Fact]
        public void TeleportMoveExecuteReturnsFalseAndMovesPiece()
        {
            // Arrange
            Board board = new Board();
            Position from = new Position(7, 0); // a1
            Position to = new Position(3, 3);   // d5
            Piece rook = new Rook(Player.White);
            board[from] = rook;
            Move move = new TeleportMove(from, to);

            // Act
            bool result = move.Execute(board);

            // Assert
            Assert.False(result); // Kein Bauern-/Schlagzug
            Assert.Null(board[from]);
            Assert.Same(rook, board[to]);
            Assert.True(rook.HasMoved);
        }

        [Fact]
        public void TeleportMoveIsLegalTargetMustBeEmpty()
        {
            // Arrange
            Board board = new Board();
            Position from = new Position(7, 0);
            Position to = new Position(3, 3);
            board[from] = new Rook(Player.White);
            board[to] = new Pawn(Player.Black); // Zielfeld ist besetzt

            Move move = new TeleportMove(from, to);

            // Act
            bool isLegal = move.IsLegal(board);

            // Assert
            Assert.False(isLegal);
        }

        [Fact]
        public void PositionSwapMoveExecuteReturnsFalseAndSwapsPieces()
        {
            // Arrange
            Board board = new Board();
            Position pos1 = new Position(7, 0); // a1
            Position pos2 = new Position(7, 1);   // b1
            Piece rook = new Rook(Player.White);
            Piece knight = new Knight(Player.White);
            board[pos1] = rook;
            board[pos2] = knight;
            Move move = new PositionSwapMove(pos1, pos2);

            // Act
            bool result = move.Execute(board);

            // Assert
            Assert.False(result); // Kein Bauern-/Schlagzug
            Assert.Same(knight, board[pos1]);
            Assert.Same(rook, board[pos2]);
            Assert.True(rook.HasMoved);
            Assert.True(knight.HasMoved);
        }

        [Fact]
        public void PositionSwapMoveIsLegalPiecesMustBeSameColor()
        {
            // Arrange
            Board board = new Board();
            Position pos1 = new Position(7, 0);
            Position pos2 = new Position(0, 0); // Gegnerische Figur
            board[pos1] = new Rook(Player.White);
            board[pos2] = new Rook(Player.Black);

            Move move = new PositionSwapMove(pos1, pos2);

            // Act
            bool isLegal = move.IsLegal(board);

            // Assert
            Assert.False(isLegal);
        }
        [Fact]
        public void PositionSwapMoveIsLegalPositionsMustBeDifferent()
        {
            // Arrange
            Board board = new Board();
            Position pos1 = new Position(7, 0);
            board[pos1] = new Rook(Player.White);
            // Wir brauchen eine zweite Figur für den Test der Bedingung im Handler.
            // Aber für die FromPos == ToPos Bedingung in PositionSwapMove.IsLegal() selbst
            // reicht es, wenn die zweite Figur theoretisch da wäre.
            // Die Kernlogik von PositionSwapMove.IsLegal() prüft FromPos == ToPos.

            Move move = new PositionSwapMove(pos1, pos1);

            // Act
            bool isLegal = move.IsLegal(board);

            // Assert
            // This specific check (FromPos == ToPos) is in the PositionSwapMove.IsLegal method.
            // However, for the swap to even be considered by a card activation handler,
            // two distinct pieces would typically be selected first.
            // The direct IsLegal check on the Move object itself will return false if FromPos == ToPos.
            Assert.False(isLegal);
        }
    }
}