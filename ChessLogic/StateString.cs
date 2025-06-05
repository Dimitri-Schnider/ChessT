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
            AddEnPassant(board, currentPlayer, lastMoveForEPContext); // 4. Mögliches En-Passant-Zielfeld.

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
        // Das Feld wird in algebraischer Notation angegeben (z.B. "e3").
        // Ist kein En-Passant-Schlag möglich, wird '-' angehängt.
        // `currentPlayer` ist der Spieler, der *jetzt* am Zug ist und potenziell En Passant schlagen könnte.
        // `lastMoveForEPContext` ist der unmittelbar vorhergegangene Zug des Gegners.
        private void AddEnPassant(Board board, Player currentPlayer, Move? lastMoveForEPContext)
        {
            Position? enPassantSquareForFen = null;

            // En Passant ist nur möglich, wenn der *Gegner* (`currentPlayer.Opponent()`)
            // im vorherigen Zug einen Bauern-Doppelschritt gemacht hat.
            if (lastMoveForEPContext is DoublePawn dpMove)
            {
                Piece? pawnThatMadeDoubleMove = board[dpMove.ToPos]; // Der Bauer nach seinem Doppelschritt.
                if (pawnThatMadeDoubleMove != null && pawnThatMadeDoubleMove.Color == currentPlayer.Opponent())
                {
                    // Das übersprungene Feld ist das potenzielle En-Passant-Zielfeld.
                    Position skippedSquare = new Position((dpMove.FromPos.Row + dpMove.ToPos.Row) / 2, dpMove.FromPos.Column);

                    // Prüfe, ob der `currentPlayer` einen Bauern hat, der dieses `skippedSquare` En Passant schlagen kann.
                    // Linker Angreifer für `currentPlayer`:
                    Position leftAttackerPos = new Position(dpMove.ToPos.Row, dpMove.ToPos.Column - 1);
                    if (Board.IsInside(leftAttackerPos) &&
                        board[leftAttackerPos]?.Type == PieceType.Pawn &&
                        board[leftAttackerPos]?.Color == currentPlayer)
                    {
                        // Simuliere den En-Passant-Zug und prüfe, ob er legal ist (kein Selbstschach).
                        EnPassant epMoveTest = new EnPassant(leftAttackerPos, skippedSquare);
                        if (epMoveTest.IsLegal(board))
                        {
                            enPassantSquareForFen = skippedSquare;
                        }
                    }

                    // Rechter Angreifer für `currentPlayer` (nur prüfen, wenn links keiner gefunden/legal war):
                    if (enPassantSquareForFen == null)
                    {
                        Position rightAttackerPos = new Position(dpMove.ToPos.Row, dpMove.ToPos.Column + 1);
                        if (Board.IsInside(rightAttackerPos) &&
                            board[rightAttackerPos]?.Type == PieceType.Pawn &&
                            board[rightAttackerPos]?.Color == currentPlayer)
                        {
                            EnPassant epMoveTest = new EnPassant(rightAttackerPos, skippedSquare);
                            if (epMoveTest.IsLegal(board))
                            {
                                enPassantSquareForFen = skippedSquare;
                            }
                        }
                    }
                }
            }

            if (enPassantSquareForFen == null)
            {
                sb.Append('-'); // Kein En-Passant möglich.
            }
            else
            {
                // Konvertiert die 0-basierte Position in algebraische Notation (z.B. "e3").
                char file = (char)('a' + enPassantSquareForFen.Column);
                int rank = 8 - enPassantSquareForFen.Row; // FEN-Rang ist 1-8 von unten nach oben.
                sb.Append(file);
                sb.Append(rank);
            }
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