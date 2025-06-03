using Xunit;
using ChessLogic;
using ChessLogic.Utilities;
using System.Linq;
using System.Collections.Generic;

namespace ChessLogic.Tests
{
    // Testklasse für die Funktionalität der Schach / Remis / Patt Logik
    public class CheckTests
    {
        // Testfall: Einfaches Schach durch einen weißen Turm auf a1 gegen schwarzen König auf a8 (leeres Brett sonst)
        [Fact]
        public void IsInCheckReturnsTrueWhenKingIsInDirectCheckByRook()
        {
            // Arrange
            Board board = new Board();
            King blackKing = new King(Player.Black);
            Rook whiteRook = new Rook(Player.White);
            board[new Position(0, 0)] = blackKing; // a8
            board[new Position(7, 0)] = whiteRook; // a1

            // Act
            bool isBlackInCheck = board.IsInCheck(Player.Black);

            // Assert
            Assert.True(isBlackInCheck);
        }

        // Testfall: König ist nicht im Schach in der Grundstellung
        [Fact]
        public void IsInCheckReturnsFalseWhenKingIsNotInCheckInitially()
        {
            // Arrange
            Board board = Board.Initial();

            // Act
            bool isWhiteInCheck = board.IsInCheck(Player.White);
            bool isBlackInCheck = board.IsInCheck(Player.Black);

            // Assert
            Assert.False(isWhiteInCheck);
            Assert.False(isBlackInCheck);
        }

        // Testfall: Schach durch einen Bauern
        [Fact]
        public void IsInCheckReturnsTrueWhenKingIsCheckedByPawn()
        {
            // Arrange
            Board board = new Board();
            King blackKing = new King(Player.Black);
            Pawn whitePawn = new Pawn(Player.White);
            board[new Position(3, 3)] = blackKing; // d5
            board[new Position(4, 4)] = whitePawn; // e4 (Bauer bedroht d5)

            // Act
            bool isBlackInCheck = board.IsInCheck(Player.Black);

            // Assert
            Assert.True(isBlackInCheck);
        }

        // Testfall: Schach wird durch eigene Figur blockiert
        [Fact]
        public void IsInCheckReturnsFalseWhenCheckIsBlockedByOwnPiece()
        {
            // Arrange
            Board board = new Board();
            King blackKing = new King(Player.Black);
            Rook whiteRook = new Rook(Player.White);
            Pawn blackPawn = new Pawn(Player.Black); // Blockierender Bauer

            board[new Position(0, 4)] = blackKing; // e8
            board[new Position(7, 4)] = whiteRook; // e1
            board[new Position(1, 4)] = blackPawn; // e7 (blockiert die Linie)

            // Act
            bool isBlackInCheck = board.IsInCheck(Player.Black);

            // Assert
            Assert.False(isBlackInCheck);
        }

        // Testfall: König gegen König ist Remis durch unzureichendes Material
        [Fact]
        public void InsufficientMaterialKingVsKingResultsInDraw()
        {
            // Arrange
            Board board = new Board();
            board[new Position(0, 0)] = new King(Player.White);
            board[new Position(7, 7)] = new King(Player.Black);

            // Act
            bool isInsufficient = board.InsufficientMaterial();

            // Assert
            Assert.True(isInsufficient);
        }

        // Testfall: König und Läufer gegen König ist Remis
        [Fact]
        public void InsufficientMaterialKingAndBishopVsKingResultsInDraw()
        {
            // Arrange
            Board board = new Board();
            board[new Position(0, 0)] = new King(Player.White);
            board[new Position(1, 1)] = new Bishop(Player.White);
            board[new Position(7, 7)] = new King(Player.Black);

            // Act
            bool isInsufficient = board.InsufficientMaterial();

            // Assert
            Assert.True(isInsufficient);
        }

        // Testfall: König und Springer gegen König ist Remis
        [Fact]
        public void InsufficientMaterialKingAndKnightVsKingResultsInDraw()
        {
            // Arrange
            Board board = new Board();
            board[new Position(0, 0)] = new King(Player.White);
            board[new Position(1, 1)] = new Knight(Player.White);
            board[new Position(7, 7)] = new King(Player.Black);

            // Act
            bool isInsufficient = board.InsufficientMaterial();

            // Assert
            Assert.True(isInsufficient);
        }

        // Testfall: König und Läufer gegen König und Läufer (gleichfarbige Läufer) ist Remis
        [Fact]
        public void InsufficientMaterialKingAndBishopVsKingAndBishopSameColorResultsInDraw()
        {
            // Arrange
            Board board = new Board();
            board[new Position(0, 0)] = new King(Player.White);
            board[new Position(1, 1)] = new Bishop(Player.White); // Läufer auf schwarzem Feld (b7)
            board[new Position(7, 7)] = new King(Player.Black);
            board[new Position(6, 6)] = new Bishop(Player.Black); // Läufer auf schwarzem Feld (g2)

            // Act
            bool isInsufficient = board.InsufficientMaterial();

            // Assert
            Assert.True(isInsufficient);
        }

        // Testfall: König und Läufer gegen König und Läufer (ungleichfarbige Läufer) ist KEIN Remis durch unzureichendes Material
        [Fact]
        public void InsufficientMaterialKingAndBishopVsKingAndBishopDifferentColorIsNotInsufficient()
        {
            // Arrange
            Board board = new Board();
            board[new Position(0, 0)] = new King(Player.White);
            board[new Position(1, 1)] = new Bishop(Player.White); // Läufer auf schwarzem Feld (b7)
            board[new Position(7, 7)] = new King(Player.Black);
            board[new Position(6, 5)] = new Bishop(Player.Black); // Läufer auf weißem Feld (f2)

            // Act
            bool isInsufficient = board.InsufficientMaterial();

            // Assert
            Assert.False(isInsufficient); // Kann noch komplex sein, aber nicht per se unzureichend
        }

        // Testfall: König und Bauer gegen König ist KEIN Remis durch unzureichendes Material
        [Fact]
        public void NotInsufficientMaterialKingAndPawnVsKing()
        {
            // Arrange
            Board board = new Board();
            board[new Position(0, 0)] = new King(Player.White);
            board[new Position(1, 0)] = new Pawn(Player.White);
            board[new Position(7, 7)] = new King(Player.Black);

            // Act
            bool isInsufficient = board.InsufficientMaterial();

            // Assert
            Assert.False(isInsufficient);
        }
    }
}