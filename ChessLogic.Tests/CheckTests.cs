using ChessLogic.Utilities;
using System.Linq;
using Xunit;

namespace ChessLogic.Tests
{
    // Testklasse für die Funktionalität der Schach-, Remis- und Patt-Logik.
    public class CheckTests
    {
        // Testfall: Testet ein direktes Schachgebot durch einen Turm.
        [Fact]
        public void IsInCheckReturnsTrueWhenKingIsInDirectCheckByRook()
        {
            // Arrange: Platziert König und gegnerischen Turm in einer Schachstellung.
            Board board = new Board();
            board[new Position(0, 0)] = new King(Player.Black);
            board[new Position(7, 0)] = new Rook(Player.White);

            // Act: Prüft, ob Schwarz im Schach ist.
            bool isBlackInCheck = board.IsInCheck(Player.Black);

            // Assert: Das Ergebnis muss true sein.
            Assert.True(isBlackInCheck);
        }

        // Testfall: Verifiziert, dass in der Grundstellung kein Spieler im Schach steht.
        [Fact]
        public void IsInCheckReturnsFalseWhenKingIsNotInCheckInitially()
        {
            // Arrange: Verwendet die Standard-Brettaufstellung.
            Board board = Board.Initial();

            // Act: Prüft beide Spieler auf Schach.
            bool isWhiteInCheck = board.IsInCheck(Player.White);
            bool isBlackInCheck = board.IsInCheck(Player.Black);

            // Assert: Keiner der Spieler darf im Schach sein.
            Assert.False(isWhiteInCheck);
            Assert.False(isBlackInCheck);
        }

        // Testfall: Testet ein Schachgebot durch einen Bauern.
        [Fact]
        public void IsInCheckReturnsTrueWhenKingIsCheckedByPawn()
        {
            // Arrange: Platziert einen König und einen gegnerischen Bauern in einer Schachstellung.
            Board board = new Board();
            board[new Position(3, 3)] = new King(Player.Black);
            board[new Position(4, 4)] = new Pawn(Player.White);

            // Act & Assert
            Assert.True(board.IsInCheck(Player.Black));
        }

        // Testfall: Stellt sicher, dass ein Schachgebot durch eine dazwischenstehende Figur blockiert wird.
        [Fact]
        public void IsInCheckReturnsFalseWhenCheckIsBlockedByOwnPiece()
        {
            // Arrange: Eine eigene Figur blockiert die Angriffslinie auf den König.
            Board board = new Board();
            board[new Position(0, 4)] = new King(Player.Black);
            board[new Position(7, 4)] = new Rook(Player.White);
            board[new Position(1, 4)] = new Pawn(Player.Black); // Blockierender Bauer

            // Act & Assert
            Assert.False(board.IsInCheck(Player.Black));
        }

        // Testfall: Testet Remis durch unzureichendes Material für grundlegende Endspiele (K vs K).
        [Fact]
        public void InsufficientMaterialKingVsKingResultsInDraw()
        {
            // Arrange: Nur die beiden Könige sind auf dem Brett.
            Board board = new Board();
            board[new Position(0, 0)] = new King(Player.White);
            board[new Position(7, 7)] = new King(Player.Black);
            // Act & Assert
            Assert.True(board.InsufficientMaterial());
        }

        // Testfall: Testet Remis durch unzureichendes Material (König und Läufer gegen König).
        [Fact]
        public void InsufficientMaterialKingAndBishopVsKingResultsInDraw()
        {
            // Arrange
            Board board = new Board();
            board[new Position(0, 0)] = new King(Player.White);
            board[new Position(1, 1)] = new Bishop(Player.White);
            board[new Position(7, 7)] = new King(Player.Black);
            // Act & Assert
            Assert.True(board.InsufficientMaterial());
        }

        // Testfall: Testet Remis durch unzureichendes Material (König und Springer gegen König).
        [Fact]
        public void InsufficientMaterialKingAndKnightVsKingResultsInDraw()
        {
            // Arrange
            Board board = new Board();
            board[new Position(0, 0)] = new King(Player.White);
            board[new Position(1, 1)] = new Knight(Player.White);
            board[new Position(7, 7)] = new King(Player.Black);
            // Act & Assert
            Assert.True(board.InsufficientMaterial());
        }

        // Testfall: Testet Remis bei gleichfarbigen Läufern.
        [Fact]
        public void InsufficientMaterialKingAndBishopVsKingAndBishopSameColorResultsInDraw()
        {
            // Arrange: Beide Läufer stehen auf Feldern der gleichen Farbe.
            Board board = new Board();
            board[new Position(0, 0)] = new King(Player.White);
            board[new Position(1, 1)] = new Bishop(Player.White); // schwarzes Feld
            board[new Position(7, 7)] = new King(Player.Black);
            board[new Position(6, 6)] = new Bishop(Player.Black); // schwarzes Feld
            // Act & Assert
            Assert.True(board.InsufficientMaterial());
        }

        // Testfall: Prüft, dass ungleichfarbige Läufer nicht als unzureichendes Material gelten.
        [Fact]
        public void InsufficientMaterialKingAndBishopVsKingAndBishopDifferentColorIsNotInsufficient()
        {
            // Arrange
            Board board = new Board();
            board[new Position(0, 0)] = new King(Player.White);
            board[new Position(1, 1)] = new Bishop(Player.White); // schwarzes Feld
            board[new Position(7, 7)] = new King(Player.Black);
            board[new Position(6, 5)] = new Bishop(Player.Black); // weisses Feld
            // Act & Assert
            Assert.False(board.InsufficientMaterial());
        }

        // Testfall: Stellt sicher, dass ein Bauer eine Mattmöglichkeit darstellt und somit kein Remis ist.
        [Fact]
        public void NotInsufficientMaterialKingAndPawnVsKing()
        {
            // Arrange
            Board board = new Board();
            board[new Position(0, 0)] = new King(Player.White);
            board[new Position(1, 0)] = new Pawn(Player.White);
            board[new Position(7, 7)] = new King(Player.Black);
            // Act & Assert
            Assert.False(board.InsufficientMaterial());
        }
    }
}