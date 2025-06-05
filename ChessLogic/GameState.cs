using ChessLogic.Moves;
using ChessLogic.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessLogic
{
    // Verwaltet den Zustand einer laufenden Schachpartie.
    public class GameState
    {
        // Das aktuelle Schachbrett mit den Figurenpositionen.
        public Board Board { get; }
        // Der Spieler, der aktuell am Zug ist.
        public Player CurrentPlayer { get; private set; }
        // Das Ergebnis des Spiels (null, wenn das Spiel noch läuft).
        public Result? Result { get; private set; }

        // Zähler für die 50-Züge-Regel: Anzahl der Halbzüge ohne Bauernbewegung oder Figurenschlag.
        private int noCaptureOrPawnMoves;
        // Öffentlicher Getter für noCaptureOrPawnMoves.
        public int NoCaptureOrPawnMoves => noCaptureOrPawnMoves;

        // Speichert die Häufigkeit jeder aufgetretenen Brettstellung (als FEN-ähnlicher String) zur Erkennung der dreifachen Stellungswiederholung.
        private readonly Dictionary<string, int> stateHistory = new Dictionary<string, int>();
        // Der letzte ausgeführte Zug, relevant für die FEN-Generierung (En-Passant-Ziel).
        private Move? lastMoveForHistory;

        // Konstruktor: Initialisiert einen neuen Spielzustand.
        public GameState(Player firstPlayerToMove, Board board)
        {
            CurrentPlayer = firstPlayerToMove;
            Board = board;
            this.lastMoveForHistory = null; // Am Anfang gibt es keinen vorherigen Zug.
            // Initialisiert die Historie mit dem Ausgangszustand.
            string initialFen = new StateString(CurrentPlayer, Board, this.lastMoveForHistory).ToString();
            stateHistory[initialFen] = 1;
        }

        // Gibt alle legalen Züge für die Figur auf der gegebenen Position zurück.
        // Berücksichtigt, ob der Spieler am Zug ist und ob der Zug den eigenen König im Schach lassen würde.
        public IEnumerable<Move> LegalMovesForPiece(Position pos)
        {
            // Keine Züge, wenn das Feld leer ist oder die Figur nicht dem aktuellen Spieler gehört.
            if (Board.IsEmpty(pos) || Board[pos]!.Color != CurrentPlayer)
            {
                return Enumerable.Empty<Move>();
            }

            Piece piece = Board[pos]!;
            // Holt alle potenziellen Züge der Figur (ohne Berücksichtigung von Selbstschach).
            IEnumerable<Move> moveCandidates = piece.GetMoves(pos, Board);
            // Filtert die Züge, die legal sind (d.h. den eigenen König nicht im Schach lassen).
            return moveCandidates.Where(move => move.IsLegal(Board));
        }

        // Aktualisiert den Spielzustand nach einem ausgeführten Zug.
        public void UpdateStateAfterMove(bool captureOrPawnOccurred, bool updateRepetitionHistory = true, Move? move = null)
        {
            Player playerWhoseTurnItWas = CurrentPlayer;
            CurrentPlayer = CurrentPlayer.Opponent(); // Wechselt den Spieler.
            this.lastMoveForHistory = move; // Speichert den gerade ausgeführten Zug.

            if (captureOrPawnOccurred) // Wenn ein Bauer gezogen oder eine Figur geschlagen wurde:
            {
                noCaptureOrPawnMoves = 0; // Setzt den 50-Züge-Zähler zurück.
                stateHistory.Clear();    // Setzt die Historie für die dreifache Wiederholung zurück.
            }
            else
            {
                noCaptureOrPawnMoves++; // Erhöht den 50-Züge-Zähler.
            }

            if (updateRepetitionHistory)
            {
                string currentStateString = new StateString(CurrentPlayer, Board, this.lastMoveForHistory).ToString();
                // Aktualisiert die Zählung der aktuellen Stellung in der Historie.
                if (stateHistory.TryGetValue(currentStateString, out int currentCount))
                {
                    stateHistory[currentStateString] = currentCount + 1;
                }
                else
                {
                    stateHistory[currentStateString] = 1;
                }
            }

            // Löscht die En-Passant-Möglichkeit des Spielers, der gerade gezogen hat,
            // wenn der Zug kein Bauern-Doppelschritt war.
            if (move is not DoublePawn)
            {
                Board.SetPawnSkipPosition(playerWhoseTurnItWas, null);
            }
            CheckForGameOver(); // Prüft nach jedem Zug auf Spielende-Bedingungen.
        }

        // Zeichnet den aktuellen Brettzustand für die Dreifachwiederholungsprüfung auf.
        // Wird nach Karteneffekten verwendet, die das Brett verändern, aber kein regulärer Zug sind.
        public void RecordCurrentStateForRepetition(Move? cardEffectAsMoveRepresentation)
        {
            this.lastMoveForHistory = cardEffectAsMoveRepresentation;
            string currentStateString = new StateString(this.CurrentPlayer, this.Board, this.lastMoveForHistory).ToString();

            if (stateHistory.TryGetValue(currentStateString, out int currentCount))
            {
                stateHistory[currentStateString] = currentCount + 1;
            }
            else
            {
                stateHistory[currentStateString] = 1;
            }
        }

        // Überschreibt den aktuellen Spieler, z.B. für Karteneffekte wie "Extrazug".
        public void SetCurrentPlayerOverride(Player player)
        {
            CurrentPlayer = player;
            this.lastMoveForHistory = null; // Nach einer solchen Änderung gibt es keinen direkten "letzten Zug".
        }

        // Setzt das Spielergebnis.
        public void SetResult(Result result)
        {
            Result = result;
        }

        // Gibt alle legalen Züge für den angegebenen Spieler zurück.
        public IEnumerable<Move> AllLegalMovesFor(Player player)
        {
            // Wenn das Spiel bereits beendet ist oder der angeforderte Spieler nicht am Zug ist, gibt es keine Züge.
            if (Result != null && player == CurrentPlayer) // Spiel vorbei, aber wir fragen nach dem Spieler, der gerade dran wäre
            {
                return Enumerable.Empty<Move>();
            }
            // Wenn das Spiel nicht vorbei ist, aber der falsche Spieler abgefragt wird
            if (Result == null && player != CurrentPlayer)
            {
                return Enumerable.Empty<Move>();
            }


            List<Move> legalMoves = new List<Move>();
            // Iteriert über alle Figuren des Spielers und sammelt deren legale Züge.
            foreach (Position pos in Board.PiecePositionsFor(player))
            {
                Piece piece = Board[pos]!; // Die Figur kann hier nicht null sein.
                if (piece.Color == player) // Doppelte Sicherheitsprüfung.
                {
                    legalMoves.AddRange(piece.GetMoves(pos, Board).Where(m => m.IsLegal(Board)));
                }
            }
            return legalMoves;
        }

        // Prüft, ob das Spiel beendet ist und setzt ggf. das Ergebnis.
        public void CheckForGameOver()
        {
            if (Result != null) return; // Spiel ist bereits beendet.

            // Wenn der aktuelle Spieler keine legalen Züge mehr hat:
            if (!AllLegalMovesFor(CurrentPlayer).Any())
            {
                if (Board.IsInCheck(CurrentPlayer)) // Schachmatt.
                {
                    Result = Result.Win(CurrentPlayer.Opponent(), EndReason.Checkmate);
                }
                else // Patt.
                {
                    Result = Result.Draw(EndReason.Stalemate);
                }
                return;
            }

            // 50-Züge-Regel.
            if (noCaptureOrPawnMoves >= 100) // 100 Halbzüge = 50 volle Züge.
            {
                Result = Result.Draw(EndReason.FiftyMoveRule);
                return;
            }

            // Dreifache Stellungswiederholung.
            string currentStateString = new StateString(CurrentPlayer, Board, this.lastMoveForHistory).ToString();
            if (stateHistory.TryGetValue(currentStateString, out int count) && count >= 3)
            {
                Result = Result.Draw(EndReason.ThreefoldRepetition);
                return;
            }

            // Unzureichendes Material.
            if (Board.InsufficientMaterial())
            {
                Result = Result.Draw(EndReason.InsufficientMaterial);
            }
        }

        // Gibt true zurück, wenn das Spiel beendet ist (ein Ergebnis vorliegt).
        public bool IsGameOver()
        {
            return Result != null;
        }
    }
}