using ChessLogic.Utilities;

namespace ChessLogic
{
    // Repräsentiert das Schachbrett und verwaltet Figurenpositionen sowie spezielle Spielzustände wie En-Passant-Möglichkeiten.
    public class Board
    {
        // Privates 2D-Array zur Speicherung der Figuren auf dem Brett. Ein null-Wert bedeutet ein leeres Feld.
        private readonly Piece?[,] pieces = new Piece?[8, 8];

        // Speichert für jeden Spieler die Position, auf die ein Bauer ziehen würde, um En Passant zu schlagen.
        // Null, wenn kein En Passant für den Spieler möglich ist.
        private readonly Dictionary<Player, Position?> pawnSkipPositions = new()
        {
            { Player.White, null },
            { Player.Black, null }
        };

        // Indexer für den Zugriff auf Figuren über Zeilen- und Spaltenindizes (0-7).
        public Piece? this[int row, int col]
        {
            get => pieces[row, col];
            set => pieces[row, col] = value;
        }

        // Indexer für den Zugriff auf Figuren über ein Positionsobjekt.
        public Piece? this[Position pos]
        {
            get => this[pos.Row, pos.Column];
            set => this[pos.Row, pos.Column] = value;
        }

        // Gibt die für En Passant relevante Sprungposition des gegnerischen Bauern zurück.
        public Position? GetPawnSkipPosition(Player player)
        {
            pawnSkipPositions.TryGetValue(player, out Position? pos);
            return pos;
        }

        // Setzt die für En Passant relevante Sprungposition, nachdem ein Bauer einen Doppelschritt gemacht hat.
        internal void SetPawnSkipPosition(Player player, Position? pos)
        {
            pawnSkipPositions[player] = pos;
        }

        // Erstellt ein neues Schachbrett mit der initialen Figurenaufstellung.
        public static Board Initial()
        {
            Board board = new();
            board.AddStartPieces();
            return board;
        }

        // Platziert alle Figuren in ihrer Standard-Startaufstellung auf dem Brett.
        private void AddStartPieces()
        {
            // Schwarze Figuren
            this[0, 0] = new Rook(Player.Black);
            this[0, 1] = new Knight(Player.Black);
            this[0, 2] = new Bishop(Player.Black);
            this[0, 3] = new Queen(Player.Black);
            this[0, 4] = new King(Player.Black);
            this[0, 5] = new Bishop(Player.Black);
            this[0, 6] = new Knight(Player.Black);
            this[0, 7] = new Rook(Player.Black);
            for (int c = 0; c < 8; c++) { this[1, c] = new Pawn(Player.Black); }

            // Weisse Figuren
            this[7, 0] = new Rook(Player.White);
            this[7, 1] = new Knight(Player.White);
            this[7, 2] = new Bishop(Player.White);
            this[7, 3] = new Queen(Player.White);
            this[7, 4] = new King(Player.White);
            this[7, 5] = new Bishop(Player.White);
            this[7, 6] = new Knight(Player.White);
            this[7, 7] = new Rook(Player.White);
            for (int c = 0; c < 8; c++) { this[6, c] = new Pawn(Player.White); }
        }

        // Prüft, ob eine gegebene Position innerhalb der Grenzen des 8x8-Bretts liegt.
        public static bool IsInside(Position pos)
        {
            return pos.Row >= 0 && pos.Row < 8 && pos.Column >= 0 && pos.Column < 8;
        }

        // Prüft, ob ein Feld auf der gegebenen Position leer ist (keine Figur darauf).
        public bool IsEmpty(Position pos)
        {
            return this[pos] == null;
        }

