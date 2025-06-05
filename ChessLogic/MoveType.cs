namespace ChessLogic
{
    // Definiert die verschiedenen Arten von Schachzügen, die im Spiel vorkommen können.
    public enum MoveType
    {
        Normal,         // Ein normaler Figurenschritt oder Schlagzug.
        CastleKS,       // Kurze Rochade (Königsseite).
        CastleQS,       // Lange Rochade (Damenseite).
        DoublePawn,     // Ein Bauern-Doppelschritt von der Startposition.
        EnPassant,      // Ein En-Passant-Schlagzug.
        PawnPromotion,  // Eine Bauernumwandlung auf der letzten Reihe.
        Teleport,       // Spezialzug: Teleport einer Figur (typischerweise durch eine Karte).
        PositionSwap    // Spezialzug: Positionstausch zweier eigener Figuren (typischerweise durch eine Karte).
    }
}