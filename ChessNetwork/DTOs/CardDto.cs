using System;

namespace ChessNetwork.DTOs
{
    // DTO zur Repräsentation einer Spielkarte.
    public class CardDto
    {
        public Guid InstanceId { get; set; }                    // Eindeutige ID dieser spezifischen Karteninstanz in der Hand eines Spielers 
        public string Id { get; set; } = string.Empty;          // Identifiziert den Kartentyp (z.B. "extrazug") und definiert dessen Effekt.
        public string Name { get; set; } = string.Empty;        // Der Name der Karte, der dem Spieler angezeigt wird.
        public string Description { get; set; } = string.Empty; // Die Beschreibung des Karteneffekts.
        public string ImageUrl { get; set; } = string.Empty;    // Die URL zum Bild der Karte.
        public int AnimationDelayMs { get; set; } = 2500;       // Verzögerung in Millisekunden (Gegen Computer), bevor die Karte animiert wird.
    }
}