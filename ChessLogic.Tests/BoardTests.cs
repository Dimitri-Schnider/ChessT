using Xunit;
using ChessLogic;
using ChessLogic.Utilities;
using System.Linq;      
using System.Collections.Generic; 

namespace ChessLogic.Tests
{
    // Testklasse für die Funktionalität des Schachbretts (Board)
    public class BoardTests
    {
        // Testfall: Überprüft, ob Positionen korrekt als innerhalb oder außerhalb des Bretts erkannt werden
        [Theory]
        [InlineData(0, 0, true)]    // a8 ist innerhalb
        [InlineData(7, 7, true)]    // h1 ist innerhalb
        [InlineData(3, 4, true)]    // e5 ist innerhalb
        [InlineData(-1, 0, false)]   // Ungültige Zeile (zu klein)
        [InlineData(0, -1, false)]   // Ungültige Spalte (zu klein)
        [InlineData(8, 7, false)]    // Ungültige Zeile (zu groß)
        [InlineData(7, 8, false)]    // Ungültige Spalte (zu groß)
        public void IsInsideCorrectlyIdentifiesPositions(int row, int col, bool expectedIsInside)
        {
            // Arrange
            Position pos = new Position(row, col);

            // Act
            bool actualIsInside = Board.IsInside(pos);

            // Assert
            Assert.Equal(expectedIsInside, actualIsInside);
        }

        // Testfall: Überprüft, ob Felder auf einem neu initialisierten Brett korrekt als leer oder besetzt erkannt werden
        [Fact]
        public void IsEmptyCorrectlyIdentifiesSquaresOnInitialBoard()
        {
            // Arrange
            Board board = Board.Initial(); // Erstellt ein Brett mit der Standardaufstellung

            // Act & Assert
            // Überprüfe einige besetzte Felder
            Assert.False(board.IsEmpty(new Position(0, 0))); // a8, schwarzer Turm
            Assert.False(board.IsEmpty(new Position(1, 3))); // d7, schwarzer Bauer
            Assert.False(board.IsEmpty(new Position(6, 4))); // e2, weißer Bauer
            Assert.False(board.IsEmpty(new Position(7, 7))); // h1, weißer Turm

            // Überprüfe einige leere Felder
            Assert.True(board.IsEmpty(new Position(3, 3))); // d5, sollte leer sein
            Assert.True(board.IsEmpty(new Position(4, 4))); // e4, sollte leer sein
        }

        // Testfall: Überprüft die korrekte Initialaufstellung einiger Schlüssel-Figuren
        [Fact]
        public void InitialBoardSetupHasCorrectKeyPieces()
        {
            // Arrange
            Board board = Board.Initial();

            // Act & Assert
            // Teste weiße Figuren
            Assert.Equal(PieceType.Rook, board[new Position(7, 0)]?.Type); // a1 Turm
            Assert.Equal(Player.White, board[new Position(7, 0)]?.Color);
            Assert.Equal(PieceType.Knight, board[new Position(7, 1)]?.Type); // b1 Springer
            Assert.Equal(Player.White, board[new Position(7, 1)]?.Color);
            Assert.Equal(PieceType.King, board[new Position(7, 4)]?.Type); // e1 König
            Assert.Equal(Player.White, board[new Position(7, 4)]?.Color);
            Assert.All(Enumerable.Range(0, 8), col => Assert.Equal(PieceType.Pawn, board[new Position(6, col)]?.Type)); // Reihe 2 Bauern
            Assert.All(Enumerable.Range(0, 8), col => Assert.Equal(Player.White, board[new Position(6, col)]?.Color));

            // Teste schwarze Figuren
            Assert.Equal(PieceType.Rook, board[new Position(0, 7)]?.Type); // h8 Turm
            Assert.Equal(Player.Black, board[new Position(0, 7)]?.Color);
            Assert.Equal(PieceType.Queen, board[new Position(0, 3)]?.Type); // d8 Dame
            Assert.Equal(Player.Black, board[new Position(0, 3)]?.Color);
            Assert.All(Enumerable.Range(0, 8), col => Assert.Equal(PieceType.Pawn, board[new Position(1, col)]?.Type)); // Reihe 7 Bauern
            Assert.All(Enumerable.Range(0, 8), col => Assert.Equal(Player.Black, board[new Position(1, col)]?.Color));
        }

