using Xunit;

namespace ChessLogic.Tests
{
    // Testklasse für die Player-Erweiterungsmethoden.
    public class PlayerExtensionsTests
    {
        // Testfall: Prüft, ob der Gegner von Weiss korrekt als Schwarz identifiziert wird.
        [Fact]
        public void OpponentWhiteReturnsBlack()
        {
            // Arrange
            Player whitePlayer = Player.White;
            // Act
            Player actualOpponent = whitePlayer.Opponent();
            // Assert
            Assert.Equal(Player.Black, actualOpponent);
        }

        // Testfall: Prüft, ob der Gegner von Schwarz korrekt als Weiss identifiziert wird.
        [Fact]
        public void OpponentBlackReturnsWhite()
        {
            // Arrange
            Player blackPlayer = Player.Black;
            // Act
            Player actualOpponent = blackPlayer.Opponent();
            // Assert
            Assert.Equal(Player.White, actualOpponent);
        }

        // Testfall: Prüft, ob der "Gegner" von None (kein Spieler) korrekt None ist.
        [Fact]
        public void OpponentNoneReturnsNone()
        {
            // Arrange
            Player nonePlayer = Player.None;
            // Act
            Player actualOpponent = nonePlayer.Opponent();
            // Assert
            Assert.Equal(Player.None, actualOpponent);
        }

        // Ein einziger, parametrisierter Testfall, der alle möglichen Spieler-Gegner-Paare überprüft.
        [Theory]
        [InlineData(Player.White, Player.Black)] // Testfall 1: Der Gegner von Weiss ist Schwarz.
        [InlineData(Player.Black, Player.White)] // Testfall 2: Der Gegner von Schwarz ist Weiss.
        [InlineData(Player.None, Player.None)]   // Testfall 3: "None" hat keinen Gegner und sollte "None" zurückgeben.
        public void OpponentReturnsCorrectOpponent(Player player, Player expectedOpponent)
        {
            // Act: Ruft die Opponent-Erweiterungsmethode auf dem übergebenen Spieler auf.
            Player actualOpponent = player.Opponent();

            // Assert: Vergleicht das tatsächliche Ergebnis mit dem erwarteten Gegner.
            Assert.Equal(expectedOpponent, actualOpponent);
        }
    }
}