using ChessLogic.Utilities;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ChessLogic.Tests
{
    // Testklasse für die Funktionalität der Läufer-Figur.
    public class BishopTests
    {
        // Testfall: Überprüft die maximale Bewegungsreichweite eines Läufers auf einem leeren Brett von der Mitte aus.
        [Fact]
        public void BishopOnEmptyBoardFromCenterHasUpTo13Moves()
        {
            // Arrange: Initialisiert ein leeres Brett und platziert einen Läufer in der Mitte.
            Board board = new Board();
            Bishop bishop = new Bishop(Player.White);
            Position bishopPos = new Position(3, 3); // d5
            board[bishopPos] = bishop;

            // Act: Ruft die möglichen Züge für den Läufer ab.
            IEnumerable<Move> moves = bishop.GetMoves(bishopPos, board);

            // Assert: Stellt sicher, dass die Anzahl der Züge der erwarteten maximalen Anzahl entspricht.
            Assert.Equal(13, moves.Count());
        }

        // Testfall: Überprüft die Bewegungsreichweite eines Läufers von einer Ecke des Bretts.
        [Fact]
        public void BishopOnEmptyBoardFromA1Has7Moves()
        {
            // Arrange: Platziert einen Läufer auf dem Feld a1.
            Board board = new Board();
            Bishop bishop = new Bishop(Player.White);
            Position bishopPos = new Position(7, 0); // a1
            board[bishopPos] = bishop;

            // Act: Ruft die möglichen Züge ab.
            IEnumerable<Move> moves = bishop.GetMoves(bishopPos, board);

            // Assert: Stellt sicher, dass von der Ecke aus 7 Züge möglich sind.
            Assert.Equal(7, moves.Count());
        }

        // Testfall: Stellt sicher, dass ein Läufer durch Figuren der eigenen Farbe blockiert wird.
        [Fact]
        public void BishopIsBlockedByOwnPieces()
        {
            // Arrange: Platziert einen Läufer und blockierende Figuren der eigenen Farbe.
            Board board = new Board();
            Bishop whiteBishop = new Bishop(Player.White);
            Position bishopPos = new Position(3, 3);
            board[bishopPos] = whiteBishop;
            board[new Position(1, 1)] = new Pawn(Player.White);
            board[new Position(5, 5)] = new Pawn(Player.White);

            // Act: Ruft die möglichen Züge ab.
            IEnumerable<Move> moves = whiteBishop.GetMoves(bishopPos, board);

            // Assert: Die Anzahl der Züge ist durch die Blockaden reduziert.
            Assert.Equal(8, moves.Count());
            Assert.DoesNotContain(moves, m => m.ToPos.Equals(new Position(0, 0)));
            Assert.DoesNotContain(moves, m => m.ToPos.Equals(new Position(6, 6)));
        }

        // Testfall: Überprüft, ob ein Läufer eine gegnerische Figur schlagen kann und dahinter blockiert wird.
        [Fact]
        public void BishopCanCaptureOpponentAndIsBlocked()
        {
            // Arrange: Platziert einen Läufer, eine schlagbare gegnerische Figur und eine blockierende eigene Figur.
            Board board = new Board();
            Bishop whiteBishop = new Bishop(Player.White);
            Position bishopPos = new Position(3, 3);
            board[bishopPos] = whiteBishop;
            board[new Position(1, 1)] = new Pawn(Player.Black);
            board[new Position(5, 5)] = new Pawn(Player.White);

            // Act: Ruft die möglichen Züge ab.
            IEnumerable<Move> moves = whiteBishop.GetMoves(bishopPos, board);

            // Assert: Der Schlagzug ist enthalten, aber Züge hinter der geschlagenen Figur sind es nicht.
            Assert.Equal(9, moves.Count());
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(1, 1)));
            Assert.DoesNotContain(moves, m => m.ToPos.Equals(new Position(0, 0)));
            Assert.DoesNotContain(moves, m => m.ToPos.Equals(new Position(6, 6)));
        }

        // Testfall: Testet, ob die Methode zur Erkennung eines Königsangriffs korrekt funktioniert.
        [Fact]
        public void BishopCanCaptureOpponentKing()
        {
            // Arrange: Positioniert einen Läufer so, dass er den gegnerischen König bedroht.
            Board board = new Board();
            Bishop whiteBishop = new Bishop(Player.White);
            King blackKing = new King(Player.Black);
            Position bishopPos = new Position(2, 2);
            Position kingPos = new Position(5, 5);
            board[bishopPos] = whiteBishop;
            board[kingPos] = blackKing;

            // Act: Prüft, ob der Läufer den König schlagen kann.
            bool canCaptureKing = whiteBishop.CanCaptureOpponentKing(bishopPos, board);

            // Assert: Die Methode sollte true zurückgeben.
            Assert.True(canCaptureKing);
        }
    }
}