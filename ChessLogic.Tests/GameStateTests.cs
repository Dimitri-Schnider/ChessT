using ChessLogic.Moves;
using ChessLogic.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace ChessLogic.Tests
{
    // Testklasse für die Verwaltung des Spielzustands.
    public class GameStateTests
    {
        // Testfall: Prüft, ob der anfängliche Spielzustand korrekt initialisiert wird.
        [Fact]
        public void InitialGameStateSetup()
        {
            // Arrange
            Board board = Board.Initial();
            Player startPlayer = Player.White;

            // Act
            GameState gameState = new GameState(startPlayer, board);

            // Assert
            Assert.Equal(startPlayer, gameState.CurrentPlayer);
            Assert.Same(board, gameState.Board);
            Assert.Null(gameState.Result);
        }

        // Testfall: Stellt sicher, dass nach einem Zug der Spieler korrekt gewechselt wird.
        [Fact]
        public void UpdateStateAfterMoveSwitchesPlayer()
        {
            // Arrange
            GameState gameState = new GameState(Player.White, Board.Initial());
            Move testMove = new NormalMove(new Position(6, 4), new Position(4, 4));

            // Act
            testMove.Execute(gameState.Board);
            gameState.UpdateStateAfterMove(true);

            // Assert
            Assert.Equal(Player.Black, gameState.CurrentPlayer);
        }

        // Testfall: Testet, ob ein Bauernzug die Zähler für die 50-Züge-Regel und Zugwiederholung zurücksetzt.
        [Fact]
        public void UpdateStateAfterMoveResetsFiftyMoveAndHistoryOnPawnMove()
        {
            // Arrange
            Board board = new Board();
            board[new Position(6, 0)] = new Pawn(Player.White);
            board[new Position(7, 4)] = new King(Player.White);
            board[new Position(0, 4)] = new King(Player.Black);
            GameState gameState = new GameState(Player.White, board);
            SetPrivateField(gameState, "noCaptureOrPawnMoves", 10);

            // Act
            Move pawnMove = new NormalMove(new Position(6, 0), new Position(5, 0));
            pawnMove.Execute(gameState.Board);
            gameState.UpdateStateAfterMove(true);

            // Assert
            Assert.Equal(0, GetPrivateStructField<int>(gameState, "noCaptureOrPawnMoves"));
            Assert.Single(GetPrivateField<Dictionary<string, int>>(gameState, "stateHistory"));
        }

        // Testfall: Testet, ob ein Schlagzug die Zähler für die 50-Züge-Regel und Zugwiederholung zurücksetzt.
        [Fact]
        public void UpdateStateAfterMoveResetsFiftyMoveAndHistoryOnCapture()
        {
            // Arrange
            Board board = new Board();
            board[new Position(1, 3)] = new Pawn(Player.White);
            board[new Position(0, 4)] = new Rook(Player.Black);
            board[new Position(7, 7)] = new King(Player.White);
            board[new Position(0, 0)] = new King(Player.Black);
            GameState gameState = new GameState(Player.White, board);
            SetPrivateField(gameState, "noCaptureOrPawnMoves", 10);

            // Act
            Move captureMove = new NormalMove(new Position(1, 3), new Position(0, 4));
            captureMove.Execute(gameState.Board);
            gameState.UpdateStateAfterMove(true);

            // Assert
            Assert.Equal(0, GetPrivateStructField<int>(gameState, "noCaptureOrPawnMoves"));
            Assert.Single(GetPrivateField<Dictionary<string, int>>(gameState, "stateHistory"));
        }

        // Testfall: Verifiziert die korrekte Erkennung eines Patts.
        [Fact]
        public void CheckForGameOverIdentifiesStalemate()
        {
            // Arrange: Stellt eine Patt-Situation her.
            Board board = new Board();
            board[new Position(2, 7)] = new King(Player.White);
            board[new Position(3, 6)] = new Queen(Player.White);
            board[new Position(0, 7)] = new King(Player.Black);
            GameState gameState = new GameState(Player.White, board);

            // Act: Führt den pattsetzenden Zug aus.
            Move stalematingMove = new NormalMove(new Position(3, 6), new Position(2, 6));
            stalematingMove.Execute(gameState.Board);
            gameState.UpdateStateAfterMove(false);

            // Assert: Das Spiel muss durch Patt beendet sein.
            Assert.True(gameState.IsGameOver(), "Game should be over due to stalemate.");
            Assert.NotNull(gameState.Result);
            Assert.Equal(EndReason.Stalemate, gameState.Result.Reason);
        }

        // Testfall: Prüft die korrekte Erkennung eines Remis durch die 50-Züge-Regel.
        [Fact]
        public void CheckForGameOverFiftyMoveRule()
        {
            // Arrange: Simuliert 99 Halbzüge ohne Schlag- oder Bauernzug.
            Board board = new Board();
            board[new Position(7, 7)] = new King(Player.White);
            board[new Position(7, 6)] = new Rook(Player.White);
            board[new Position(0, 0)] = new King(Player.Black);
            GameState gameState = new GameState(Player.White, board);
            SetPrivateField(gameState, "noCaptureOrPawnMoves", 99);

            // Act: Führt den 100. Halbzug aus.
            Move rookMove = new NormalMove(new Position(7, 6), new Position(7, 5));
            rookMove.Execute(gameState.Board);
            gameState.UpdateStateAfterMove(false);

            // Assert: Das Spiel muss beendet sein.
            Assert.True(gameState.IsGameOver(), "Game should be over by 50-move rule.");
            Assert.NotNull(gameState.Result);
            Assert.Equal(EndReason.FiftyMoveRule, gameState.Result.Reason);
        }

        // Testfall: Testet die Remiserkennung durch dreifache Stellungswiederholung.
        [Fact]
        public void CheckForGameOverThreefoldRepetition()
        {
            // Arrange: Erzeugt eine Sequenz, die zu einer dreifachen Wiederholung führt.
            Board board = new Board();
            board[new Position(7, 4)] = new King(Player.White);
            board[new Position(7, 3)] = new Queen(Player.White);
            board[new Position(0, 4)] = new King(Player.Black);
            board[new Position(0, 3)] = new Queen(Player.Black);
            GameState gameState = new GameState(Player.White, board);

            Move wQd1_e2 = new NormalMove(new Position(7, 3), new Position(6, 4));
            Move bQd8_e7 = new NormalMove(new Position(0, 3), new Position(1, 4));
            Move wQe2_d1 = new NormalMove(new Position(6, 4), new Position(7, 3));
            Move bQe7_d8 = new NormalMove(new Position(1, 4), new Position(0, 3));

            // Act: Spielt die Zugsequenz zweimal durch.
            wQd1_e2.Execute(gameState.Board); gameState.UpdateStateAfterMove(false);
            bQd8_e7.Execute(gameState.Board); gameState.UpdateStateAfterMove(false);
            wQe2_d1.Execute(gameState.Board); gameState.UpdateStateAfterMove(false);
            bQe7_d8.Execute(gameState.Board); gameState.UpdateStateAfterMove(false);
            Assert.False(gameState.IsGameOver());

            wQd1_e2.Execute(gameState.Board); gameState.UpdateStateAfterMove(false);
            bQd8_e7.Execute(gameState.Board); gameState.UpdateStateAfterMove(false);
            wQe2_d1.Execute(gameState.Board); gameState.UpdateStateAfterMove(false);
            bQe7_d8.Execute(gameState.Board); gameState.UpdateStateAfterMove(false);

            // Assert: Das Spiel muss nach der dritten Wiederholung beendet sein.
            Assert.True(gameState.IsGameOver(), "Game should be over by threefold repetition.");
            Assert.NotNull(gameState.Result);
            Assert.Equal(EndReason.ThreefoldRepetition, gameState.Result.Reason);
        }

        // Testfall: Stellt sicher, dass eine gefesselte Figur sich nicht so bewegen darf, dass der eigene König im Schach steht.
        [Fact]
        public void LegalMovesForPieceDoesNotAllowSelfCheckPinnedPiece()
        {
            // Arrange: Erzeugt eine Stellung mit einer gefesselten Dame.
            Board board = new Board();
            board[new Position(7, 4)] = new King(Player.White);
            board[new Position(6, 4)] = new Queen(Player.White);
            board[new Position(0, 4)] = new Rook(Player.Black);
            GameState gameState = new GameState(Player.White, board);

            // Act: Ruft die legalen Züge für die gefesselte Dame ab.
            IEnumerable<Move> queenMoves = gameState.LegalMovesForPiece(new Position(6, 4));

            // Assert: Züge aus der Fesselungslinie heraus sind nicht erlaubt.
            Assert.DoesNotContain(queenMoves, m => m.ToPos.Equals(new Position(6, 3)));
            Assert.Contains(queenMoves, m => m.ToPos.Equals(new Position(5, 4)));
        }

        // Testfall: Verifiziert die korrekte Erkennung eines Schachmatts am Beispiel des Schäfermatts.
        [Fact]
        public void CheckForGameOverIdentifiesScholarsMate()
        {
            // Arrange: Spielt die Züge des Schäfermatts.
            Board board = Board.Initial();
            GameState gameState = new GameState(Player.White, board);
            new NormalMove(new Position(6, 4), new Position(4, 4)).Execute(gameState.Board); gameState.UpdateStateAfterMove(true);
            new NormalMove(new Position(1, 4), new Position(3, 4)).Execute(gameState.Board); gameState.UpdateStateAfterMove(true);
            new NormalMove(new Position(7, 5), new Position(4, 2)).Execute(gameState.Board); gameState.UpdateStateAfterMove(false);
            new NormalMove(new Position(0, 1), new Position(2, 2)).Execute(gameState.Board); gameState.UpdateStateAfterMove(false);
            new NormalMove(new Position(7, 3), new Position(5, 5)).Execute(gameState.Board); gameState.UpdateStateAfterMove(false);
            new NormalMove(new Position(1, 3), new Position(2, 3)).Execute(gameState.Board); gameState.UpdateStateAfterMove(true);

            // Act: Führt den mattsetzenden Zug aus.
            Move mateMove = new NormalMove(new Position(5, 5), new Position(1, 5));
            mateMove.Execute(gameState.Board);
            gameState.UpdateStateAfterMove(true);

            // Assert: Das Spiel muss durch Schachmatt beendet sein.
            Assert.True(gameState.IsGameOver(), "Game should be over after Scholar's Mate (Qxf7#).");
            Assert.NotNull(gameState.Result);
            Assert.Equal(Player.White, gameState.Result.Winner);
            Assert.Equal(EndReason.Checkmate, gameState.Result.Reason);
        }

        #region Helper Methods for Reflection

        // Hilfsmethode, um private Referenztyp-Felder für Testzwecke zu lesen.
        private static T? GetPrivateField<T>(object obj, string fieldName) where T : class
        {
            FieldInfo? field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
                throw new ArgumentException($"Field '{fieldName}' not found in type {obj.GetType().Name}.", nameof(fieldName));
            return field.GetValue(obj) as T;
        }

        // Hilfsmethode, um private Werttyp-Felder für Testzwecke zu lesen.
        private static T GetPrivateStructField<T>(object obj, string fieldName) where T : struct
        {
            FieldInfo? field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
                throw new ArgumentException($"Field '{fieldName}' not found in type {obj.GetType().Name}.", nameof(fieldName));
            object? value = field.GetValue(obj);
            if (value == null)
                throw new InvalidOperationException($"Field '{fieldName}' returned null for a struct type.");
            return (T)value;
        }

        // Hilfsmethode, um private Felder für Testzwecke zu setzen.
        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            FieldInfo? field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
                throw new ArgumentException($"Field '{fieldName}' not found in type {obj.GetType().Name}.", nameof(fieldName));
            field.SetValue(obj, value);
        }

        #endregion
    }
}