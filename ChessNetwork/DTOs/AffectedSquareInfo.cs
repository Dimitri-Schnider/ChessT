namespace ChessNetwork.DTOs
{
    // Repräsentiert ein einzelnes Schachfeld, das durch einen Karteneffekt visuell hervorgehoben werden soll.
    public class AffectedSquareInfo
    {
        public string Square { get; set; } = string.Empty; // Die Koordinate des Feldes in algebraischer Notation (z.B. "e4").
        public string Type { get; set; } = string.Empty;   // Der Typ der Hervorhebung, der im Client einer CSS-Klasse zugeordnet wird.
    }
}