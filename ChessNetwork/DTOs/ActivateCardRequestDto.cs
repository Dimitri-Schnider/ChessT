using ChessLogic;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ChessNetwork.DTOs
{
    // DTO für die Anfrage zur Aktivierung einer Karte.
    public record class ActivateCardRequestDto : IValidatableObject
    {
        private static readonly Regex _squareRegex = new("^[a-h][1-8]$", RegexOptions.Compiled);

        // Die eindeutige Instanz-ID der Karte, die aus der Hand des Spielers aktiviert wird.
        public Guid CardInstanceId { get; set; }

        // Die Typ-ID der Karte (z.B. "teleport"), die den Effekt bestimmt.
        public string CardTypeId { get; set; } = string.Empty;

        // Die Start-Koordinate für Karteneffekte (z.B. Teleport, Opfergabe).
        public string? FromSquare { get; set; }

        // Die Ziel-Koordinate für Karteneffekte (z.B. Teleport, Positionstausch).
        public string? ToSquare { get; set; }

        // Der Figurentyp, der für den "Wiedergeburt"-Effekt ausgewählt wurde.
        public PieceType? PieceTypeToRevive { get; set; }

        // Das Zielfeld auf dem Brett für den "Wiedergeburt"-Effekt.
        public string? TargetRevivalSquare { get; set; }

        // Die Instanz-ID der Karte, die beim "Kartentausch"-Effekt aus der eigenen Hand angeboten wird.
        public Guid? CardInstanceIdToSwapFromHand { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (CardInstanceId == Guid.Empty)
            {
                yield return new ValidationResult("CardInstanceId darf nicht leer sein.", new[] { nameof(CardInstanceId) });
            }
            if (string.IsNullOrEmpty(CardTypeId))
            {
                yield return new ValidationResult("CardTypeId ist erforderlich.", new[] { nameof(CardTypeId) });
            }
            if (!string.IsNullOrEmpty(FromSquare) && !_squareRegex.IsMatch(FromSquare))
            {
                yield return new ValidationResult("FromSquare muss im Format a1–h8 sein, falls angegeben.", new[] { nameof(FromSquare) });
            }
            if (!string.IsNullOrEmpty(ToSquare) && !_squareRegex.IsMatch(ToSquare))
            {
                yield return new ValidationResult("ToSquare muss im Format a1–h8 sein, falls angegeben.", new[] { nameof(ToSquare) });
            }
            if (!string.IsNullOrEmpty(TargetRevivalSquare) && !_squareRegex.IsMatch(TargetRevivalSquare))
            {
                yield return new ValidationResult("TargetRevivalSquare muss im Format a1–h8 sein, falls angegeben.", new[] { nameof(TargetRevivalSquare) });
            }
        }
    }
}