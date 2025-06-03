// File: GameStateTests.cs
using Xunit;
using ChessLogic;
using ChessLogic.Utilities;
using ChessLogic.Moves;    // Für NormalMove etc.
using System;                // Für ArgumentOutOfRangeException, Enum
using System.Linq;
using System.Collections.Generic;
using System.Reflection;     // Für Reflection (GetInstanceField)

namespace ChessLogic.Tests
{
    public class GameStateTests
    {
        // Hilfsmethode um private Felder für Testzwecke zu lesen (Reflection)
        // Typsicherer gemacht und umbenannt für Klarheit
        private static T? GetPrivateField<T>(object obj, string fieldName) where T : class
        {
            FieldInfo? field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
                throw new ArgumentException($"Field '{fieldName}' not found in type {obj.GetType().Name}.", nameof(fieldName));
            return field.GetValue(obj) as T;
        }

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


        // Hilfsmethode um private Felder zu setzen (Reflection) - Vorsicht bei der Verwendung!
        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            FieldInfo? field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
                throw new ArgumentException($"Field '{fieldName}' not found in type {obj.GetType().Name}.", nameof(fieldName));
            field.SetValue(obj, value);
        }

        [Fact]
        public void InitialGameStateSetup()
        {
            Board board = Board.Initial();
            Player startPlayer = Player.White;
            GameState gameState = new GameState(startPlayer, board);

            Assert.Equal(startPlayer, gameState.CurrentPlayer);
            Assert.Same(board, gameState.Board); // Der GameState Konstruktor weist das Board direkt zu
            Assert.Null(gameState.Result);
        }

        [Fact]
        public void UpdateStateAfterMoveSwitchesPlayer()
        {
            Board board = Board.Initial();
            GameState gameState = new GameState(Player.White, board);
            Move testMove = new NormalMove(new Position(6, 4), new Position(4, 4)); // e2-e4

            testMove.Execute(gameState.Board); // Wichtig: Zug auf dem Board des GameStates ausführen
            gameState.UpdateStateAfterMove(true); // true, da Bauernzug

            Assert.Equal(Player.Black, gameState.CurrentPlayer);
        }

        [Fact]
        public void UpdateStateAfterMoveResetsFiftyMoveAndHistoryOnPawnMove()
        {
            Board board = new Board();
            board[new Position(6, 0)] = new Pawn(Player.White);
            board[new Position(7, 4)] = new King(Player.White);
            board[new Position(0, 4)] = new King(Player.Black);
            GameState gameState = new GameState(Player.White, board);

            // Manuell Zähler und History manipulieren für den Test
            SetPrivateField(gameState, "noCaptureOrPawnMoves", 10);
            var stateHistory = GetPrivateField<Dictionary<string, int>>(gameState, "stateHistory");
            Assert.NotNull(stateHistory);
            stateHistory!["someFakeState1"] = 1; // Null-Forgiving, da NotNull geprüft
            stateHistory["someFakeState2"] = 2;

            Move pawnMove = new NormalMove(new Position(6, 0), new Position(5, 0)); // a2-a3
            pawnMove.Execute(gameState.Board);
            gameState.UpdateStateAfterMove(true); // Bauernzug! captureOrPawn = true

            Assert.Equal(0, GetPrivateStructField<int>(gameState, "noCaptureOrPawnMoves"));
            var historyAfter = GetPrivateField<Dictionary<string, int>>(gameState, "stateHistory");
            Assert.NotNull(historyAfter);
            // Nach einem Bauernzug/Schlag wird stateHistory.Clear() aufgerufen, dann wird der neue Zustand hinzugefügt.
            Assert.Single(historyAfter!);
            Assert.False(gameState.IsGameOver());
        }

        [Fact]
        public void UpdateStateAfterMoveResetsFiftyMoveAndHistoryOnCapture()
        {
            Board board = new Board();
            board[new Position(1, 3)] = new Pawn(Player.White);
            board[new Position(0, 4)] = new Rook(Player.Black);
            board[new Position(7, 7)] = new King(Player.White);
            board[new Position(0, 0)] = new King(Player.Black);
            GameState gameState = new GameState(Player.White, board);

            SetPrivateField(gameState, "noCaptureOrPawnMoves", 10);
            var stateHistory = GetPrivateField<Dictionary<string, int>>(gameState, "stateHistory");
            Assert.NotNull(stateHistory);
            stateHistory!["someFakeState1"] = 1;

            Move captureMove = new NormalMove(new Position(1, 3), new Position(0, 4)); // d7xe8
            captureMove.Execute(gameState.Board);
            gameState.UpdateStateAfterMove(true); // Schlagzug (captureOrPawn = true)

            Assert.Equal(0, GetPrivateStructField<int>(gameState, "noCaptureOrPawnMoves"));
            var historyAfter = GetPrivateField<Dictionary<string, int>>(gameState, "stateHistory");
            Assert.NotNull(historyAfter);
            Assert.Single(historyAfter!);
        }