        // Testfall: Überprüft die GetPawnSkipPosition und SetPawnSkipPosition Methoden
        [Fact]
        public void GetAndSetPawnSkipPositionWorksCorrectly()
        {
            // Arrange
            Board board = new Board(); // Leeres Brett für diesen Test
            Position whiteSkipPos = new Position(2, 3);
            Position blackSkipPos = new Position(5, 4);

            // Act & Assert für Weiß
            Assert.Null(board.GetPawnSkipPosition(Player.White));
            board.SetPawnSkipPosition(Player.White, whiteSkipPos);
            Assert.Equal(whiteSkipPos, board.GetPawnSkipPosition(Player.White));
            board.SetPawnSkipPosition(Player.White, null);
            Assert.Null(board.GetPawnSkipPosition(Player.White));

            // Act & Assert für Schwarz
            Assert.Null(board.GetPawnSkipPosition(Player.Black));
            board.SetPawnSkipPosition(Player.Black, blackSkipPos);
            Assert.Equal(blackSkipPos, board.GetPawnSkipPosition(Player.Black));
            board.SetPawnSkipPosition(Player.Black, null);
            Assert.Null(board.GetPawnSkipPosition(Player.Black));
        }

        // Testfall: Stellt sicher, dass Board.Copy() eine tiefe Kopie erstellt
        [Fact]
        public void CopyCreatesDeepCloneOfBoard()
        {
            // Arrange
            Board originalBoard = Board.Initial();
            Position pawnPos = new Position(1, 0); // Schwarzer Bauer auf a7
            Position skipPos = new Position(2, 0); // Mögliche Sprungposition

            originalBoard.SetPawnSkipPosition(Player.Black, skipPos); // Setze einen Zustand, der kopiert werden muss
            Piece? originalPawn = originalBoard[pawnPos];
            if (originalPawn != null) originalPawn.HasMoved = true;


            // Act
            Board copiedBoard = originalBoard.Copy();

            // Assert
            // 1. Überprüfen, ob die Figuren an derselben Stelle denselben Typ und Farbe haben
            Assert.Equal(originalBoard[pawnPos]?.Type, copiedBoard[pawnPos]?.Type);
            Assert.Equal(originalBoard[pawnPos]?.Color, copiedBoard[pawnPos]?.Color);
            Assert.Equal(originalBoard[pawnPos]?.HasMoved, copiedBoard[pawnPos]?.HasMoved);

            // 2. Überprüfen, ob es sich um unterschiedliche Instanzen handelt (für Figuren)
            Assert.NotSame(originalBoard[pawnPos], copiedBoard[pawnPos]); // Wichtig für tiefe Kopie

            // 3. PawnSkipPosition muss ebenfalls kopiert werden
            Assert.Equal(originalBoard.GetPawnSkipPosition(Player.Black), copiedBoard.GetPawnSkipPosition(Player.Black));


            // 4. Änderung an der Kopie darf Original nicht beeinflussen
            copiedBoard[pawnPos] = null; // Figur auf Kopie entfernen
            copiedBoard.SetPawnSkipPosition(Player.Black, null);

            Assert.NotNull(originalBoard[pawnPos]); // Figur muss im Original noch da sein
            Assert.Equal(skipPos, originalBoard.GetPawnSkipPosition(Player.Black)); // SkipPos im Original unverändert
        }

        // Testfall: Zählt alle Figurenpositionen auf einem initialen Brett
        [Fact]
        public void PiecePositionsReturnsAll32PiecePositionsOnInitialBoard()
        {
            // Arrange
            Board board = Board.Initial();

            // Act
            List<Position> piecePositions = board.PiecePositions().ToList();

            // Assert
            Assert.Equal(32, piecePositions.Count); // Es sollten 32 Figuren sein
        }

