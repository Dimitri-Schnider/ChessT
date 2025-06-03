using System;
using System.ComponentModel.DataAnnotations;
using ChessLogic;

namespace ChessNetwork.DTOs
{
    // DTO für die Übermittlung eines Zugs.
    public record MoveDto(
        // Startkoordinate des Zugs (z.B. "e2"). Erforderlich, Format a1-h8.
        [Required(ErrorMessage = "Startfeld (From) ist erforderlich.")]
        [RegularExpression("^[a-h][1-8]$", ErrorMessage = "From muss im Format a1–h8 sein.")]
        string From,

        // Zielkoordinate des Zugs (z.B. "e4"). Erforderlich, Format a1-h8.
        [Required(ErrorMessage = "Zielfeld (To) ist erforderlich.")]
        [RegularExpression("^[a-h][1-8]$", ErrorMessage = "To muss im Format a1–h8 sein.")]
        string To,

        // Eindeutige ID des ziehenden Spielers. Erforderlich.
        [Required(ErrorMessage = "PlayerId darf nicht leer sein.")]
        Guid PlayerId,

        // Optional: Figurentyp für Bauernumwandlung.
        PieceType? PromotionTo = null
    );
}