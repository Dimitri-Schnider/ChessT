using ChessLogic.Utilities;
using System.Linq;

namespace ChessLogic
{
    // Repräsentiert das Schachbrett und verwaltet Figurenpositionen.
    public class Board
    {
        // Speichert Figurenpositionen; null bedeutet leeres Feld.
        private readonly Piece?[,] pieces = new Piece?[8, 8];
        // Speichert En-Passant-Sprungpositionen für Bauern.
        private readonly Dictionary<Player, Position?> pawnSkipPositions = new Dictionary<Player, Position?>
        {
            { Player.White, null },
            { Player.Black, null }
        };
        // Indexer für Zugriff via Zeile/Spalte.
        public Piece? this[int row, int col]
        {
            get { return pieces[row, col]; }
            set { pieces[row, col] = value; }
        }

        // Indexer für Zugriff via Positionsobjekt.
        public Piece? this[Position pos]
        {
            get { return this[pos.Row, pos.Column]; }
            set { this[pos.Row, pos.Column] = value; }
        }

        // Ruft die letzte En-Passant-Sprungposition eines Spielers ab.
        public Position? GetPawnSkipPosition(Player player)
        {
            pawnSkipPositions.TryGetValue(player, out Position? pos);
            return pos;
        }

        // Setzt die letzte En-Passant-Sprungposition eines Spielers.
        public void SetPawnSkipPosition(Player player, Position? pos)
        {
            pawnSkipPositions[player] = pos;
        }

        // Erstellt ein Brett mit initialer Figurenaufstellung.
        public static Board Initial()
        {
            Board board = new Board();
            board.AddStartPieces();
            return board;
        }

        // Platziert Figuren in ihrer Startaufstellung.
        private void AddStartPieces()
        {
            this[0, 0] = new Rook(Player.Black);
            this[0, 1] = new Knight(Player.Black);
            this[0, 2] = new Bishop(Player.Black);
            this[0, 3] = new Queen(Player.Black);
            this[0, 4] = new King(Player.Black);
            this[0, 5] = new Bishop(Player.Black);
            this[0, 6] = new Knight(Player.Black);
            this[0, 7] = new Rook(Player.Black);
            for (int c = 0; c < 8; c++) { this[1, c] = new Pawn(Player.Black); }

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

        // Prüft, ob eine Position innerhalb des Bretts liegt.
        public static bool IsInside(Position pos)
        {
            return pos.Row >= 0 && pos.Row < 8 && pos.Column >= 0 && pos.Column < 8;
        }

        // Prüft, ob ein Feld leer ist.
        public bool IsEmpty(Position pos)
        {
            return this[pos] == null;
        }

        // Gibt alle besetzten Felder zurück.
        public IEnumerable<Position> PiecePositions()
        {
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    Position pos = new Position(r, c);
                    if (!IsEmpty(pos)) { yield return pos; }
                }
            }
        }

        // Gibt alle besetzten Felder eines Spielers zurück.
        public IEnumerable<Position> PiecePositionsFor(Player player)
        {
            return PiecePositions().Where(pos => this[pos]?.Color == player);
        }

        // Prüft, ob der König eines Spielers im Schach steht.
        public bool IsInCheck(Player player)
        {
            return PiecePositionsFor(player.Opponent()).Any(pos =>
            {
                Piece? piece = this[pos];
                return piece != null && piece.CanCaptureOpponentKing(pos, this);
            });
        }

        // Erstellt eine tiefe Kopie des Bretts.
        public Board Copy()
        {
            Board copy = new Board();
            foreach (Position pos in PiecePositions())
            {
                Piece? originalPiece = this[pos];
                if (originalPiece != null)
                {
                    copy[pos] = originalPiece.Copy();
                }
            }
            copy.pawnSkipPositions[Player.White] = this.pawnSkipPositions[Player.White];
            copy.pawnSkipPositions[Player.Black] = this.pawnSkipPositions[Player.Black];
            return copy;
        }

