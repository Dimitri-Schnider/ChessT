using ChessLogic;
using System;
using System.ComponentModel.DataAnnotations;

namespace ChessNetwork.DTOs
{
    // DTO für die Anfrage zur Aktivierung einer Karte.
    // Enthält alle notwendigen Informationen, die der Server zur Verarbeitung des Karteneffekts benötigt.
    public record class ActivateCardRequestDto
    {
        // Die eindeutige Instanz-ID der Karte, die aus der Hand des Spielers aktiviert wird.
        [Required(ErrorMessage = "CardInstanceId ist erforderlich.")]
        public Guid CardInstanceId { get; set; }

        // Die Typ-ID der Karte (z.B. "teleport"), die den Effekt bestimmt.
        [Required(ErrorMessage = "CardTypeId ist erforderlich.")]
        public required string CardTypeId { get; set; }

        // Die Start-Koordinate für Karteneffekte (z.B. Teleport, Opfergabe).
        [RegularExpression("^[a-h][1-8]$", ErrorMessage = "FromSquare muss im Format a1–h8 sein, falls angegeben.")]
        public string? FromSquare { get; set; }

        // Die Ziel-Koordinate für Karteneffekte (z.B. Teleport, Positionstausch).
        [RegularExpression("^[a-h][1-8]$", ErrorMessage = "ToSquare muss im Format a1–h8 sein, falls angegeben.")]
        public string? ToSquare { get; set; }

        // Der Figurentyp, der für den "Wiedergeburt"-Effekt ausgewählt wurde.
        public PieceType? PieceTypeToRevive { get; set; }

        // Das Zielfeld auf dem Brett für den "Wiedergeburt"-Effekt.
        [RegularExpression("^[a-h][1-8]$", ErrorMessage = "TargetRevivalSquare muss im Format a1–h8 sein, falls angegeben.")]
        public string? TargetRevivalSquare { get; set; }

        // Die Instanz-ID der Karte, die beim "Kartentausch"-Effekt aus der eigenen Hand angeboten wird.
        public Guid? CardInstanceIdToSwapFromHand { get; set; }
    }
}