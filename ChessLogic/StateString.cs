// File: [SolutionDir]/ChessLogic/StateString.cs
using System.Text;
using System.Globalization;
using ChessLogic.Utilities; // Für Position
using ChessLogic.Moves;     // Für Move, DoublePawn

namespace ChessLogic
{
    // Generiert eine String-Repräsentation des Spielzustands (ähnlich FEN).
    public class StateString
    {
        private readonly StringBuilder sb = new StringBuilder();

        // Konstruktor: Baut den Zustands-String.
        // Nimmt jetzt optional den letzten Zug entgegen, um En-Passant korrekt zu behandeln.
        public StateString(Player currentPlayer, Board board, Move? lastMove = null)
        {
            AddPiecePlacement(board);
            sb.Append(' ');
            AddCurrentPlayer(currentPlayer);
            sb.Append(' ');
            AddCastlingRights(board);
            sb.Append(' ');
            AddEnPassant(board, currentPlayer, lastMove); // lastMove wird hier übergeben
        }

        // Gibt den erstellten Zustands-String zurück.
        public override string ToString()
        {
            return sb.ToString();
        }

        // Gibt das Zeichen für eine Figur zurück.
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

        // Fügt Figuren einer Reihe zum String hinzu.
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

        // Fügt die gesamte Figurenaufstellung zum String hinzu.
        private void AddPiecePlacement(Board board)
        {
            for (int r = 0; r < 8; r++)
            {
                if (r != 0) { sb.Append('/'); }
                AddRowData(board, r);
            }
        }

        // Fügt den aktuellen Spieler zum String hinzu.
        private void AddCurrentPlayer(Player currentPlayer)
        {
            if (currentPlayer == Player.White) { sb.Append('w'); }
            else { sb.Append('b'); }
        }

        // Fügt Rochaderechte zum String hinzu.
        private void AddCastlingRights(Board board)
        {
            bool castleWKS = board.CastleRightKS(Player.White);
            bool castleWQS = board.CastleRightQS(Player.White);
            bool castleBKS = board.CastleRightKS(Player.Black);
            bool castleBQS = board.CastleRightQS(Player.Black);
            if (!(castleWKS || castleWQS || castleBKS || castleBQS)) { sb.Append('-'); return; }
            if (castleWKS) sb.Append('K');
            if (castleWQS) sb.Append('Q');
            if (castleBKS) sb.Append('k');
            if (castleBQS) sb.Append('q');
        }

        // Fügt En-Passant-Zielquadrat zum String hinzu.
        // Berücksichtigt den letzten Zug, um das korrekte EP-Quadrat zu identifizieren.
        private void AddEnPassant(Board board, Player currentPlayer, Move? lastMove)
        {
            Position? enPassantTargetSquare = null;

            // Das En-Passant-Feld im FEN wird gesetzt, wenn der *letzte* Zug
            // ein Doppelbauernschritt des Gegners war und dadurch eine
            // En-Passant-Schlagmöglichkeit für den aktuellen Spieler entstanden ist.
            if (lastMove is DoublePawn) // Prüft, ob der letzte Zug ein DoublePawn war.
            {
                // Der Spieler, der den Doppelbauernschritt gemacht hat, ist currentPlayer.Opponent().
                // Die Methode GetPawnSkipPosition(farbeDesSpielersDerDoppelschrittMachte)
                // liefert das Feld, auf das geschlagen werden könnte (das Feld *hinter* dem gezogenen Bauern).
                enPassantTargetSquare = board.GetPawnSkipPosition(currentPlayer.Opponent());
            }
            // Wenn lastMove kein DoublePawn war, oder wenn das Board kein gültiges
            // En-Passant-Feld für den Gegner des aktuellen Spielers hat, bleibt enPassantTargetSquare null.

            if (enPassantTargetSquare == null)
            {
                sb.Append('-');
                return;
            }

            // Konvertiere die Board-Koordinaten (0-7 für Zeile und Spalte) in algebraische Notation.
            char file = (char)('a' + enPassantTargetSquare.Column);
            // FEN-Ränge sind 1-8, wobei 1 die Grundreihe von Weiss ist. Array-Zeilen sind 0-7 von oben (Schwarz).
            int rank = 8 - enPassantTargetSquare.Row;
            sb.Append(file);
            sb.Append(rank);
        }
    }
}