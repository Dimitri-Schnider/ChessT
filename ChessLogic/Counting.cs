using System;
using System.Collections.Generic;

namespace ChessLogic
{
    // Hilfsklasse zum Zählen der verschiedenen Figurentypen für jeden Spieler.
    public class Counting
    {
        private readonly Dictionary<PieceType, int> whiteCount = new(); // Speichert die Anzahl der weissen Figuren pro Typ.
        private readonly Dictionary<PieceType, int> blackCount = new(); // Speichert die Anzahl der schwarzen Figuren pro Typ.

        // Die Gesamtzahl aller Figuren auf dem Brett.
        public int TotalCount { get; private set; }        

        // Initialisiert alle Zähler für jeden Figurentyp mit 0.
        public Counting()
        {
            foreach (PieceType type in Enum.GetValues<PieceType>())
            {
                whiteCount[type] = 0;
                blackCount[type] = 0;
            }
        }

        // Erhöht den Zähler für die angegebene Figur und die Gesamtzahl.
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

        // Gibt die Anzahl der weissen Figuren des angegebenen Typs zurück.
        public int White(PieceType type)
        {
            return whiteCount[type];
        }

        // Gibt die Anzahl der schwarzen Figuren des angegebenen Typs zurück.
        public int Black(PieceType type)
        {
            return blackCount[type];
        }
    }
}