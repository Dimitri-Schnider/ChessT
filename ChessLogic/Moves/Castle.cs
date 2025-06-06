using ChessLogic.Utilities;
using System;

namespace ChessLogic
{
    // Repräsentiert einen Rochade-Zug.
    public class Castle : Move
    {
        public override MoveType Type { get; }      // Der Typ des Rochade-Zugs (kurz oder lang).
        public override Position FromPos { get; }    // Die Startposition des Königs.
        public override Position ToPos { get; }      // Die Zielposition des Königs.

        private readonly Direction kingMoveDir;      // Die Richtung, in die sich der König bewegt.
        private readonly Position rookFromPos;      // Die Startposition des beteiligten Turms.
        private readonly Position rookToPos;        // Die Zielposition des beteiligten Turms.

        public Castle(MoveType type, Position kingPos)
        {
            Type = type;
            FromPos = kingPos;

            if (type == MoveType.CastleKS) // Kurze Rochade
            {
                kingMoveDir = Direction.East;
                ToPos = new Position(kingPos.Row, 6);       // König zieht auf g-Linie.
                rookFromPos = new Position(kingPos.Row, 7); // Turm startet auf h-Linie.
                rookToPos = new Position(kingPos.Row, 5);   // Turm zieht auf f-Linie.
            }
            else if (type == MoveType.CastleQS) // Lange Rochade
            {
                kingMoveDir = Direction.West;
                ToPos = new Position(kingPos.Row, 2);       // König zieht auf c-Linie.
                rookFromPos = new Position(kingPos.Row, 0); // Turm startet auf a-Linie.
                rookToPos = new Position(kingPos.Row, 3);   // Turm zieht auf d-Linie.
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(type), "Ungültiger Rochadetyp für Castle-Konstruktor.");
            }
        }

        // Führt die Rochade aus, indem König und Turm bewegt werden.
        public override bool Execute(Board board)
        {
            new NormalMove(FromPos, ToPos).Execute(board);
            new NormalMove(rookFromPos, rookToPos).Execute(board);
            return false; // Rochade ist kein Schlag- oder Bauernzug.
        }

        // Prüft, ob die Rochade legal ist (König nicht im Schach und zieht nicht über bedrohte Felder).
        public override bool IsLegal(Board board)
        {
            Player player = board[FromPos]!.Color;
            if (board.IsInCheck(player))
            {
                return false;
            }

            Board copy = board.Copy();
            Position kingPosInCopy = FromPos;

            for (int i = 0; i < 2; i++)
            {
                new NormalMove(kingPosInCopy, kingPosInCopy + kingMoveDir).Execute(copy);
                kingPosInCopy += kingMoveDir;

                if (copy.IsInCheck(player))
                {
                    return false;
                }
            }

            return true;
        }
    }
}