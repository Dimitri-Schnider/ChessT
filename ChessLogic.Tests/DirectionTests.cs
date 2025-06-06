using ChessLogic.Utilities;
using Xunit;

namespace ChessLogic.Tests
{
    // Testklasse für die Funktionalität der Direction-Hilfsklasse.
    public class DirectionTests
    {
        // Testfall: Testet, ob die Addition von zwei Richtungen das korrekte, kombinierte Ergebnis liefert.
        [Fact]
        public void AddingTwoDirectionsReturnsCorrectCombinedDirection()
        {
            // Arrange
            Direction dir1 = Direction.North;
            Direction dir2 = Direction.East;
            Direction expectedCombinedDir = new Direction(-1, 1);

            // Act
            Direction actualCombinedDir = dir1 + dir2;

            // Assert
            Assert.Equal(expectedCombinedDir, actualCombinedDir);
        }

        // Testfall: Prüft, ob die Multiplikation einer Richtung mit einem Skalar die Bewegung korrekt skaliert.
        [Fact]
        public void MultiplyingDirectionByScalarReturnsCorrectScaledDirection()
        {
            // Arrange
            Direction dir = Direction.South;
            int scalar = 2;
            Direction expectedScaledDir = new Direction(2, 0);

            // Act
            Direction actualScaledDir = scalar * dir;

            // Assert
            Assert.Equal(expectedScaledDir, actualScaledDir);
        }

        // Testfall: Stellt sicher, dass vordefinierte diagonale Richtungen korrekt aus Hauptrichtungen zusammengesetzt sind.
        [Fact]
        public void PredefinedDiagonalDirectionsAreCorrectlyComposed()
        {
            // Arrange
            Direction expectedNorthEast = new Direction(-1, 1);
            // Act
            Direction actualNorthEast = Direction.NorthEast;
            // Assert
            Assert.Equal(expectedNorthEast, actualNorthEast);
        }
    }
}