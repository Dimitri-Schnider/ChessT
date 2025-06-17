using ChessLogic.Moves;
using ChessLogic.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace ChessLogic
{
    // Verwaltet den dynamischen Zustand einer laufenden Schachpartie.
    public class GameState
    {
        public virtual Board Board { get; }                             // Das aktuelle Schachbrett mit den Figurenpositionen.
        public virtual Player CurrentPlayer { get; private set; }       // Der Spieler, der aktuell am Zug ist.
        public Result? Result { get; private set; }                     // Das Ergebnis des Spiels (null, wenn das Spiel noch läuft).
        public int NoCaptureOrPawnMoves => noCaptureOrPawnMoves;        // Öffentlicher Getter für den 50-Züge-Zähler.

        private int noCaptureOrPawnMoves;                               // Zähler für die 50-Züge-Regel.
        private readonly Dictionary<string, int> stateHistory = new();  // Speichert die Häufigkeit jeder Brettstellung.
        private Move? lastMoveForHistory;                               // Der letzte ausgeführte Zug für die FEN-Generierung.

        // Parameterloser Konstruktor für das Mocking in Tests.
        public GameState() { }

        // Initialisiert einen neuen Spielzustand.
        public GameState(Player firstPlayerToMove, Board board)
        {
            CurrentPlayer = firstPlayerToMove;
            Board = board;
            lastMoveForHistory = null;

            string initialFen = new StateString(CurrentPlayer, Board, lastMoveForHistory).ToString();
            stateHistory[initialFen] = 1;
        }

        // Gibt alle legalen Züge für die Figur auf der gegebenen Position zurück.
        public IEnumerable<Move> LegalMovesForPiece(Position pos)
        {
            if (Board.IsEmpty(pos) || Board[pos]!.Color != CurrentPlayer)
            {
                return Enumerable.Empty<Move>();
            }

            Piece piece = Board[pos]!;
            IEnumerable<Move> moveCandidates = piece.GetMoves(pos, Board);
            return moveCandidates.Where(move => move.IsLegal(Board));
        }

        // Aktualisiert den Spielzustand nach einem ausgeführten Zug.
        public void UpdateStateAfterMove(bool captureOrPawnOccurred, bool updateRepetitionHistory = true, Move? move = null)
        {
            Player playerWhoseTurnItWas = CurrentPlayer;
            CurrentPlayer = CurrentPlayer.Opponent();
            lastMoveForHistory = move;

            if (captureOrPawnOccurred)
            {
                noCaptureOrPawnMoves = 0;
                stateHistory.Clear();
            }
            else
            {
                noCaptureOrPawnMoves++;
            }

            if (updateRepetitionHistory)
            {
                RecordCurrentStateForRepetition(move);
            }

            if (move is not DoublePawn)
            {
                Board.SetPawnSkipPosition(playerWhoseTurnItWas, null);
            }

            CheckForGameOver();
        }

        // Zeichnet den aktuellen Brettzustand für die Dreifachwiederholungsprüfung auf.
        public void RecordCurrentStateForRepetition(Move? moveContext)
        {
            lastMoveForHistory = moveContext;
            string currentStateString = new StateString(CurrentPlayer, Board, lastMoveForHistory).ToString();

            if (stateHistory.TryGetValue(currentStateString, out int currentCount))
            {
                stateHistory[currentStateString] = currentCount + 1;
            }
            else
            {
                stateHistory[currentStateString] = 1;
            }
        }

        // Überschreibt den aktuellen Spieler (z.B. für Karteneffekte wie "Extrazug").
        public void SetCurrentPlayerOverride(Player player)
        {
            CurrentPlayer = player;
            lastMoveForHistory = null;
        }

        // Setzt das Spielergebnis.
        public void SetResult(Result result)
        {
            Result = result;
        }

        // Gibt alle legalen Züge für den angegebenen Spieler zurück.
        public IEnumerable<Move> AllLegalMovesFor(Player player)
        {
            if (Result != null && player == CurrentPlayer)
            {
                return Enumerable.Empty<Move>();
            }
            if (Result == null && player != CurrentPlayer)
            {
                return Enumerable.Empty<Move>();
            }

            List<Move> legalMoves = new();
            foreach (Position pos in Board.PiecePositionsFor(player))
            {
                Piece piece = Board[pos]!;
                if (piece.Color == player)
                {
                    legalMoves.AddRange(piece.GetMoves(pos, Board).Where(m => m.IsLegal(Board)));
                }
            }
            return legalMoves;
        }

        // Prüft, ob das Spiel beendet ist und setzt ggf. das Ergebnis.
        public void CheckForGameOver()
        {
            if (Result != null) return;

            if (!AllLegalMovesFor(CurrentPlayer).Any())
            {
                Result = Board.IsInCheck(CurrentPlayer)
                    ? Result.Win(CurrentPlayer.Opponent(), EndReason.Checkmate)
                    : Result.Draw(EndReason.Stalemate);
                return;
            }

            if (noCaptureOrPawnMoves >= 100)
            {
                Result = Result.Draw(EndReason.FiftyMoveRule);
                return;
            }

            string currentStateString = new StateString(CurrentPlayer, Board, lastMoveForHistory).ToString();
            if (stateHistory.TryGetValue(currentStateString, out int count) && count >= 3)
            {
                Result = Result.Draw(EndReason.ThreefoldRepetition);
                return;
            }

            if (Board.InsufficientMaterial())
            {
                Result = Result.Draw(EndReason.InsufficientMaterial);
            }
        }

        // Gibt true zurück, wenn das Spiel beendet ist.
        public bool IsGameOver()
        {
            return Result != null;
        }
    }
}