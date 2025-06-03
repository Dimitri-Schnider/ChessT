using System;
using System.Collections.Generic;

namespace ChessLogic
{
    // Zählt Figuren auf dem Brett.
    public class Counting
    {
        // Zähler für weisse Figuren.
        private readonly Dictionary<PieceType, int> whiteCount = new();
        // Zähler für schwarze Figuren.
        private readonly Dictionary<PieceType, int> blackCount = new();
        // Gesamtzahl aller Figuren.
        public int TotalCount { get; private set; }

        // Initialisiert Zähler für alle Figurentypen.
        public Counting()
        {
            foreach (PieceType type in Enum.GetValues<PieceType>())
            {
                whiteCount[type] = 0;
                blackCount[type] = 0;
            }
        }

        // Erhöht den Zähler für eine gegebene Figur.
        public void Increment(Player color, PieceType type)
        {
            if (color == Player.White)
            {
                whiteCount[type]++;
            }
            else if (color == Player.Black)
            {
                blackCount[type]++;
            }
            TotalCount++;
        }

        // Gibt Anzahl weisser Figuren eines Typs zurück.
        public int White(PieceType type)
        {
            return whiteCount[type];
        }

        // Gibt Anzahl schwarzer Figuren eines Typs zurück.
        public int Black(PieceType type)
        {
            return blackCount[type];
        }
    }
}