using System;

namespace ChessNetwork.DTOs
{
    // Datenübertragungsobjekt für eine Spielkarte.
    public class CardDto
    {
        // Eindeutige ID dieser spezifischen Karteninstanz.
        // Wird serverseitig beim Erstellen/Ziehen der Karte generiert.
        public Guid InstanceId { get; set; }

        // ID, die den KARTENTYP definiert (z.B. "extrazug", "teleport").
        // Kommt aus CardConstants.
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required string ImageUrl { get; set; }
    }
}