        // Gibt eine Aufzählung aller Positionen zurück, auf denen aktuell eine Figur steht.
        public IEnumerable<Position> PiecePositions()
        {
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    Position pos = new(r, c);
                    if (!IsEmpty(pos))
                    {
                        yield return pos;
                    }
                }
            }
        }

        // Gibt eine Aufzählung aller Positionen zurück, die von Figuren des angegebenen Spielers besetzt sind.
        public IEnumerable<Position> PiecePositionsFor(Player player)
        {
            return PiecePositions().Where(pos => this[pos]?.Color == player);
        }

        // Prüft, ob der König des angegebenen Spielers im Schach steht.
        public virtual bool IsInCheck(Player player)
        {
            // Iteriert über alle Figuren des Gegners und prüft, ob eine davon den König des Spielers bedroht.
            return PiecePositionsFor(player.Opponent()).Any(pos =>
            {
                Piece? piece = this[pos];
                // CanCaptureOpponentKing prüft, ob die Figur auf 'pos' den gegnerischen König schlagen könnte.
                return piece != null && piece.CanCaptureOpponentKing(pos, this);
            });
        }

        // Erstellt eine tiefe Kopie des aktuellen Brettzustands.
        // Alle Figuren und En-Passant-Informationen werden ebenfalls kopiert.
        public Board Copy()
        {
            Board copy = new();
            foreach (Position pos in PiecePositions())
            {
                Piece? originalPiece = this[pos];
                if (originalPiece != null)
                {
                    copy[pos] = originalPiece.Copy(); // Kopiert jede Figur einzeln.
                }
            }
            // Kopiert die En-Passant-Zustände.
            copy.pawnSkipPositions[Player.White] = pawnSkipPositions[Player.White];
            copy.pawnSkipPositions[Player.Black] = pawnSkipPositions[Player.Black];
            return copy;
        }

        // Zählt alle Figuren auf dem Brett und gibt ein Counting-Objekt zurück.
        public Counting CountPieces()
        {
            Counting counting = new();
            foreach (Position pos in PiecePositions())
            {
                Piece? piece = this[pos];
                if (piece != null)
                {
                    counting.Increment(piece.Color, piece.Type);
                }
            }
            return counting;
        }

        // Prüft, ob auf dem Brett eine Situation mit unzureichendem Material für ein Matt vorliegt.
        public bool InsufficientMaterial()
        {
            Counting counting = CountPieces();
            // Deckt die häufigsten Fälle für unzureichendes Material ab.
            return IsKingVKing(counting) || IsKingBishopVKing(counting) ||
                   IsKingKnightVKing(counting) || IsKingBishopVKingBishop(counting);
        }

        // Hilfsmethode: Prüft, ob nur noch zwei Könige auf dem Brett sind.
        private static bool IsKingVKing(Counting counting)
        {
            return counting.TotalCount == 2;
        }

        // Hilfsmethode: Prüft, ob ein Spieler König + Läufer gegen einen nackten König hat.
        private static bool IsKingBishopVKing(Counting counting)
        {
            return counting.TotalCount == 3 && (counting.White(PieceType.Bishop) == 1 || counting.Black(PieceType.Bishop) == 1);
        }

        // Hilfsmethode: Prüft, ob ein Spieler König + Springer gegen einen nackten König hat.
        private static bool IsKingKnightVKing(Counting counting)
        {
            return counting.TotalCount == 3 && (counting.White(PieceType.Knight) == 1 || counting.Black(PieceType.Knight) == 1);
        }

        // Hilfsmethode: Prüft, ob beide Spieler König + Läufer haben und die Läufer auf Feldern gleicher Farbe stehen.
        private bool IsKingBishopVKingBishop(Counting counting)
        {
            if (counting.TotalCount != 4)
            {
                return false;
            }

            if (counting.White(PieceType.Bishop) != 1 || counting.Black(PieceType.Bishop) != 1)
            {
                return false;
            }

            Position? wBishopPos = FindPiece(Player.White, PieceType.Bishop);
            Position? bBishopPos = FindPiece(Player.Black, PieceType.Bishop);

            // Remis, wenn beide Läufer auf Feldern derselben Farbe stehen.
            return wBishopPos != null && bBishopPos != null && wBishopPos.SquareColor() == bBishopPos.SquareColor();
        }

        // Findet die erste Figur eines bestimmten Typs und einer bestimmten Farbe auf dem Brett.
        private Position? FindPiece(Player color, PieceType type)
        {
            return PiecePositionsFor(color).FirstOrDefault(pos => this[pos]?.Type == type);
        }

        // Prüft, ob König und der entsprechende Turm für die Rochade noch unbewegt sind.
        private bool IsUnmovedKingAndRook(Position kingPos, Position rookPos)
        {
            if (IsEmpty(kingPos) || IsEmpty(rookPos))
            {
                return false;
            }

            Piece? king = this[kingPos];
            Piece? rook = this[rookPos];
            return king != null && rook != null &&
                   king.Type == PieceType.King && rook.Type == PieceType.Rook &&
                   !king.HasMoved && !rook.HasMoved;
        }

        // Prüft das Rochaderecht für die kurze Rochade (Königsseite) des angegebenen Spielers.
        public bool CastleRightKS(Player player)
        {
            return player switch
            {
                Player.White => IsUnmovedKingAndRook(new Position(7, 4), new Position(7, 7)), // e1, h1
                Player.Black => IsUnmovedKingAndRook(new Position(0, 4), new Position(0, 7)), // e8, h8
                _ => false
            };
        }

        // Prüft das Rochaderecht für die lange Rochade (Damenseite) des angegebenen Spielers.
        public bool CastleRightQS(Player player)
        {
            return player switch
            {
                Player.White => IsUnmovedKingAndRook(new Position(7, 4), new Position(7, 0)), // e1, a1
                Player.Black => IsUnmovedKingAndRook(new Position(0, 4), new Position(0, 0)), // e8, a8
                _ => false
            };
        }

        // Hilfsmethode für CanCaptureEnPassant: Prüft, ob ein Bauer des Spielers auf einer der potenziellen Startpositionen
        // für einen En-Passant-Schlag auf 'skipPos' steht und dieser legal wäre.
        private bool HasPawnInPosition(Player player, Position[] pawnPositions, Position skipPos)
        {
            foreach (Position pos in pawnPositions.Where(IsInside)) // Nur Positionen auf dem Brett berücksichtigen.
            {
                Piece? piece = this[pos];
                // Figur muss existieren, dem Spieler gehören und ein Bauer sein.
                if (piece == null || piece.Color != player || piece.Type != PieceType.Pawn)
                {
                    continue;
                }
                EnPassant move = new EnPassant(pos, skipPos);
                if (move.IsLegal(this)) // Prüft, ob der En-Passant-Schlag legal ist (z.B. kein Selbstschach).
                {
                    return true;
                }
            }
            return false;
        }

        // Prüft, ob der angegebene Spieler aktuell einen En-Passant-Schlag ausführen kann.
        public bool CanCaptureEnPassant(Player player)
        {
            // Holt die Position, die der gegnerische Bauer im letzten Zug übersprungen hat.
            Position? skipPos = GetPawnSkipPosition(player.Opponent());

            if (skipPos == null) // Wenn keine solche Position existiert, ist kein En Passant möglich.
            {
                return false;
            }

            // Definiert die Positionen, auf denen ein Bauer des Spielers stehen müsste, um En Passant schlagen zu können.
            Position[] pawnPositions = player switch
            {
                Player.White => new Position[] { skipPos + Direction.SouthWest, skipPos + Direction.SouthEast },
                Player.Black => new Position[] { skipPos + Direction.NorthWest, skipPos + Direction.NorthEast },
                _ => Array.Empty<Position>() // Sollte nicht für Player.None aufgerufen werden.
            };
            return HasPawnInPosition(player, pawnPositions, skipPos);
        }
    }
}