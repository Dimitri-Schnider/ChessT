// File: [SolutionDir]\ChessLogic\StateString.cs
using System.Text;
using System.Globalization;
using ChessLogic.Utilities; // Für Position
using ChessLogic.Moves; // Für Move, DoublePawn

namespace ChessLogic
{
    public class StateString
    {
        private readonly StringBuilder sb = new StringBuilder();

        // NEUER Konstruktor für die Tests (nimmt 2 Argumente)
        public StateString(Player currentPlayer, Board board)
            : this(currentPlayer, board, null, 0, 1, false) // Ruft den Hauptkonstruktor mit Standardwerten auf
        {
        }

        // Konstruktor für interne stateHistory (ohne Zugzähler in FEN, falls so gewünscht)
        // Der lastMoveContext ist hier wichtig, um den Zustand *nach* diesem Zug korrekt darzustellen.
        public StateString(Player currentPlayer, Board board, Move? lastMoveForEPContext)
            : this(currentPlayer, board, lastMoveForEPContext, 0, 1, false) // Default für interne Nutzung, falls Zähler nicht gebraucht
        {
        }

        // Hauptkonstruktor, der die optionalen Zähler für die API-FEN aufnehmen kann
        public StateString(Player currentPlayer, Board board, Move? lastMoveForEPContext, int? halfMoveClock, int? fullMoveNumber, bool includeMoveCounts = true)
        {
            AddPiecePlacement(board);
            sb.Append(' ');
            AddCurrentPlayer(currentPlayer);
            sb.Append(' ');
            AddCastlingRights(board);
            sb.Append(' ');
            AddEnPassant(board, currentPlayer, lastMoveForEPContext);
            if (includeMoveCounts)
            {
                sb.Append(' ');
                AddHalfMoveClock(halfMoveClock.HasValue ? halfMoveClock.Value : 0);
                sb.Append(' ');
                AddFullMoveNumber(fullMoveNumber.HasValue ? fullMoveNumber.Value : 1);
            }
        }

        private void AddPiecePlacement(Board board)
        {
            for (int r = 0; r < 8; r++)
            {
                if (r != 0) { sb.Append('/'); }
                AddRowData(board, r);
            }
        }

        private void AddRowData(Board board, int row)
        {
            int empty = 0;
            for (int c = 0; c < 8; c++)
            {
                Piece? piece = board[row, c];
                if (piece == null)
                {
                    empty++;
                    continue;
                }
                if (empty > 0) { sb.Append(empty); empty = 0; }
                sb.Append(PieceChar(piece));
            }
            if (empty > 0) { sb.Append(empty); }
        }

        private static char PieceChar(Piece piece)
        {
            char c = piece.Type switch
            {
                PieceType.Pawn => 'p',
                PieceType.Knight => 'n',
                PieceType.Rook => 'r',
                PieceType.Bishop => 'b',
                PieceType.Queen => 'q',
                PieceType.King => 'k',
                _ => ' '
            };
            if (piece.Color == Player.White) { return char.ToUpper(c, CultureInfo.InvariantCulture); }
            return c;
        }

        private void AddCurrentPlayer(Player currentPlayer)
        {
            if (currentPlayer == Player.White) { sb.Append('w'); }
            else { sb.Append('b'); }
        }

        private void AddCastlingRights(Board board)
        {
            bool castleWKS = board.CastleRightKS(Player.White);
            bool castleWQS = board.CastleRightQS(Player.White);
            bool castleBKS = board.CastleRightKS(Player.Black);
            bool castleBQS = board.CastleRightQS(Player.Black);
            if (!(castleWKS || castleWQS || castleBKS || castleBQS))
            {
                sb.Append('-');
                return;
            }
            if (castleWKS) sb.Append('K');
            if (castleWQS) sb.Append('Q');
            if (castleBKS) sb.Append('k');
            if (castleBQS) sb.Append('q');
        }

        private void AddEnPassant(Board board, Player currentPlayer, Move? lastMoveJustMade)
        {
            Position? enPassantSquareForFen = null;

            if (lastMoveJustMade is DoublePawn dpMove)
            {
                Piece? pawnThatMadeDoubleMove = board[dpMove.ToPos];
                if (pawnThatMadeDoubleMove != null && pawnThatMadeDoubleMove.Color == currentPlayer.Opponent())
                {
                    Position skippedSquare = new Position((dpMove.FromPos.Row + dpMove.ToPos.Row) / 2, dpMove.FromPos.Column);
                    Position leftAttackerPos = new Position(dpMove.ToPos.Row, dpMove.ToPos.Column - 1);
                    if (Board.IsInside(leftAttackerPos) &&
                        board[leftAttackerPos]?.Type == PieceType.Pawn &&
                        board[leftAttackerPos]?.Color == currentPlayer)
                    {
                        enPassantSquareForFen = skippedSquare;
                    }

                    if (enPassantSquareForFen == null)
                    {
                        Position rightAttackerPos = new Position(dpMove.ToPos.Row, dpMove.ToPos.Column + 1);
                        if (Board.IsInside(rightAttackerPos) &&
                            board[rightAttackerPos]?.Type == PieceType.Pawn &&
                            board[rightAttackerPos]?.Color == currentPlayer)
                        {
                            enPassantSquareForFen = skippedSquare;
                        }
                    }
                }
            }
            if (enPassantSquareForFen == null)
            {
                sb.Append('-');
            }
            else
            {
                char file = (char)('a' + enPassantSquareForFen.Column);
                int rank = 8 - enPassantSquareForFen.Row;
                sb.Append(file);
                sb.Append(rank);
            }
        }

        private void AddHalfMoveClock(int halfMoveClock)
        {
            sb.Append(halfMoveClock);
        }

        private void AddFullMoveNumber(int fullMoveNumber)
        {
            sb.Append(fullMoveNumber);
        }

        public override string ToString()
        {
            return sb.ToString();
        }
    }
}