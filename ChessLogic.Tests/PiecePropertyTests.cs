using Xunit;
using ChessLogic; 

namespace ChessLogic.Tests
{
    // Testklasse für grundlegende Eigenschaften von Schachfiguren
    public class PiecePropertyTests
    {
        // Testfälle für den Bauern (Pawn)
        [Theory]
        [InlineData(Player.White)] // Testlauf 1: Weißer Bauer
        [InlineData(Player.Black)] // Testlauf 2: Schwarzer Bauer
        public void PawnPropertiesAreCorrect(Player color)
        {
            // Arrange & Act
            Pawn pawn = new Pawn(color);

            // Assert
            Assert.Equal(PieceType.Pawn, pawn.Type);       // Typ muss Bauer sein
            Assert.Equal(color, pawn.Color);              // Farbe muss der übergebenen Farbe entsprechen
            Assert.False(pawn.HasMoved);                  // Ein neuer Bauer darf noch nicht gezogen sein
        }

        // Testfälle für den Turm (Rook)
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

        // Testfälle für den Springer (Knight)
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

        // Testfälle für den Läufer (Bishop)
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

        // Testfälle für die Dame (Queen)
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

        // Testfälle für den König (King)
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

        // Testfall: Kopieren einer Figur erhält Eigenschaften
        [Fact]
        public void CopiedPieceRetainsProperties()
        {
            // Arrange
            Pawn originalPawn = new Pawn(Player.White);
            originalPawn.HasMoved = true; // Zustand ändern

            // Act
            Piece copiedPiece = originalPawn.Copy(); // Kopie erstellen

            // Assert
            Assert.Equal(originalPawn.Type, copiedPiece.Type);
            Assert.Equal(originalPawn.Color, copiedPiece.Color);
            Assert.Equal(originalPawn.HasMoved, copiedPiece.HasMoved); // Auch HasMoved muss kopiert werden
            Assert.NotSame(originalPawn, copiedPiece); // Es muss eine neue Instanz sein, nicht dieselbe
        }
    }
}