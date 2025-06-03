using Xunit;
using ChessLogic;

namespace ChessLogic.Tests
{
    public class CountingTests
    {
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
            Assert.Equal(0, counting.White(PieceType.Knight));
            Assert.Equal(0, counting.Black(PieceType.Pawn)); // Schwarz sollte unberührt sein
            Assert.Equal(3, counting.TotalCount);
        }

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
            Assert.Equal(0, counting.Black(PieceType.King));
            Assert.Equal(0, counting.White(PieceType.Queen)); // Weiß sollte unberührt sein
            Assert.Equal(2, counting.TotalCount);
        }

        [Fact]
        public void IncrementHandlesBothColorsAndAllPieceTypes()
        {
            // Arrange
            Counting counting = new Counting();
            int expectedTotal = 0;

            // Act & Assert for White
            foreach (PieceType type in Enum.GetValues<PieceType>())
            {
                counting.Increment(Player.White, type);
                expectedTotal++;
                Assert.Equal(1, counting.White(type));
                Assert.Equal(expectedTotal, counting.TotalCount);
            }

            // Act & Assert for Black
            foreach (PieceType type in Enum.GetValues<PieceType>())
            {
                counting.Increment(Player.Black, type);
                expectedTotal++;
                Assert.Equal(1, counting.Black(type));
                Assert.Equal(expectedTotal, counting.TotalCount);
            }

            // Double check totals and some specific counts
            Assert.Equal(1, counting.White(PieceType.King));
            Assert.Equal(1, counting.Black(PieceType.Pawn));
            Assert.Equal(Enum.GetValues<PieceType>().Length * 2, counting.TotalCount);
        }
    }
}