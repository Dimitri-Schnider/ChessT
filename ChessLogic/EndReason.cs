namespace ChessLogic
{
    // Definiert Gründe für das Spielende.
    public enum EndReason
    {
        Checkmate,              // Schachmatt.
        Stalemate,              // Patt.
        FiftyMoveRule,          // 50-Züge-Regel.
        InsufficientMaterial,   // Unzureichendes Material.
        ThreefoldRepetition,    // Dreifache Stellungswiederholung.
        TimeOut                 // Zeitüberschreitung.
    }
}