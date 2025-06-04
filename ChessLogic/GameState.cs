using ChessLogic.Moves;
using ChessLogic.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessLogic
{
    public class GameState
    {
        public Board Board { get; }
        public Player CurrentPlayer { get; private set; }
        public Result? Result { get; private set; }
        private int noCaptureOrPawnMoves;
        public int NoCaptureOrPawnMoves => noCaptureOrPawnMoves; // Öffentlicher Getter
        private readonly Dictionary<string, int> stateHistory = new Dictionary<string, int>();
        private Move? lastMoveForHistory;

        public GameState(Player firstPlayerToMove, Board board)
        {
            CurrentPlayer = firstPlayerToMove;
            Board = board;
            this.lastMoveForHistory = null;
            string initialFen = new StateString(CurrentPlayer, Board, this.lastMoveForHistory).ToString();
            stateHistory[initialFen] = 1;
        }

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

        public void UpdateStateAfterMove(bool captureOrPawnOccurred, bool updateRepetitionHistory = true, Move? move = null)
        {
            Player playerWhoseTurnItWas = CurrentPlayer;
            CurrentPlayer = CurrentPlayer.Opponent();
            this.lastMoveForHistory = move;

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
                string currentStateString = new StateString(CurrentPlayer, Board, this.lastMoveForHistory).ToString();
                if (stateHistory.TryGetValue(currentStateString, out int currentCount))
                {
                    stateHistory[currentStateString] = currentCount + 1;
                }
                else
                {
                    stateHistory[currentStateString] = 1;
                }
            }

            if (move is not DoublePawn)
            {
                Board.SetPawnSkipPosition(playerWhoseTurnItWas, null);
            }
            CheckForGameOver();
        }

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

        public void SetCurrentPlayerOverride(Player player)
        {
            CurrentPlayer = player;
            this.lastMoveForHistory = null;
        }

        public void SetResult(Result result)
        {
            Result = result;
        }

        public IEnumerable<Move> AllLegalMovesFor(Player player)
        {
            if (player != CurrentPlayer && Result == null)
            {
                return Enumerable.Empty<Move>();
            }
            if (Result != null && player == CurrentPlayer)
            {
                return Enumerable.Empty<Move>();
            }

            List<Move> legalMoves = new List<Move>();
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

        public void CheckForGameOver() // Sicherstellen, dass diese public ist
        {
            if (Result != null) return;
            if (!AllLegalMovesFor(CurrentPlayer).Any())
            {
                if (Board.IsInCheck(CurrentPlayer))
                {
                    Result = Result.Win(CurrentPlayer.Opponent(), EndReason.Checkmate);
                }
                else
                {
                    Result = Result.Draw(EndReason.Stalemate);
                }
                return;
            }

            if (noCaptureOrPawnMoves >= 100)
            {
                Result = Result.Draw(EndReason.FiftyMoveRule);
                return;
            }

            string currentStateString = new StateString(CurrentPlayer, Board, this.lastMoveForHistory).ToString();
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

        public bool IsGameOver()
        {
            return Result != null;
        }
    }
}