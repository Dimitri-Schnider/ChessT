using System;

namespace ChessNetwork.DTOs
{
    // DTO zur Repräsentation einer Spielkarte.
    public class CardDto
    {
        public Guid InstanceId { get; set; }                // Eindeutige ID dieser spezifischen Karteninstanz in der Hand eines Spielers 
        public required string Id { get; set; }             // Identifiziert den Kartentyp (z.B. "extrazug") und definiert dessen Effekt. 
        public required string Name { get; set; }           // Der Name der Karte, der dem Spieler angezeigt wird. 
        public required string Description { get; set; }    // Die Beschreibung des Karteneffekts. 
        public required string ImageUrl { get; set; }       // Die URL zum Bild der Karte. 
        public int AnimationDelayMs { get; set; } = 2500;   // Verzögerung in Millisekunden (Gegen Computer), bevor die Karte animiert wird.
    }
}