        [Fact]
        public void CheckForGameOverIdentifiesStalemate()
        {
            // Arrange: Setup *bevor* Weiß den pattsetzenden Zug macht.
            // Weiß: Kh6 (2,7), Qg5 (3,6). Schwarz: Kh8 (0,7). Weiß ist am Zug.
            Board board = new Board();
            board[new Position(2, 7)] = new King(Player.White);   // Kh6
            board[new Position(3, 6)] = new Queen(Player.White);  // Qg5 (auf g5)
            board[new Position(0, 7)] = new King(Player.Black);   // Kh8

            // Initialisiere GameState mit Weiß am Zug.
            GameState gameState = new GameState(Player.White, board);

            // Weißer Zug: Qg5 nach g6 (2,6), was Schwarz patt setzt.
            Move stalematingMove = new NormalMove(new Position(3, 6), new Position(2, 6));

            // Act
            // Stelle sicher, dass der pattsetzende Zug für Weiß legal ist.
            var whiteQueen = gameState.Board[new Position(3, 6)];
            Assert.NotNull(whiteQueen);
            // Assert.Contains(stalematingMove.ToPos, whiteQueen.GetMoves(new Position(3,6), gameState.Board).Select(m => m.ToPos));
            // Eine robustere Prüfung wäre über gameState.LegalMovesForPiece:
            var legalMovesForQueen = gameState.LegalMovesForPiece(new Position(3, 6));
            Assert.Contains(legalMovesForQueen, m => m.ToPos.Equals(new Position(2, 6)) && m.Type == MoveType.Normal);


            stalematingMove.Execute(gameState.Board); // Weiß zieht Qg5-g6.

            // Aktualisiere Spielzustand. 'false', da kein Bauern-/Schlagzug.
            // CurrentPlayer wird zu Schwarz, CheckForGameOver wird für Schwarz ausgeführt.
            gameState.UpdateStateAfterMove(false);

            // Assert
            // Wenn die nächste Assertion (IsGameOver) fehlschlägt, liegt es wahrscheinlich daran, dass
            // AllLegalMovesFor(Player.Black).Any() in CheckForGameOver `true` zurückgibt,
            // obwohl Schwarz keine legalen Züge hat.
            Assert.True(gameState.IsGameOver(), "Game should be over due to stalemate.");
            Assert.NotNull(gameState.Result); // Dies war der ursprüngliche Fehlerpunkt.
            if (gameState.Result != null)
            {
                Assert.Equal(Player.None, gameState.Result.Winner);
                Assert.Equal(EndReason.Stalemate, gameState.Result.Reason);
            }
        }

        [Fact]
        public void CheckForGameOverFiftyMoveRule()
        {
            // Arrange
            Board board = new Board();
            // Wichtig: Figuren, die *nicht* InsufficientMaterial sind! Z.B. K+R vs K
            board[new Position(7, 7)] = new King(Player.White); // Kh1
            board[new Position(7, 6)] = new Rook(Player.White); // Rg1
            board[new Position(0, 0)] = new King(Player.Black); // Ka8

            GameState gameState = new GameState(Player.White, board);

            // Setze den Zähler für die 50-Züge-Regel manuell auf 99 Halbzüge
            SetPrivateField(gameState, "noCaptureOrPawnMoves", 99);

            // Der 100. Halbzug. Weiß ist am Zug.
            // Ein legaler Zug, der weder ein Bauernzug noch ein Schlagzug ist.
            // Z.B. Rg1-Rf1
            Position fromPos = new Position(7, 6); // Rg1
            Position toPos = new Position(7, 5);   // Rf1
            Move rookMove = new NormalMove(fromPos, toPos);

            Assert.True(rookMove.IsLegal(gameState.Board), "Rook move for 50-move test should be legal for board state.");
            // Um die volle Legalität im GameState zu prüfen (Selbstschach etc.):
            var legalMovesForRook = gameState.LegalMovesForPiece(fromPos);
            Assert.Contains(legalMovesForRook, m => m.ToPos.Equals(toPos) && m.Type == MoveType.Normal);

            // Act
            rookMove.Execute(gameState.Board);
            gameState.UpdateStateAfterMove(false); // Dies ist der 100. Halbzug ohne Reset des Zählers

            // Assert
            Assert.True(gameState.IsGameOver(), "Game should be over by 50-move rule.");
            Assert.NotNull(gameState.Result);
            if (gameState.Result != null)
            {
                Assert.Equal(Player.None, gameState.Result.Winner);
                Assert.Equal(EndReason.FiftyMoveRule, gameState.Result.Reason);
            }
        }

