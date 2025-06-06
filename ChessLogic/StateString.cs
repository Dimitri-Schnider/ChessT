using ChessLogic.Moves;
using ChessLogic.Utilities;
using System.Globalization;
using System.Text;

namespace ChessLogic
{
    // Generiert eine textuelle Repräsentation des Spielzustands, ähnlich der Forsyth-Edwards-Notation (FEN).
    public class StateString
    {
        private readonly StringBuilder sb = new();

        public StateString(Player currentPlayer, Board board, Move? lastMoveForEPContext)
            : this(currentPlayer, board, lastMoveForEPContext, 0, 1, false)
        {
        }

        public StateString(Player currentPlayer, Board board)
            : this(currentPlayer, board, null, 0, 1, false)
        {
        }

        // Hauptkonstruktor: Erstellt den FEN-ähnlichen String.
        public StateString(Player currentPlayer, Board board, Move? lastMoveForEPContext, int? halfMoveClock, int? fullMoveNumber, bool includeMoveCounts)
        {
            AddPiecePlacement(board);
            sb.Append(' ');
            AddCurrentPlayer(currentPlayer);
            sb.Append(' ');
            AddCastlingRights(board);
            sb.Append(' ');
            AddEnPassant(board, lastMoveForEPContext);
            if (includeMoveCounts)
            {
                sb.Append(' ');
                AddHalfMoveClock(halfMoveClock ?? 0);
                sb.Append(' ');
                AddFullMoveNumber(fullMoveNumber ?? 1);
            }
        }

        // Fügt den Teil der Figurenaufstellung zum String hinzu.
        private void AddPiecePlacement(Board board)
        {
            for (int r = 0; r < 8; r++)
            {
                if (r != 0) { sb.Append('/'); }
                AddRowData(board, r);
            }
        }

        // Fügt die Figurendaten einer einzelnen Reihe hinzu.
        private void AddRowData(Board board, int row)
        {
            int empty = 0;
            for (int c = 0; c < 8; c++)
            {
                if (board[row, c] is Piece piece)
                {
                    if (empty > 0)
                    {
                        sb.Append(empty);
                        empty = 0;
                    }
                    sb.Append(PieceChar(piece));
                }
                else
                {
                    empty++;
                }
            }
            if (empty > 0) { sb.Append(empty); }
        }

        // Konvertiert ein Piece-Objekt in sein entsprechendes FEN-Zeichen.
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
            return piece.Color == Player.White ? char.ToUpper(c, CultureInfo.InvariantCulture) : c;
        }

        // Fügt das Zeichen für den Spieler am Zug hinzu ('w' oder 'b').
        private void AddCurrentPlayer(Player currentPlayer)
        {
            sb.Append(currentPlayer == Player.White ? 'w' : 'b');
        }

        // Fügt die Informationen über die Rochaderechte hinzu.
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

        // Fügt das mögliche En-Passant-Zielfeld hinzu.
        private void AddEnPassant(Board board, Move? lastMoveForEPContext)
        {
            if (lastMoveForEPContext is not DoublePawn dpMove)
            {
                sb.Append('-');
                return;
            }

            Position skippedSquare = new((dpMove.FromPos.Row + dpMove.ToPos.Row) / 2, dpMove.FromPos.Column);
            char file = (char)('a' + skippedSquare.Column);
            int rank = 8 - skippedSquare.Row;
            sb.Append(file);
            sb.Append(rank);
        }

        // Fügt den Halbzugzähler für die 50-Züge-Regel hinzu.
        private void AddHalfMoveClock(int halfMoveClock)
        {
            sb.Append(halfMoveClock);
        }

        // Fügt die volle Zugnummer hinzu.
        private void AddFullMoveNumber(int fullMoveNumber)
        {
            sb.Append(fullMoveNumber);
        }

        // Gibt den vollständig zusammengesetzten String zurück.
        public override string ToString()
        {
            return sb.ToString();
        }
    }
}