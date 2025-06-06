using ChessLogic.Moves;
using ChessLogic.Utilities;
using System.Globalization;
using System.Text;

namespace ChessLogic
{
    // Generiert eine textuelle Repräsentation des Spielzustands, ähnlich der Forsyth-Edwards-Notation (FEN).
    // Diese wird für die Logik der dreifachen Stellungswiederholung und potenziell für Debugging oder externe Schnittstellen verwendet.
    public class StateString
    {
        private readonly StringBuilder sb = new StringBuilder();

        // Hauptkonstruktor: Erstellt den FEN-ähnlichen String.
        // Parameter:
        // - currentPlayer: Der Spieler, der am Zug ist.
        // - board: Der aktuelle Brettzustand.
        // - lastMoveForEPContext: Der unmittelbar vorhergegangene Zug, relevant für die En-Passant-Regel.
        // - halfMoveClock: Zähler für die 50-Züge-Regel (Anzahl Halbzüge seit dem letzten Bauernzug oder Schlagzug).
        // - fullMoveNumber: Die aktuelle volle Zugnummer (beginnt bei 1 und wird nach jedem Zug von Schwarz erhöht).
        // - includeMoveCounts: True, wenn Halbzug- und Vollzugzähler in den String aufgenommen werden sollen.
        public StateString(Player currentPlayer, Board board, Move? lastMoveForEPContext, int? halfMoveClock, int? fullMoveNumber, bool includeMoveCounts)
        {
            AddPiecePlacement(board); // 1. Figurenaufstellung.
            sb.Append(' ');
            AddCurrentPlayer(currentPlayer); // 2. Spieler am Zug.
            sb.Append(' ');
            AddCastlingRights(board); // 3. Verfügbare Rochaderechte.
            sb.Append(' ');
            AddEnPassant(board, lastMoveForEPContext); // 4. Mögliches En-Passant-Zielfeld.

            if (includeMoveCounts)
            {
                sb.Append(' ');
                AddHalfMoveClock(halfMoveClock ?? 0); // 5. Halbzugzähler.
                sb.Append(' ');
                AddFullMoveNumber(fullMoveNumber ?? 1); // 6. Volle Zugnummer.
            }
        }

        // Überladener Konstruktor für die interne Verwendung (z.B. stateHistory), bei dem Zugzähler standardmässig nicht enthalten sind.
        // `lastMoveForEPContext` ist hier wichtig, um den Zustand *nach* diesem Zug für die En-Passant-Logik korrekt darzustellen.
        public StateString(Player currentPlayer, Board board, Move? lastMoveForEPContext)
            : this(currentPlayer, board, lastMoveForEPContext, 0, 1, false)
        {
        }

        // Überladener Konstruktor, oft für Tests oder initiale Zustände verwendet, bei denen `lastMoveForEPContext` und Zugzähler nicht relevant sind.
        public StateString(Player currentPlayer, Board board)
            : this(currentPlayer, board, null, 0, 1, false)
        {
        }

        // Fügt den Teil der Figurenaufstellung (Position aller Figuren) zum FEN-String hinzu.
        private void AddPiecePlacement(Board board)
        {
            for (int r = 0; r < 8; r++) // Iteriert über jede Reihe des Bretts.
            {
                if (r != 0) { sb.Append('/'); } // Trennzeichen zwischen den Reihen.
                AddRowData(board, r); // Fügt die Daten der aktuellen Reihe hinzu.
            }
        }