        [Fact]
        public void CheckForGameOverThreefoldRepetition()
        {
            // Arrange
            Board board = new Board();
            // Setup: Weiß Ke1, Qd1. Schwarz Ke8, Qd8.
            // Weiß kann Qd1-e2, Schwarz Qd8-e7, Weiß Qe2-d1, Schwarz Qe7-d8 -> 2. Mal Ausgangsstellung für Weiß
            // Weiß Qd1-e2, Schwarz Qd8-e7, Weiß Qe2-d1, Schwarz Qe7-d8 -> 3. Mal Ausgangsstellung für Weiß -> Remis
            board[new Position(7, 4)] = new King(Player.White); // Ke1
            board[new Position(7, 3)] = new Queen(Player.White); // Qd1
            board[new Position(0, 4)] = new King(Player.Black); // Ke8
            board[new Position(0, 3)] = new Queen(Player.Black); // Qd8

            GameState gameState = new GameState(Player.White, board); // Zustand S0 (W am Zug), Historie: {S0:1}

            Move wQd1_e2 = new NormalMove(new Position(7, 3), new Position(6, 4)); // Qd1-e2
            Move bQd8_e7 = new NormalMove(new Position(0, 3), new Position(1, 4)); // Qd8-e7
            Move wQe2_d1 = new NormalMove(new Position(6, 4), new Position(7, 3)); // Qe2-d1
            Move bQe7_d8 = new NormalMove(new Position(1, 4), new Position(0, 3)); // Qe7-d8

            // Spielsequenz, um S0 dreimal zu erreichen, wenn Weiß am Zug ist:
            // S0 (W am Zug) - Initialzustand, Zähler 1 in Historie aus Konstruktor.

            // 1. Wiederholung von S0 (S0 tritt zum 2. Mal auf)
            wQd1_e2.Execute(gameState.Board); gameState.UpdateStateAfterMove(false); // W1 -> B am Zug, Zustand S_W1
            bQd8_e7.Execute(gameState.Board); gameState.UpdateStateAfterMove(false); // B1 -> W am Zug, Zustand S_B1
            wQe2_d1.Execute(gameState.Board); gameState.UpdateStateAfterMove(false); // W2 -> B am Zug, Zustand S_W2
            bQe7_d8.Execute(gameState.Board); gameState.UpdateStateAfterMove(false); // B2 -> W am Zug, Zustand S0 wieder erreicht. Zähler für S0 ist jetzt 2.
            Assert.False(gameState.IsGameOver(), "Game should not be over after 2nd repetition of S0");

            // 2. Wiederholung von S0 (S0 tritt zum 3. Mal auf)
            wQd1_e2.Execute(gameState.Board); gameState.UpdateStateAfterMove(false); // W3 -> B am Zug, Zustand S_W1
            bQd8_e7.Execute(gameState.Board); gameState.UpdateStateAfterMove(false); // B3 -> W am Zug, Zustand S_B1
            wQe2_d1.Execute(gameState.Board); gameState.UpdateStateAfterMove(false); // W4 -> B am Zug, Zustand S_W2
            bQe7_d8.Execute(gameState.Board); gameState.UpdateStateAfterMove(false); // B4 -> W am Zug, Zustand S0 wieder erreicht. Zähler für S0 ist jetzt 3 -> Remis!

            // Assert
            Assert.True(gameState.IsGameOver(), "Game should be over by threefold repetition.");
            Assert.NotNull(gameState.Result);
            if (gameState.Result != null)
            {
                Assert.Equal(Player.None, gameState.Result.Winner);
                Assert.Equal(EndReason.ThreefoldRepetition, gameState.Result.Reason);
            }
        }

