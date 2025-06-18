using System.Globalization;

namespace ChessLogic.Utilities
{
    public static class PositionParser
    {
        // Parst eine Position in algebraischer Notation (z.B. "e4") in ein Positionsobjekt.
        public static Position ParsePos(string alg)
        {
            if (string.IsNullOrWhiteSpace(alg) || alg.Length != 2) throw new ArgumentException("Ungültiges algebraisches Format für Position.", nameof(alg));
            int col = alg[0] - 'a';
            if (!int.TryParse(alg[1].ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int rankValue)) throw new ArgumentException("Ungültiger Rang in algebraischer Notation.", nameof(alg));
            int row = 8 - rankValue;
            if (col < 0 || col > 7 || row < 0 || row > 7) throw new ArgumentException("Position ausserhalb des Schachbretts.", nameof(alg));
            return new Position(row, col);
        }
    }
}