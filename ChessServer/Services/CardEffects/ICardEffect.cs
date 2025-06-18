using ChessLogic;
using ChessNetwork.DTOs;
using System;
using System.Collections.Generic;

namespace ChessServer.Services.CardEffects
{
    // Definiert das Ergebnis einer Kartenaktivierung, das vom Server an den Client zurückgegeben wird.
    public record CardActivationResult(
        bool Success,                                           // Gibt an, ob die Aktivierung erfolgreich war.
        string? ErrorMessage = null,                            // Fehlermeldung, falls die Aktivierung fehlschlug.
        bool EndsPlayerTurn = true,                             // Gibt an, ob der Zug des Spielers nach dem Effekt beendet ist (Standard: ja).
        Guid? PlayerIdToSignalDraw = null,                      // Die ID des Spielers, der als Folge des Effekts eine Karte ziehen darf.
        bool BoardUpdatedByCardEffect = false,                  // Gibt an, ob der Effekt das Brett direkt verändert hat.
        List<AffectedSquareInfo>? AffectedSquaresByCard = null, // Liste der Felder, die durch den Effekt visuell hervorgehoben werden sollen.
        CardDto? CardGivenByPlayerForSwapEffect = null,         // Die Karte, die beim Tausch abgegeben wurde (nur für CardSwap).
        CardDto? CardReceivedByPlayerForSwapEffect = null       // Die Karte, die beim Tausch erhalten wurde (nur für CardSwap).
    );

    // Definiert den Vertrag für alle Karteneffekt-Implementierungen.
    public interface ICardEffect
    {
        // Führt den spezifischen Karteneffekt aus und gibt das Ergebnis zurück.
        CardActivationResult Execute(
            GameSession session,            // `session`: Die aktuelle Spielsitzung, die den gesamten Kontext (Brett, Spieler, Timer) bereitstellt.
            Guid playerId,                  // `playerId`: Die ID des Spielers, der die Karte aktiviert.
            Player playerDataColor,         // `playerDataColor`: Die Farbe des aktivierenden Spielers.
            IHistoryManager historyManager, // `historyManager`: Der Manager für den Spielverlauf, um Aktionen zu protokollieren.
            string cardTypeId,              // `cardTypeId`: Die Typ-ID der aktivierten Karte (z.B. "teleport").
            string? fromSquareAlg,          // `fromSquareAlg`: Optionale Start-Koordinate für den Effekt (z.B. bei Teleport, Opfergabe).
            string? toSquareAlg             // `toSquareAlg`: Optionale Ziel-Koordinate für den Effekt (z.B. bei Teleport, Positionstausch).
         );
    }
}