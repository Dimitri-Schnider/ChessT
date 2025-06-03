using Xunit;     
using ChessLogic;

namespace ChessLogic.Tests 
{
    // Testklasse für die PlayerExtensions
    public class PlayerExtensionsTests
    {
        // Testfall: Prüft, ob der Gegner von Weiss Schwarz ist.
        [Fact] // Markiert diese Methode als einen einzelnen Testfall
        public void OpponentWhiteReturnsBlack()
        {
            // Arrange: Testdaten vorbereiten
            Player whitePlayer = Player.White;
            Player expectedOpponent = Player.Black;

            // Act: Die zu testende Methode ausführen
            Player actualOpponent = whitePlayer.Opponent();

            // Assert: Das Ergebnis überprüfen
            Assert.Equal(expectedOpponent, actualOpponent); // Vergleicht erwartetes mit tatsächlichem Ergebnis
        }

        // Testfall: Prüft, ob der Gegner von Schwarz Weiss ist.
        [Fact]
        public void OpponentBlackReturnsWhite()
        {
            // Arrange
            Player blackPlayer = Player.Black;
            Player expectedOpponent = Player.White;

            // Act
            Player actualOpponent = blackPlayer.Opponent();

            // Assert
            Assert.Equal(expectedOpponent, actualOpponent);
        }

        // Testfall: Prüft, ob der Gegner von None (kein Spieler) wieder None ist.
        [Fact]
        public void OpponentNoneReturnsNone()
        {
            // Arrange
            Player nonePlayer = Player.None;
            Player expectedOpponent = Player.None;

            // Act
            Player actualOpponent = nonePlayer.Opponent();

            // Assert
            Assert.Equal(expectedOpponent, actualOpponent);
        }

        // Parametrisierter Testfall: Führt denselben Test mit mehreren verschiedenen Eingabedaten aus.
        [Theory] // Markiert diese Methode als parametrisierten Testfall
        [InlineData(Player.White, Player.Black)] // Testlauf 1: Weiss -> Schwarz
        [InlineData(Player.Black, Player.White)] // Testlauf 2: Schwarz -> Weiss
        [InlineData(Player.None, Player.None)]   // Testlauf 3: None -> None
        public void OpponentReturnsCorrectOpponent(Player player, Player expectedOpponent)
        {
            // Act: Die zu testende Methode mit den Inline-Daten ausführen
            Player actualOpponent = player.Opponent();

            // Assert: Das Ergebnis überprüfen
            Assert.Equal(expectedOpponent, actualOpponent);
        }
    }
}