        // Fügt die Figurendaten einer einzelnen Reihe zum FEN-String hinzu.
        // Leere Felder werden durch eine Zahl repräsentiert, die die Anzahl der aufeinanderfolgenden leeren Felder angibt.
        private void AddRowData(Board board, int row)
        {
            int empty = 0; // Zähler für aufeinanderfolgende leere Felder.
            for (int c = 0; c < 8; c++) // Iteriert über jede Spalte der Reihe.
            {
                Piece? piece = board[row, c];
                if (piece == null) // Wenn das Feld leer ist.
                {
                    empty++;
                    continue;
                }
                if (empty > 0) // Wenn zuvor leere Felder gezählt wurden.
                {
                    sb.Append(empty); // Fügt die Anzahl der leeren Felder hinzu.
                    empty = 0; // Setzt den Zähler zurück.
                }
                sb.Append(PieceChar(piece)); // Fügt das Zeichen für die Figur hinzu.
            }
            if (empty > 0) { sb.Append(empty); } // Fügt verbleibende leere Felder am Ende der Reihe hinzu.
        }

        // Konvertiert ein Piece-Objekt in sein entsprechendes FEN-Zeichen.
        // Grossbuchstaben für weisse Figuren, Kleinbuchstaben für schwarze.
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
                _ => ' ' // Sollte nie erreicht werden für valide Figurentypen.
            };
            if (piece.Color == Player.White) { return char.ToUpper(c, CultureInfo.InvariantCulture); }
            return c;
        }

        // Fügt das Zeichen für den Spieler am Zug zum FEN-String hinzu ('w' für Weiss, 'b' für Schwarz).
        private void AddCurrentPlayer(Player currentPlayer)
        {
            if (currentPlayer == Player.White) { sb.Append('w'); }
            else { sb.Append('b'); }
        }

        // Fügt die Informationen über die Rochaderechte zum FEN-String hinzu.
        // 'K' für weisse kurze Rochade, 'Q' für weisse lange, 'k' für schwarze kurze, 'q' für schwarze lange.
        // '-' wenn keine Rochade mehr möglich ist.
        private void AddCastlingRights(Board board)
        {
            bool castleWKS = board.CastleRightKS(Player.White);
            bool castleWQS = board.CastleRightQS(Player.White);
            bool castleBKS = board.CastleRightKS(Player.Black);
            bool castleBQS = board.CastleRightQS(Player.Black);

            if (!(castleWKS || castleWQS || castleBKS || castleBQS))
            {
                sb.Append('-'); // Keine Rochaderechte für beide Spieler.
                return;
            }
            if (castleWKS) sb.Append('K');
            if (castleWQS) sb.Append('Q');
            if (castleBKS) sb.Append('k');
            if (castleBQS) sb.Append('q');
        }

        // Fügt das mögliche En-Passant-Zielfeld zum FEN-String hinzu.
        private void AddEnPassant(Board board, Move? lastMoveForEPContext)
        {
            // Wenn der letzte Zug kein Bauern-Doppelschritt war, gibt es kein En-Passant-Feld.
            if (lastMoveForEPContext is not DoublePawn dpMove)
            {
                sb.Append('-');
                return;
            }

            // Das En-Passant-Feld ist das Feld, das vom Bauern übersprungen wurde.
            // Die FEN-Notation erfordert dieses Feld, unabhängig davon, ob ein Schlag tatsächlich möglich ist.
            Position skippedSquare = new Position((dpMove.FromPos.Row + dpMove.ToPos.Row) / 2, dpMove.FromPos.Column);
            char file = (char)('a' + skippedSquare.Column);
            int rank = 8 - skippedSquare.Row;
            sb.Append(file);
            sb.Append(rank);
        }


        // Fügt den Halbzugzähler zum FEN-String hinzu (Anzahl der Halbzüge seit dem letzten Bauernzug oder Schlagzug).
        // Wird für die 50-Züge-Regel verwendet.
        private void AddHalfMoveClock(int halfMoveClock)
        {
            sb.Append(halfMoveClock);
        }

        // Fügt die volle Zugnummer zum FEN-String hinzu.
        // Beginnt bei 1 und wird nach jedem Zug von Schwarz inkrementiert.
        private void AddFullMoveNumber(int fullMoveNumber)
        {
            sb.Append(fullMoveNumber);
        }

        // Gibt den vollständig zusammengesetzten FEN-ähnlichen String zurück.
        public override string ToString()
        {
            return sb.ToString();
        }
    }
}