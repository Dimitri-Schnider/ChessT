using Xunit;
using ChessLogic.Utilities;

namespace ChessLogic.Tests
{
    // Testklasse für die Direction-Funktionalität
    public class DirectionTests
    {
        // Testfall: Addition von zwei Richtungen
        [Fact]
        public void AddingTwoDirectionsReturnsCorrectCombinedDirection()
        {
            // Arrange
            Direction dir1 = Direction.North; // (-1, 0)
            Direction dir2 = Direction.East;  // (0, 1)
            Direction expectedCombinedDir = Direction.NorthEast; // (-1, 1)

            // Act
            Direction actualCombinedDir = dir1 + dir2;

            // Assert
            Assert.Equal(expectedCombinedDir.RowDelta, actualCombinedDir.RowDelta);
            Assert.Equal(expectedCombinedDir.ColumnDelta, actualCombinedDir.ColumnDelta);
        }

        // Testfall: Multiplikation einer Richtung mit einem Skalar
        [Fact]
        public void MultiplyingDirectionByScalarReturnsCorrectScaledDirection()
        {
            // Arrange
            Direction dir = Direction.South; // (1, 0)
            int scalar = 2;
            Direction expectedScaledDir = new Direction(2, 0); // Zwei Schritte nach Süden

            // Act
            Direction actualScaledDir = scalar * dir;

            // Assert
            Assert.Equal(expectedScaledDir.RowDelta, actualScaledDir.RowDelta);
            Assert.Equal(expectedScaledDir.ColumnDelta, actualScaledDir.ColumnDelta);
        }

        // Testfall: Gleichheit vordefinierter Richtungen (hier für NorthEast als Beispiel)
        [Fact]
        public void PredefinedDiagonalDirectionsAreCorrectlyComposed()
        {
            // Arrange
            Direction expectedNorthEast = new Direction(-1, 1);

            // Act
            Direction actualNorthEast = Direction.NorthEast;

            // Assert
            Assert.Equal(expectedNorthEast.RowDelta, actualNorthEast.RowDelta);
            Assert.Equal(expectedNorthEast.ColumnDelta, actualNorthEast.ColumnDelta);
        }
    }
}