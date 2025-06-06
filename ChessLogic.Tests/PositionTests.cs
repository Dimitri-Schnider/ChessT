using ChessLogic.Utilities;
using Xunit;

namespace ChessLogic.Tests
{
    // Testklasse für die Position-Hilfsklasse.
    public class PositionTests
    {
        // Testfall: Überprüft, ob zwei Positionsobjekte mit denselben Koordinaten als gleich angesehen werden.
        [Fact]
        public void PositionsWithSameCoordinatesAreEqual()
        {
            // Arrange
            Position pos1 = new Position(3, 4);
            Position pos2 = new Position(3, 4);
            // Act & Assert
            Assert.True(pos1.Equals(pos2));
            Assert.True(pos1 == pos2);
            Assert.False(pos1 != pos2);
            Assert.Equal(pos1.GetHashCode(), pos2.GetHashCode());
        }

        // Testfall: Überprüft, ob zwei Positionsobjekte mit unterschiedlichen Koordinaten als ungleich angesehen werden.
        [Fact]
        public void PositionsWithDifferentCoordinatesAreNotEqual()
        {
            // Arrange
            Position pos1 = new Position(3, 4);
            Position pos2 = new Position(4, 3);
            // Act & Assert
            Assert.False(pos1.Equals(pos2));
            Assert.False(pos1 == pos2);
            Assert.True(pos1 != pos2);
            Assert.NotEqual(pos1.GetHashCode(), pos2.GetHashCode());
        }

        // Testfall: Stellt sicher, dass die Feldfarbe für verschiedene Positionen korrekt berechnet wird.
        [Theory]
        [InlineData(0, 0, Player.White)]
        [InlineData(0, 1, Player.Black)]
        [InlineData(7, 7, Player.White)]
        [InlineData(7, 0, Player.Black)]
        public void SquareColorReturnsCorrectPlayer(int row, int col, Player expectedColor)
        {
            // Arrange
            Position pos = new Position(row, col);
            // Act
            Player actualColor = pos.SquareColor();
            // Assert
            Assert.Equal(expectedColor, actualColor);
        }

        // Testfall: Überprüft die Addition einer Richtung zu einer Position.
        [Fact]
        public void AddingDirectionToPositionReturnsCorrectNewPosition()
        {
            // Arrange
            Position startPos = new Position(3, 3);
            Direction moveDir = Direction.NorthEast;
            Position expectedPos = new Position(2, 4);
            // Act
            Position actualPos = startPos + moveDir;
            // Assert
            Assert.Equal(expectedPos, actualPos);
        }
    }
}