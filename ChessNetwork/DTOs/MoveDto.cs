using ChessLogic;
using System;
using System.ComponentModel.DataAnnotations;

namespace ChessNetwork.DTOs
{
    // DTO zur Übermittlung eines Zugs vom Client zum Server.
    public record MoveDto(
        // Startkoordinate des Zugs in algebraischer Notation (z.B. "e2").
        [Required(ErrorMessage = "Startfeld (From) ist erforderlich.")]
        [RegularExpression("^[a-h][1-8]$", ErrorMessage = "From muss im Format a1–h8 sein.")]
        string From,

        // Zielkoordinate des Zugs in algebraischer Notation (z.B. "e4").
        [Required(ErrorMessage = "Zielfeld (To) ist erforderlich.")]
        [RegularExpression("^[a-h][1-8]$", ErrorMessage = "To muss im Format a1–h8 sein.")]
        string To,

        // Die eindeutige ID des Spielers, der den Zug ausführt.
        [Required(ErrorMessage = "PlayerId darf nicht leer sein.")]
        Guid PlayerId,

        // Der Figurentyp, zu dem ein Bauer umgewandelt werden soll (optional).
        PieceType? PromotionTo = null
    );
}