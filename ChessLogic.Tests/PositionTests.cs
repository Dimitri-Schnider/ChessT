using Xunit;
using ChessLogic;
using ChessLogic.Utilities;

namespace ChessLogic.Tests
{
    // Testklasse für die Position-Funktionalität
    public class PositionTests
    {
        // Testfall: Überprüft die Gleichheit von zwei identischen Positionen
        [Fact]
        public void PositionsWithSameCoordinatesAreEqual()
        {
            // Arrange
            Position pos1 = new Position(3, 4); // Erzeuge Position e5 (0-indiziert: Zeile 3, Spalte 4)
            Position pos2 = new Position(3, 4);

            // Act & Assert
            Assert.True(pos1.Equals(pos2));         // Prüft Equals-Methode
            Assert.True(pos1 == pos2);              // Prüft Operator ==
            Assert.False(pos1 != pos2);             // Prüft Operator !=
            Assert.Equal(pos1.GetHashCode(), pos2.GetHashCode()); // HashCodes sollten auch gleich sein
        }

        // Testfall: Überprüft die Ungleichheit von zwei unterschiedlichen Positionen
        [Fact]
        public void PositionsWithDifferentCoordinatesAreNotEqual()
        {
            // Arrange
            Position pos1 = new Position(3, 4); // e5
            Position pos2 = new Position(4, 3); // d4

            // Act & Assert
            Assert.False(pos1.Equals(pos2));
            Assert.False(pos1 == pos2);
            Assert.True(pos1 != pos2);
            Assert.NotEqual(pos1.GetHashCode(), pos2.GetHashCode());
        }

        // Testfälle: Überprüft die Feldfarbe für verschiedene Positionen
        [Theory]
        [InlineData(0, 0, Player.White)] // a8 ist ein weißes Feld (wenn man von Weiß unten ausgeht, ist es ein schwarzes Feld, aber die Logik (Row+Col)%2 ist oft andersrum oder bezogen auf A1 als Weiß)
                                         // Laut Implementierung: (0+0)%2 == 0 -> Player.White. Das ist die implementierte Logik.
        [InlineData(0, 1, Player.Black)] // b8 ist ein schwarzes Feld
        [InlineData(7, 7, Player.White)] // h1 ist ein weißes Feld
        [InlineData(7, 0, Player.Black)] // a1 ist ein schwarzes Feld
        public void SquareColorReturnsCorrectPlayer(int row, int col, Player expectedColor)
        {
            // Arrange
            Position pos = new Position(row, col);

            // Act
            Player actualColor = pos.SquareColor();

            // Assert
            Assert.Equal(expectedColor, actualColor);
        }

        // Testfall: Überprüft die Addition einer Richtung zu einer Position
        [Fact]
        public void AddingDirectionToPositionReturnsCorrectNewPosition()
        {
            // Arrange
            Position startPos = new Position(3, 3); // d5
            Direction moveDir = Direction.NorthEast; // Ein Feld nach oben (-1 Zeile) und ein Feld nach rechts (+1 Spalte)
                                                     // North = (-1, 0), East = (0, 1) => NorthEast = (-1, 1)
            Position expectedPos = new Position(2, 4); // e6 (Zeile 3-1=2, Spalte 3+1=4)

            // Act
            Position actualPos = startPos + moveDir;

            // Assert
            Assert.Equal(expectedPos, actualPos);
        }
    }
}