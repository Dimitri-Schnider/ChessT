namespace ChessLogic
{
    // Definiert verschiedene Arten von Schachzügen.
    public enum MoveType
    {
        Normal,         // Normaler Figurenschritt.
        CastleKS,       // Kurze Rochade.
        CastleQS,       // Lange Rochade.
        DoublePawn,     // Bauern-Doppelschritt.
        EnPassant,      // En-Passant-Schlag.
        PawnPromotion,  // Bauernumwandlung.
        Teleport,       // Teleport einer Figur (Spezialzug).
        PositionSwap    // Positionstausch zweier Figuren (Spezialzug).
    }
}