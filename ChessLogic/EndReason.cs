namespace ChessLogic
{
    // Definiert die verschiedenen Gründe, warum eine Schachpartie enden kann.
    public enum EndReason
    {
        Checkmate,              // Schachmatt: Der König ist im Schach und es gibt keinen legalen Zug.
        Stalemate,              // Patt: Der Spieler am Zug ist nicht im Schach, hat aber keinen legalen Zug.
        FiftyMoveRule,          // 50-Züge-Regel: 50 Züge wurden von beiden Spielern gemacht, ohne dass ein Bauer gezogen oder eine Figur geschlagen wurde.
        InsufficientMaterial,   // Unzureichendes Material: Es sind nicht genügend Figuren auf dem Brett, um ein Matt zu erzwingen.
        ThreefoldRepetition,    // Dreifache Stellungswiederholung: Dieselbe Stellung ist dreimal im Spiel aufgetreten, mit demselben Spieler am Zug und denselben Zugmöglichkeiten.
        TimeOut                 // Zeitüberschreitung: Ein Spieler hat seine Bedenkzeit überschritten.
    }
}