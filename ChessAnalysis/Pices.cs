using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessAnalysis.Models
{
    public enum PieceType
    {
        King, Queen, Rook, Bishop, Knight, Pawn
    }

    public enum PieceColor
    {
        White, Black
    }

    public class Pieces
    {
        public PieceType Type { get; set; }
        public PieceColor Color { get; set; }

        /// <summary>
        /// 0..7 für die Spalten a=0, b=1, …, h=7
        /// </summary>
        public int File { get; set; }

        /// <summary>
        /// 0..7 für die Reihen 1=0, 2=1, …, 8=7 (interner Index)
        /// </summary>
        public int Rank { get; set; }
    }
}
