using Xunit;

namespace ChessLogic.Tests
{
    // Testklasse für die Funktionalität der Counting-Hilfsklasse.
    public class CountingTests
    {
        // Testfall: Stellt sicher, dass die Zähler für alle Figurentypen initial auf Null stehen.
        [Fact]
        public void CountingInitializesAllCountsToZero()
        {
            // Arrange & Act
            Counting counting = new Counting();

            // Assert
            Assert.Equal(0, counting.TotalCount);
            foreach (PieceType type in Enum.GetValues<PieceType>())
            {
                Assert.Equal(0, counting.White(type));
                Assert.Equal(0, counting.Black(type));
            }
        }

        // Testfall: Prüft, ob die Zähler für weisse Figuren korrekt erhöht werden.
        [Fact]
        public void IncrementCorrectlyIncrementsWhitePieceCounts()
        {
            // Arrange
            Counting counting = new Counting();

            // Act
            counting.Increment(Player.White, PieceType.Pawn);
            counting.Increment(Player.White, PieceType.Pawn);
            counting.Increment(Player.White, PieceType.Rook);

            // Assert
            Assert.Equal(2, counting.White(PieceType.Pawn));
            Assert.Equal(1, counting.White(PieceType.Rook));
            Assert.Equal(3, counting.TotalCount);
        }

        // Testfall: Prüft, ob die Zähler für schwarze Figuren korrekt erhöht werden.
        [Fact]
        public void IncrementCorrectlyIncrementsBlackPieceCounts()
        {
            // Arrange
            Counting counting = new Counting();

            // Act
            counting.Increment(Player.Black, PieceType.Queen);
            counting.Increment(Player.Black, PieceType.Bishop);

            // Assert
            Assert.Equal(1, counting.Black(PieceType.Queen));
            Assert.Equal(1, counting.Black(PieceType.Bishop));
            Assert.Equal(2, counting.TotalCount);
        }

        // Testfall: Stellt sicher, dass das Inkrementieren für beide Farben und alle Figurentypen funktioniert.
        [Fact]
        public void IncrementHandlesBothColorsAndAllPieceTypes()
        {
            // Arrange
            Counting counting = new Counting();
            int expectedTotal = 0;

            // Act & Assert
            foreach (PieceType type in Enum.GetValues<PieceType>())
            {
                counting.Increment(Player.White, type);
                expectedTotal++;
                Assert.Equal(1, counting.White(type));
            }

            foreach (PieceType type in Enum.GetValues<PieceType>())
            {
                counting.Increment(Player.Black, type);
                expectedTotal++;
                Assert.Equal(1, counting.Black(type));
            }

            Assert.Equal(expectedTotal, counting.TotalCount);
        }
    }
}