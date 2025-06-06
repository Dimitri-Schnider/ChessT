using ChessLogic.Moves;
using ChessLogic.Utilities;
using Xunit;

namespace ChessLogic.Tests
{
    // Testklasse für die Erzeugung von FEN-ähnlichen Zustandsbeschreibungen.
    public class StateStringTests
    {
        // Testfall: Überprüft, ob der FEN-ähnliche String für die Startaufstellung korrekt ist.
        [Fact]
        public void InitialBoardStateStringIsCorrect()
        {
            // Arrange
            Board board = Board.Initial();
            Player currentPlayer = Player.White;
            // Act
            string stateString = new StateString(currentPlayer, board).ToString();
            // Assert
            string expectedFenPrefix = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq -";
            Assert.Equal(expectedFenPrefix, stateString);
        }

        // Testfall: Testet den FEN-String nach dem Eröffnungszug e2-e4.
        [Fact]
        public void StateStringAfterE2E4IsCorrect()
        {
            // Arrange
            Board board = Board.Initial();
            Player initialPlayer = Player.White;
            Move whitePawnDoubleStep = new DoublePawn(new Position(6, 4), new Position(4, 4));
            whitePawnDoubleStep.Execute(board);
            Player currentPlayerAfterMove = initialPlayer.Opponent();

            // Act
            string stateString = new StateString(currentPlayerAfterMove, board, whitePawnDoubleStep).ToString();

            // Assert
            string expectedFenPrefix = "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3";
            Assert.Equal(expectedFenPrefix, stateString);
        }

        // Testfall: Überprüft den FEN-String, nachdem alle Rochaderechte durch Figurenbewegungen verloren gegangen sind.
        [Fact]
        public void StateStringWithNoCastlingRights()
        {
            // Arrange: Simuliert, dass alle Könige und Türme bereits gezogen haben.
            Board board = Board.Initial();
            if (board[new Position(7, 4)] is Piece wk) wk.HasMoved = true;
            if (board[new Position(7, 0)] is Piece wra) wra.HasMoved = true;
            if (board[new Position(7, 7)] is Piece wrh) wrh.HasMoved = true;
            if (board[new Position(0, 4)] is Piece bk) bk.HasMoved = true;
            if (board[new Position(0, 0)] is Piece bra) bra.HasMoved = true;
            if (board[new Position(0, 7)] is Piece brh) brh.HasMoved = true;

            // Act
            string stateString = new StateString(Player.White, board).ToString();

            // Assert
            string expectedFenPrefix = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w - -";
            Assert.Equal(expectedFenPrefix, stateString);
        }

        // Testfall: Stellt sicher, dass der FEN-String korrekt anzeigt, wenn Schwarz am Zug ist.
        [Fact]
        public void StateStringForBlackToMove()
        {
            // Arrange
            Board board = Board.Initial();
            Player currentPlayer = Player.Black;
            // Act
            string stateString = new StateString(currentPlayer, board).ToString();
            // Assert
            string expectedFenPrefix = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR b KQkq -";
            Assert.Equal(expectedFenPrefix, stateString);
        }

        // Testfall: Testet einen komplexeren FEN-String nach mehreren Zügen, inklusive einer Rochade.
        [Fact]
        public void StateStringAfterSeveralMovesAndBlackCastlesQueenSide()
        {
            // Arrange
            Board board = new Board();
            board[0, 4] = new King(Player.Black);
            board[0, 0] = new Rook(Player.Black);
            board[1, 4] = new Pawn(Player.Black);
            board[7, 4] = new King(Player.White);
            board[7, 0] = new Rook(Player.White);
            board[7, 7] = new Rook(Player.White);
            board[4, 4] = new Pawn(Player.White);
            board[4, 4]!.HasMoved = true;
            board[0, 4]!.HasMoved = false;
            board[0, 0]!.HasMoved = false;
            board[7, 4]!.HasMoved = false;
            board[7, 0]!.HasMoved = false;
            board[7, 7]!.HasMoved = false;

            // Act
            Move castleQS = new Castle(MoveType.CastleQS, new Position(0, 4));
            castleQS.Execute(board);
            string stateString = new StateString(Player.White, board).ToString();

            // Assert
            string expectedFenPrefix = "2kr4/4p3/8/8/4P3/8/8/R3K2R w KQ -";
            Assert.Equal(expectedFenPrefix, stateString);
        }
    }
}