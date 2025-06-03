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
        // Gibt an, ob der Zug gültig war.
        public bool IsValid { get; init; }
        // Fehlermeldung, falls Zug ungültig.
        public string? ErrorMessage { get; init; }
        // Neuer Brettzustand nach dem Zug.
        public required BoardDto NewBoard { get; init; }
        // Gibt an, ob der anfragende Spieler am Zug ist.
        public bool IsYourTurn { get; init; }
        // Spielstatus aus Sicht des ziehenden Spielers nach dem Zug.
        public GameStatusDto Status { get; init; }
        // ID des Spielers, der nach diesem Zug eine Karte ziehen darf.
        public Guid? PlayerIdToSignalCardDraw { get; init; }
        // NEU: Die tatsächlich gezogene Karte, falls eine durch den Zug gezogen wurde.
        public CardDto? NewlyDrawnCard { get; init; }


        // Startquadrat des letzten Zugs (für Hervorhebung)
        public string? LastMoveFrom { get; init; }
        // Zielquadrat des letzten Zugs (für Hervorhebung)
        public string? LastMoveTo { get; init; }
        // Quadrate, die durch einen Karteneffekt hervorgehoben werden sollen
        public List<AffectedSquareInfo>? CardEffectSquares { get; init; }
    }
}