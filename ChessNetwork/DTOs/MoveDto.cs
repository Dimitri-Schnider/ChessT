using ChessLogic;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ChessNetwork.DTOs
{
    // DTO zur Übermittlung eines Zugs vom Client zum Server.
    public record MoveDto(
        // Startkoordinate des Zugs in algebraischer Notation (z.B. "e2").
        string From,

        // Zielkoordinate des Zugs in algebraischer Notation (z.B. "e4").
        string To,

        // Die eindeutige ID des Spielers, der den Zug ausführt.
        Guid PlayerId,

        // Der Figurentyp, zu dem ein Bauer umgewandelt werden soll (optional).
        PieceType? PromotionTo = null
    ) : IValidatableObject
    {
        private static readonly Regex _squareRegex = new("^[a-h][1-8]$", RegexOptions.Compiled);

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (PlayerId == Guid.Empty)
            {
                yield return new ValidationResult("PlayerId darf nicht leer sein.", new[] { nameof(PlayerId) });
            }

            if (string.IsNullOrEmpty(From) || !_squareRegex.IsMatch(From))
            {
                yield return new ValidationResult("From muss im Format a1–h8 sein.", new[] { nameof(From) });
            }

            if (string.IsNullOrEmpty(To) || !_squareRegex.IsMatch(To))
            {
                yield return new ValidationResult("To muss im Format a1–h8 sein.", new[] { nameof(To) });
            }
        }
    }
}