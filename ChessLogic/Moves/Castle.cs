using ChessLogic.Utilities;
using System;

namespace ChessLogic
{
    // Repräsentiert einen Rochade-Zug.
    public class Castle : Move
    {
        // Typ des Rochade-Zugs (kurz oder lang).
        public override MoveType Type { get; }
        // Startposition des Königs.
        public override Position FromPos { get; }
        // Zielposition des Königs nach der Rochade.
        public override Position ToPos { get; }

        // Richtung, in die sich der König bewegt.
        private readonly Direction kingMoveDir;
        // Startposition des Turms, der an der Rochade beteiligt ist.
        private readonly Position rookFromPos;
        // Zielposition des Turms nach der Rochade.
        private readonly Position rookToPos;

        // Konstruktor für einen Rochade-Zug.
        public Castle(MoveType type, Position kingPos)
        {
            Type = type;
            FromPos = kingPos;

            if (type == MoveType.CastleKS) // Kurze Rochade (Königsseite).
            {
                kingMoveDir = Direction.East;
                ToPos = new Position(kingPos.Row, 6); // König zieht auf g-Linie.
                rookFromPos = new Position(kingPos.Row, 7); // Turm startet auf h-Linie.
                rookToPos = new Position(kingPos.Row, 5); // Turm zieht auf f-Linie.
            }
            else if (type == MoveType.CastleQS) // Lange Rochade (Damenseite).
            {
                kingMoveDir = Direction.West;
                ToPos = new Position(kingPos.Row, 2); // König zieht auf c-Linie.
                rookFromPos = new Position(kingPos.Row, 0); // Turm startet auf a-Linie.
                rookToPos = new Position(kingPos.Row, 3); // Turm zieht auf d-Linie.
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(type), "Ungültiger Rochadetyp für Castle-Konstruktor.");
            }
        }

        // Führt die Rochade auf dem Brett aus.
        // Bewegt König und Turm an ihre neuen Positionen.
        // Gibt false zurück, da Rochade kein Schlag- oder Bauernzug ist (relevant für 50-Züge-Regel).
        public override bool Execute(Board board)
        {
            new NormalMove(FromPos, ToPos).Execute(board); // Königszug.
            new NormalMove(rookFromPos, rookToPos).Execute(board); // Turmzug.
            return false;
        }

        // Prüft, ob die Rochade unter den aktuellen Umständen legal ist.
        public override bool IsLegal(Board board)
        {
            Piece? kingPiece = board[FromPos];
            if (kingPiece == null)
            {
                return false;
            }

            Player player = kingPiece.Color;
            // König darf nicht im Schach stehen.
            if (board.IsInCheck(player))
            {
                return false;
            }

            Board copy = board.Copy();
            Position kingPosInCopy = FromPos;

            // König darf nicht über ein bedrohtes Feld ziehen.
            for (int i = 0; i < 2; i++)
            {
                new NormalMove(kingPosInCopy, kingPosInCopy + kingMoveDir).Execute(copy);
                kingPosInCopy += kingMoveDir;

                if (copy.IsInCheck(player))
                {
                    return false;
                }
            }
            // Das Zielfeld des Königs darf ebenfalls nicht bedroht sein (wird durch die Schleife abgedeckt).
            return true;
        }
    }
}