        // Testfall: Zählt Figurenpositionen für einen bestimmten Spieler auf einem initialen Brett
        [Theory]
        [InlineData(Player.White, 16)] // Weiß hat 16 Figuren
        [InlineData(Player.Black, 16)] // Schwarz hat 16 Figuren
        public void PiecePositionsForPlayerReturnsCorrectNumberOfPositions(Player player, int expectedCount)
        {
            // Arrange
            Board board = Board.Initial();

            // Act
            List<Position> playerPiecePositions = board.PiecePositionsFor(player).ToList();

            // Assert
            Assert.Equal(expectedCount, playerPiecePositions.Count);
            Assert.All(playerPiecePositions, pos => Assert.Equal(player, board[pos]?.Color)); // Alle gefundenen Figuren müssen die korrekte Farbe haben
        }

        // Testfall: Überprüft die Figurenzählung mit Board.CountPieces()
        [Fact]
        public void CountPiecesReturnsCorrectCountsForInitialBoard()
        {
            // Arrange
            Board board = Board.Initial();

            // Act
            Counting counts = board.CountPieces();

            // Assert
            Assert.Equal(8, counts.White(PieceType.Pawn));
            Assert.Equal(2, counts.White(PieceType.Rook));
            Assert.Equal(2, counts.White(PieceType.Knight));
            Assert.Equal(2, counts.White(PieceType.Bishop));
            Assert.Equal(1, counts.White(PieceType.Queen));
            Assert.Equal(1, counts.White(PieceType.King));

            Assert.Equal(8, counts.Black(PieceType.Pawn));
            Assert.Equal(2, counts.Black(PieceType.Rook));
            Assert.Equal(2, counts.Black(PieceType.Knight));
            Assert.Equal(2, counts.Black(PieceType.Bishop));
            Assert.Equal(1, counts.Black(PieceType.Queen));
            Assert.Equal(1, counts.Black(PieceType.King));

            Assert.Equal(32, counts.TotalCount);
        }

        // Testfall: Überprüft die initialen Rochaderechte
        [Theory]
        [InlineData(Player.White, true)] // Weiß sollte anfangs rochieren können
        [InlineData(Player.Black, true)] // Schwarz sollte anfangs rochieren können
        public void InitialCastlingRightsAreCorrect(Player player, bool expectedCanCastle)
        {
            // Arrange
            Board board = Board.Initial();

            // Act
            bool canCastleKS = board.CastleRightKS(player);
            bool canCastleQS = board.CastleRightQS(player);

            // Assert
            Assert.Equal(expectedCanCastle, canCastleKS); // Beide Rochaden sollten initial möglich sein
            Assert.Equal(expectedCanCastle, canCastleQS);
        }

        // Testfall: Rochaderechte ändern sich nach Figurenbewegung
        [Fact]
        public void CastlingRightsChangeAfterPieceMove()
        {
            // Arrange
            Board board = Board.Initial();
            Position whiteKingInitialPos = new Position(7, 4);
            Position whiteRookKSPos = new Position(7, 7);
            Piece? king = board[whiteKingInitialPos];

            // Act: König bewegen
            if (king != null) king.HasMoved = true; // Simulieren, dass der König gezogen hat

            // Assert: Rochaderechte für Weiß sollten nun falsch sein
            Assert.False(board.CastleRightKS(Player.White));
            Assert.False(board.CastleRightQS(Player.White));

            // Arrange: Schwarzen Turm bewegen für einen anderen Test
            board = Board.Initial(); // Neues initiales Brett
            Piece? blackRookQSPos = board[new Position(0, 0)];
            if (blackRookQSPos != null) blackRookQSPos.HasMoved = true;

            // Assert: Lange Rochade Schwarz nicht mehr möglich, kurze schon
            Assert.True(board.CastleRightKS(Player.Black));
            Assert.False(board.CastleRightQS(Player.Black));
        }

        // Testfall: König und Dame gegen König ist KEIN Remis durch unzureichendes Material
        [Fact]
        public void NotInsufficientMaterialKingAndQueenVsKing()
        {
            // Arrange
            Board board = new Board();
            board[new Position(0, 0)] = new King(Player.White);
            board[new Position(1, 0)] = new Queen(Player.White); // Dame
            board[new Position(7, 7)] = new King(Player.Black);

            // Act
            bool isInsufficient = board.InsufficientMaterial();

            // Assert
            Assert.False(isInsufficient);
        }

