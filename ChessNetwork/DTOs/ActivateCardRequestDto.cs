// File: [SolutionDir]/ChessNetwork/DTOs/ActivateCardRequestDto.cs
using System;
using System.ComponentModel.DataAnnotations;
using ChessLogic;

namespace ChessNetwork.DTOs
{
    public record class ActivateCardRequestDto
    {
        [Required(ErrorMessage = "CardInstanceId ist erforderlich.")]
        public Guid CardInstanceId { get; set; }

        [Required(ErrorMessage = "CardTypeId ist erforderlich.")]
        public required string CardTypeId { get; set; }

        // Validierung bleibt für Karten wie Teleport, Positionstausch wichtig.
        // Für Wiedergeburt werden diese Felder null sein und die Validierung greift nicht.
        [RegularExpression("^[a-h][1-8]$", ErrorMessage = "FromSquare muss im Format a1–h8 sein, falls angegeben.")]
        public string? FromSquare { get; set; }

        [RegularExpression("^[a-h][1-8]$", ErrorMessage = "ToSquare muss im Format a1–h8 sein, falls angegeben.")]
        public string? ToSquare { get; set; }

        // Diese Felder werden jetzt für Wiedergeburt verwendet.
        public PieceType? PieceTypeToRevive { get; set; }

        [RegularExpression("^[a-h][1-8]$", ErrorMessage = "TargetRevivalSquare muss im Format a1–h8 sein, falls angegeben.")]
        public string? TargetRevivalSquare { get; set; }

        public Guid? CardInstanceIdToSwapFromHand { get; set; }
    }
}