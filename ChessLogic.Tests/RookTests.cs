using Xunit;
using ChessLogic;
using ChessLogic.Utilities;
using System.Linq;
using System.Collections.Generic;

namespace ChessLogic.Tests
{
    // Testklasse für die spezifische Zuglogik des Turms
    public class RookTests
    {
        // Testfall: Turm auf leerem Brett hat 14 mögliche Züge von der Mitte aus
        [Fact]
        public void RookOnEmptyBoardFromCenterHas14Moves()
        {
            // Arrange
            Board board = new Board();
            Rook rook = new Rook(Player.White);
            Position rookPos = new Position(3, 3); // d5
            board[rookPos] = rook;

            // Act
            IEnumerable<Move> moves = rook.GetMoves(rookPos, board);

            // Assert
            Assert.Equal(14, moves.Count()); // 7 horizontal + 7 vertikal
        }

        // Testfall: Turm wird durch eigene Figuren blockiert
        [Fact]
        public void RookIsBlockedByOwnPieces()
        {
            // Arrange
            Board board = new Board();
            Rook whiteRook = new Rook(Player.White);
            Position rookPos = new Position(3, 3); // d5
            board[rookPos] = whiteRook;

            board[new Position(3, 5)] = new Pawn(Player.White); // Eigener Bauer auf f5 (blockiert rechts)
            board[new Position(1, 3)] = new Pawn(Player.White); // Eigener Bauer auf d7 (blockiert oben)

            // Act
            IEnumerable<Move> moves = whiteRook.GetMoves(rookPos, board);

            // Assert
            // Erwartete Züge: d1,d2,d3,d4 (4 vertikal unten) + a5,b5,c5 (3 horizontal links) + d6 (1 vertikal oben bis zum Blocker) + e5 (1 horizontal rechts bis zum Blocker)
            // Total = 4 + 3 + 1 + 1 = 9
            Assert.Equal(9, moves.Count());
            Assert.DoesNotContain(moves, m => m.ToPos.Equals(new Position(3, 6))); // f6 (hinter Blocker) ist nicht erreichbar
            Assert.DoesNotContain(moves, m => m.ToPos.Equals(new Position(0, 3))); // d8 (hinter Blocker) ist nicht erreichbar
        }

        // Testfall: Turm kann gegnerische Figuren schlagen, aber nicht dahinter ziehen
        [Fact]
        public void RookCanCaptureOpponentAndIsBlocked()
        {
            // Arrange
            Board board = new Board();
            Rook whiteRook = new Rook(Player.White);
            Position rookPos = new Position(7, 0); // a1
            board[rookPos] = whiteRook;

            board[new Position(5, 0)] = new Pawn(Player.Black); // Gegnerischer Bauer auf a3 (kann geschlagen werden)
            board[new Position(7, 2)] = new Pawn(Player.Black); // Gegnerischer Bauer auf c1 (kann geschlagen werden)

            // Act
            IEnumerable<Move> moves = whiteRook.GetMoves(rookPos, board);

            // Assert
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(5, 0))); // Schlag auf a3
            Assert.Contains(moves, m => m.ToPos.Equals(new Position(7, 2))); // Schlag auf c1
            // Turm kann nicht hinter die geschlagene Figur ziehen
            Assert.DoesNotContain(moves, m => m.ToPos.Equals(new Position(4, 0))); // a4 (hinter a3)
            Assert.DoesNotContain(moves, m => m.ToPos.Equals(new Position(7, 3))); // d1 (hinter c1)

            // Züge: a2 (0) + b1 (1) + Schlag auf a3 (1) + Schlag auf c1 (1) = 3 normale Züge
            // Plus alle Felder bis zum Schlag
            // Vertikal: (6,0)
            // Horizontal: (7,1)
            // Anzahl Züge: (7,0)->(6,0) und (7,0)->(5,0) == 2 Züge nach oben
            // (7,0)->(7,1) und (7,0)->(7,2) == 2 Züge nach rechts
            Assert.Equal(4, moves.Count());
        }
    }
}