        // Testfall: König und Turm gegen König ist KEIN Remis durch unzureichendes Material
        [Fact]
        public void NotInsufficientMaterialKingAndRookVsKing()
        {
            // Arrange
            Board board = new Board();
            board[new Position(0, 0)] = new King(Player.White);
            board[new Position(1, 0)] = new Rook(Player.White); // Turm
            board[new Position(7, 7)] = new King(Player.Black);

            // Act
            bool isInsufficient = board.InsufficientMaterial();

            // Assert
            Assert.False(isInsufficient);
        }

        // Testfall: Überprüft CanCaptureEnPassant, wenn möglich
        [Fact]
        public void CanCaptureEnPassantReturnsTrueWhenPossible()
        {
            // Arrange
            Board board = new Board();
            // Weißer Bauer auf d5, schwarzer Bauer macht c7-c5
            board[new Position(3, 3)] = new Pawn(Player.White); // d5
            board[new Position(1, 2)] = new Pawn(Player.Black); // c7

            // Führe schwarzen Doppelschritt aus, um En-Passant-Situation zu erzeugen
            Move blackDoubleStep = new DoublePawn(new Position(1, 2), new Position(3, 2));
            blackDoubleStep.Execute(board); // Setzt PawnSkipPosition für Schwarz auf (2,2) (c6)

            // Act
            bool canWhiteCaptureEP = board.CanCaptureEnPassant(Player.White);

            // Assert
            Assert.True(canWhiteCaptureEP);
        }

        // Testfall: Überprüft CanCaptureEnPassant, wenn nicht möglich
        [Fact]
        public void CanCaptureEnPassantReturnsFalseWhenNotPossible()
        {
            // Arrange
            Board board = Board.Initial(); // Standardaufstellung, kein En Passant möglich

            // Act
            bool canWhiteCaptureEP = board.CanCaptureEnPassant(Player.White);
            bool canBlackCaptureEP = board.CanCaptureEnPassant(Player.Black);

            // Assert
            Assert.False(canWhiteCaptureEP);
            Assert.False(canBlackCaptureEP);
        }

        [Fact]
        public void CanCaptureEnPassantReturnsFalseAfterOneMoreMove()
        {
            // Arrange
            Board board = new Board();
            board[new Position(3, 3)] = new Pawn(Player.White); // d5
            board[new Position(1, 2)] = new Pawn(Player.Black); // c7

            Move blackDoubleStep = new DoublePawn(new Position(1, 2), new Position(3, 2));
            blackDoubleStep.Execute(board); // Black's c7-c5, skip pos c6 for black

            // White does NOT capture en passant, e.g. moves another piece
            board[new Position(7, 4)] = null; // remove white king for simplicity
            board[new Position(6, 4)] = new King(Player.White); // e2 King
            Move kingMove = new NormalMove(new Position(6, 4), new Position(7, 4));
            kingMove.Execute(board);
            // Crucially, the pawnSkipPosition for Black for the previous move should be cleared
            // if the en passant capture is not made immediately.
            // The current GameState logic handles clearing pawnSkipPosition when the turn changes.
            // Here, we manually simulate that the opportunity has passed by clearing it.
            // If CanCaptureEnPassant relies on *any* pawnSkipPosition of the opponent,
            // this test would need GameState interaction.
            // However, Board.CanCaptureEnPassant uses GetPawnSkipPosition(player.Opponent()),
            // which is fine. The *timing* of when this skip position is valid is handled by GameState.
            // For this Board unit test, we assume the skip position is either set or not.
            // If white moved another piece, the GameState would switch to black, then white.
            // When it's white's turn again, the skip position from black's c7-c5 move is no longer valid.
            // For this test, we'll assume the skip position *is* still set from black's prior move,
            // but white *could not* capture (e.g. white's pawn wasn't adjacent).
            // Let's test the case where the skip position is *cleared*.

            board.SetPawnSkipPosition(Player.Black, null); // En passant opportunity passed

            // Act
            bool canWhiteCaptureEP = board.CanCaptureEnPassant(Player.White);

            // Assert
            Assert.False(canWhiteCaptureEP);
        }
    }
}