using ChessLogic.Utilities;
using System;

namespace ChessLogic
{
    // Repräsentiert einen Rochade-Zug.
    public class Castle : Move
    {
        // Typ des Zugs (kurze oder lange Rochade).
        public override MoveType Type { get; }
        // Startposition des Königs.
        public override Position FromPos { get; }
        // Zielposition des Königs.
        public override Position ToPos { get; }

        // Bewegungsrichtung des Königs.
        private readonly Direction kingMoveDir;
        // Startposition des Turms.
        private readonly Position rookFromPos;
        // Zielposition des Turms.
        private readonly Position rookToPos;

        // Konstruktor für einen Rochade-Zug.
        public Castle(MoveType type, Position kingPos)
        {
            Type = type;
            FromPos = kingPos;

            if (type == MoveType.CastleKS)
            {
                kingMoveDir = Direction.East;
                ToPos = new Position(kingPos.Row, 6);
                rookFromPos = new Position(kingPos.Row, 7);
                rookToPos = new Position(kingPos.Row, 5);
            }
            else if (type == MoveType.CastleQS)
            {
                kingMoveDir = Direction.West;
                ToPos = new Position(kingPos.Row, 2);
                rookFromPos = new Position(kingPos.Row, 0);
                rookToPos = new Position(kingPos.Row, 3);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(type), "Ungültiger Rochadetyp für Castle-Konstruktor.");
            }
        }

        // Führt die Rochade auf dem Brett aus.
        public override bool Execute(Board board)
        {
            new NormalMove(FromPos, ToPos).Execute(board);
            new NormalMove(rookFromPos, rookToPos).Execute(board);
            return false; // Rochade ist kein Schlag- oder Bauernzug.
        }

        // Prüft, ob die Rochade legal ist.
        public override bool IsLegal(Board board)
        {
            Piece? kingPiece = board[FromPos];
            if (kingPiece == null) return false;
            Player player = kingPiece.Color;
            if (board.IsInCheck(player))
            {
                return false; // Darf nicht aus dem Schach rochieren.
            }

            Board copy = board.Copy();
            Position kingPosInCopy = FromPos;
            for (int i = 0; i < 2; i++) // Prüft Felder, die der König überquert.
            {
                new NormalMove(kingPosInCopy, kingPosInCopy + kingMoveDir).Execute(copy);
                kingPosInCopy += kingMoveDir;

                if (copy.IsInCheck(player))
                {
                    return false; // Darf nicht durch Schach rochieren.
                }
            }
            return true; // Rochade ist legal, wenn auch das Zielfeld nicht bedroht ist.
        }
    }
}