        [Fact]
        public void LegalMovesForPieceDoesNotAllowSelfCheckPinnedPiece()
        {
            Board board = new Board();
            King whiteKing = new King(Player.White);
            Queen whiteQueen = new Queen(Player.White);
            Rook blackRook = new Rook(Player.Black);
            Position kingPos = new Position(7, 4);
            Position queenPos = new Position(6, 4);
            Position attackingRookPos = new Position(0, 4);
            board[kingPos] = whiteKing;
            board[queenPos] = whiteQueen;
            board[attackingRookPos] = blackRook;
            GameState gameState = new GameState(Player.White, board);
            IEnumerable<Move> queenMoves = gameState.LegalMovesForPiece(queenPos);
            Position illegalTargetPos = new Position(6, 3);
            Position legalTargetPosAlongPin = new Position(5, 4);
            Assert.DoesNotContain(queenMoves, m => m.ToPos.Equals(illegalTargetPos));
            Assert.Contains(queenMoves, m => m.ToPos.Equals(legalTargetPosAlongPin));
        }

        [Fact]
        public void CheckForGameOverIdentifiesScholarsMate()
        {
            // Arrange: Starte mit der Initialaufstellung
            Board board = Board.Initial();
            GameState gameState = new GameState(Player.White, board); // Weiß beginnt

            // Züge für das Schäfermatt (eine gängige Variante)
            // 1. e4 e5
            // 2. Bc4 Nc6 (Schwarz entwickelt Springer, stoppt Matt nicht direkt)
            // 3. Qf3 d6  (Schwarz öffnet für Läufer, verteidigt f7 nicht)
            // 4. Qxf7# (Dame schlägt Bauer auf f7, gedeckt von Läufer auf c4)

            Move move1_white_e4 = new NormalMove(new Position(6, 4), new Position(4, 4)); // e2-e4
            move1_white_e4.Execute(gameState.Board);
            gameState.UpdateStateAfterMove(true); // Bauernzug

            Move move1_black_e5 = new NormalMove(new Position(1, 4), new Position(3, 4)); // e7-e5
            move1_black_e5.Execute(gameState.Board);
            gameState.UpdateStateAfterMove(true); // Bauernzug

            Move move2_white_Bc4 = new NormalMove(new Position(7, 5), new Position(4, 2)); // Bf1-c4
            move2_white_Bc4.Execute(gameState.Board);
            gameState.UpdateStateAfterMove(false);

            Move move2_black_Nc6 = new NormalMove(new Position(0, 1), new Position(2, 2)); // Nb8-c6
            move2_black_Nc6.Execute(gameState.Board);
            gameState.UpdateStateAfterMove(false);

            Move move3_white_Qf3 = new NormalMove(new Position(7, 3), new Position(5, 5)); // Qd1-f3
            move3_white_Qf3.Execute(gameState.Board);
            gameState.UpdateStateAfterMove(false);

            // Ein Zug für Schwarz, der f7 nicht verteidigt, z.B. d6, um den Läufer zu öffnen
            Move move3_black_d6 = new NormalMove(new Position(1, 3), new Position(2, 3)); // d7-d6
            move3_black_d6.Execute(gameState.Board);
            gameState.UpdateStateAfterMove(true); // Bauernzug

            // Act: Weiß setzt Matt
            // Der Bauer auf f7 ist an Position (1,5)
            Move white_Qxf7_mate = new NormalMove(new Position(5, 5), new Position(1, 5)); // Qf3xf7#

            // Stelle sicher, dass der Zug prinzipiell legal ist auf dem Brett
            var whiteQueen = gameState.Board[new Position(5, 5)]; // Dame auf f3
            Assert.NotNull(whiteQueen);
            var legalMovesForQueen = gameState.LegalMovesForPiece(new Position(5, 5));
            // Prüfen, ob der Schlag auf f7 (1,5) unter den legalen Zügen ist
            Assert.Contains(legalMovesForQueen, m => m.ToPos.Equals(new Position(1, 5)));


            white_Qxf7_mate.Execute(gameState.Board); // Dame schlägt Bauer auf f7
            // Der Schlagzug ist auch ein "captureOrPawn" Zug.
            // CurrentPlayer wird zu Schwarz, CheckForGameOver wird für Schwarz ausgeführt.
            gameState.UpdateStateAfterMove(true);

            // Assert
            // Wenn dieser Test fehlschlägt, bedeutet das, dass die Spiellogik das Matt nicht erkennt.
            // Höchstwahrscheinlich findet `AllLegalMovesFor(Player.Black)` fälschlicherweise noch Züge für Schwarz.
            Assert.True(gameState.IsGameOver(), "Game should be over after Scholar's Mate (Qxf7#).");
            Assert.NotNull(gameState.Result);
            if (gameState.Result != null)
            {
                Assert.Equal(Player.White, gameState.Result.Winner);
                Assert.Equal(EndReason.Checkmate, gameState.Result.Reason);
            }
        }
    }
}