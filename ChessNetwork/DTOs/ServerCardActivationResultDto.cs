using System;
using System.Collections.Generic;

namespace ChessNetwork.DTOs
{
    // DTO, das das detaillierte Ergebnis einer Kartenaktivierung vom Server meldet.
    public class ServerCardActivationResultDto
    {
        public bool Success { get; set; }                                       // Gibt an, ob die Aktivierung erfolgreich war.
        public string? ErrorMessage { get; set; }                               // Fehlermeldung, falls die Aktivierung fehlschlug.
        public required string CardId { get; set; }                             // Die ID der aktivierten Karte.
        public List<AffectedSquareInfo>? AffectedSquaresByCard { get; set; }    // Liste der Felder, die durch den Effekt visuell hervorgehoben werden sollen.
        public bool EndsPlayerTurn { get; set; } = true;                        // Gibt an, ob der Zug des Spielers nach dem Effekt beendet ist.
        public bool BoardUpdatedByCardEffect { get; set; }                      // Gibt an, ob der Effekt das Brett verändert hat.
        public Guid? PlayerIdToSignalCardDraw { get; set; }                     // Die ID des Spielers, der als Folge des Effekts eine Karte ziehen darf.
        public CardDto? NewlyDrawnCard { get; set; }                            // Die neu gezogene Karte, falls zutreffend.
        public CardDto? CardGivenByPlayerForSwap { get; set; }                  // Die Karte, die beim Tausch abgegeben wurde.
        public CardDto? CardReceivedByPlayerForSwap { get; set; }               // Die Karte, die beim Tausch erhalten wurde.
        public PositionDto? PawnPromotionPendingAt { get; set; }                // Die Position, an der nach dem Effekt eine Bauernumwandlung ansteht.
    }
}