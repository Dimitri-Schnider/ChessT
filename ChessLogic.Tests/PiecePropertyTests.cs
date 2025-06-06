using Xunit;

namespace ChessLogic.Tests
{
    // Testklasse für grundlegende Eigenschaften von Schachfiguren.
    public class PiecePropertyTests
    {
        // Testfall: Überprüft die korrekten Standardeigenschaften eines neu erstellten Bauern.
        [Theory]
        [InlineData(Player.White)]
        [InlineData(Player.Black)]
        public void PawnPropertiesAreCorrect(Player color)
        {
            // Arrange & Act
            Pawn pawn = new Pawn(color);
            // Assert
            Assert.Equal(PieceType.Pawn, pawn.Type);
            Assert.Equal(color, pawn.Color);
            Assert.False(pawn.HasMoved);
        }

        // Testfall: Überprüft die korrekten Standardeigenschaften eines neu erstellten Turms.
        [Theory]
        [InlineData(Player.White)]
        [InlineData(Player.Black)]
        public void RookPropertiesAreCorrect(Player color)
        {
            // Arrange & Act
            Rook rook = new Rook(color);
            // Assert
            Assert.Equal(PieceType.Rook, rook.Type);
            Assert.Equal(color, rook.Color);
            Assert.False(rook.HasMoved);
        }

        // Testfall: Überprüft die korrekten Standardeigenschaften eines neu erstellten Springers.
        [Theory]
        [InlineData(Player.White)]
        [InlineData(Player.Black)]
        public void KnightPropertiesAreCorrect(Player color)
        {
            // Arrange & Act
            Knight knight = new Knight(color);
            // Assert
            Assert.Equal(PieceType.Knight, knight.Type);
            Assert.Equal(color, knight.Color);
            Assert.False(knight.HasMoved);
        }

        // Testfall: Überprüft die korrekten Standardeigenschaften eines neu erstellten Läufers.
        [Theory]
        [InlineData(Player.White)]
        [InlineData(Player.Black)]
        public void BishopPropertiesAreCorrect(Player color)
        {
            // Arrange & Act
            Bishop bishop = new Bishop(color);
            // Assert
            Assert.Equal(PieceType.Bishop, bishop.Type);
            Assert.Equal(color, bishop.Color);
            Assert.False(bishop.HasMoved);
        }

        // Testfall: Überprüft die korrekten Standardeigenschaften einer neu erstellten Dame.
        [Theory]
        [InlineData(Player.White)]
        [InlineData(Player.Black)]
        public void QueenPropertiesAreCorrect(Player color)
        {
            // Arrange & Act
            Queen queen = new Queen(color);
            // Assert
            Assert.Equal(PieceType.Queen, queen.Type);
            Assert.Equal(color, queen.Color);
            Assert.False(queen.HasMoved);
        }

        // Testfall: Überprüft die korrekten Standardeigenschaften eines neu erstellten Königs.
        [Theory]
        [InlineData(Player.White)]
        [InlineData(Player.Black)]
        public void KingPropertiesAreCorrect(Player color)
        {
            // Arrange & Act
            King king = new King(color);
            // Assert
            Assert.Equal(PieceType.King, king.Type);
            Assert.Equal(color, king.Color);
            Assert.False(king.HasMoved);
        }

        // Testfall: Stellt sicher, dass das Kopieren einer Figur alle Eigenschaften korrekt übernimmt.
        [Fact]
        public void CopiedPieceRetainsProperties()
        {
            // Arrange
            Pawn originalPawn = new Pawn(Player.White) { HasMoved = true };
            // Act
            Piece copiedPiece = originalPawn.Copy();
            // Assert
            Assert.Equal(originalPawn.Type, copiedPiece.Type);
            Assert.Equal(originalPawn.Color, copiedPiece.Color);
            Assert.Equal(originalPawn.HasMoved, copiedPiece.HasMoved);
            Assert.NotSame(originalPawn, copiedPiece);
        }
    }
}