        // Zählt die Figuren auf dem Brett.
        public Counting CountPieces()
        {
            Counting counting = new Counting();
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

        // Prüft auf Remis durch unzureichendes Material.
        public bool InsufficientMaterial()
        {
            Counting counting = CountPieces();
            return IsKingVKing(counting) || IsKingBishopVKing(counting) ||
                   IsKingKnightVKing(counting) || IsKingBishopVKingBishop(counting);
        }

        // Hilfsmethode: König gegen König.
        private static bool IsKingVKing(Counting counting) => counting.TotalCount == 2;

        // Hilfsmethode: König und Läufer gegen König.
        private static bool IsKingBishopVKing(Counting counting)
        {
            return counting.TotalCount == 3 && (counting.White(PieceType.Bishop) == 1 || counting.Black(PieceType.Bishop) == 1);
        }

        // Hilfsmethode: König und Springer gegen König.
        private static bool IsKingKnightVKing(Counting counting)
        {
            return counting.TotalCount == 3 && (counting.White(PieceType.Knight) == 1 || counting.Black(PieceType.Knight) == 1);
        }

        // Hilfsmethode: König und Läufer gegen König und Läufer (gleichfarbige Läufer).
        private bool IsKingBishopVKingBishop(Counting counting)
        {
            if (counting.TotalCount != 4) return false;
            if (counting.White(PieceType.Bishop) != 1 || counting.Black(PieceType.Bishop) != 1) return false;

            Position? wBishopPos = FindPiece(Player.White, PieceType.Bishop);
            Position? bBishopPos = FindPiece(Player.Black, PieceType.Bishop);
            return wBishopPos != null && bBishopPos != null && wBishopPos.SquareColor() == bBishopPos.SquareColor();
        }

        // Findet die erste Figur eines Typs für einen Spieler.
        private Position? FindPiece(Player color, PieceType type)
        {
            return PiecePositionsFor(color).FirstOrDefault(pos => this[pos]?.Type == type);
        }

        // Prüft, ob König und Turm für Rochade unbewegt sind.
        private bool IsUnmovedKingAndRook(Position kingPos, Position rookPos)
        {
            if (IsEmpty(kingPos) || IsEmpty(rookPos)) return false;
            Piece? king = this[kingPos];
            Piece? rook = this[rookPos];
            return king != null && rook != null &&
                   king.Type == PieceType.King && rook.Type == PieceType.Rook &&
                   !king.HasMoved && !rook.HasMoved;
        }

        // Prüft Rochaderecht Königsseite.
        public bool CastleRightKS(Player player)
        {
            return player switch
            {
                Player.White => IsUnmovedKingAndRook(new Position(7, 4), new Position(7, 7)),
                Player.Black => IsUnmovedKingAndRook(new Position(0, 4), new Position(0, 7)),
                _ => false
            };
        }

        // Prüft Rochaderecht Damenseite.
        public bool CastleRightQS(Player player)
        {
            return player switch
            {
                Player.White => IsUnmovedKingAndRook(new Position(7, 4), new Position(7, 0)),
                Player.Black => IsUnmovedKingAndRook(new Position(0, 4), new Position(0, 0)),
                _ => false
            };
        }

        // Hilfsmethode für En-Passant-Prüfung.
        private bool HasPawnInPosition(Player player, Position[] pawnPositions, Position skipPos)
        {
            foreach (Position pos in pawnPositions.Where(IsInside))
            {
                Piece? piece = this[pos];
                if (piece == null || piece.Color != player || piece.Type != PieceType.Pawn)
                {
                    continue;
                }
                EnPassant move = new EnPassant(pos, skipPos);
                if (move.IsLegal(this)) { return true; }
            }
            return false;
        }

        // Prüft, ob En Passant möglich ist.
        public bool CanCaptureEnPassant(Player player)
        {
            Position? skipPos = GetPawnSkipPosition(player.Opponent());
            if (skipPos == null) { return false; }

            Position[] pawnPositions = player switch
            {
                Player.White => new Position[] { skipPos + Direction.SouthWest, skipPos + Direction.SouthEast },
                Player.Black => new Position[] { skipPos + Direction.NorthWest, skipPos + Direction.NorthEast },
                _ => Array.Empty<Position>()
            };
            return HasPawnInPosition(player, pawnPositions, skipPos);
        }
    }
}