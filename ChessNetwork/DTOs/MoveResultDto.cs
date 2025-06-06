using System; 
using System.Collections.Generic;
using ChessNetwork.DTOs; 

namespace ChessNetwork.DTOs
{
    // Definiert mögliche Spielstatus nach einem Zug.
    public enum GameStatusDto
    {
        None,                     // Kein besonderer Status.
        Check,                    // Aktueller Spieler im Schach.
        Checkmate,                // Gegner schachmatt gesetzt.
        Stalemate,                // Patt.
        Draw50MoveRule,           // Remis durch 50-Züge-Regel.
        DrawInsufficientMaterial, // Remis durch unzureichendes Material.
        DrawThreefoldRepetition,  // Remis durch dreifache Stellungswiederholung.
        TimeOut                   // Spielende durch Zeitüberschreitung.
    }

    // DTO für das Ergebnis eines ausgeführten Zugs.
    public class MoveResultDto
    {
        public bool IsValid { get; init; }                                  // Gibt an, ob der Zug gültig war.
        public string? ErrorMessage { get; init; }                          // Fehlermeldung, falls Zug ungültig.
        public required BoardDto NewBoard { get; init; }                    // Neuer Brettzustand nach dem Zug.
        public bool IsYourTurn { get; init; }                               // Gibt an, ob der anfragende Spieler am Zug ist.
        public GameStatusDto Status { get; init; }                          // Spielstatus aus Sicht des ziehenden Spielers.
        public Guid? PlayerIdToSignalCardDraw { get; init; }                // ID des Spielers, der eine Karte ziehen darf.
        public CardDto? NewlyDrawnCard { get; init; }                       // Die gezogene Karte, falls eine durch den Zug verdient wurde.
        public string? LastMoveFrom { get; init; }                          // Startquadrat des letzten Zugs (für Hervorhebung).
        public string? LastMoveTo { get; init; }                            // Zielquadrat des letzten Zugs (für Hervorhebung).
        public List<AffectedSquareInfo>? CardEffectSquares { get; init; }   // Quadrate, die durch einen Karteneffekt hervorgehoben werden sollen.
    }
}