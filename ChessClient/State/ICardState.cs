using ChessNetwork.DTOs;
using ChessNetwork;
using ChessClient.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChessLogic;

namespace ChessClient.State
{
    // Definiert den Vertrag für den State-Container, der alle Zustände rund um die Spielkarten verwaltet.
    public interface ICardState
    {
        event Action? StateChanged;                                             // Wird ausgelöst, wenn sich der Zustand der Karten ändert, um die UI zu benachrichtigen.
        CardDto? SelectedCardForInfoPanel { get; }                              // Die Karte, die aktuell im Info-Panel zur Ansicht oder Aktivierung ausgewählt ist.
        bool IsCardActivationPending { get; }                                   // Gibt an, ob gerade eine Kartenaktivierung läuft und auf eine Benutzerinteraktion (z.B. Feldauswahl) gewartet wird.
        List<CardDto> PlayerHandCards { get; }                                  // Die Liste der Karten, die der Spieler aktuell auf der Hand hat.
        int MyDrawPileCount { get; }                                            // Die Anzahl der verbleibenden Karten im Nachziehstapel des Spielers.
        List<PlayedCardInfo> MyPlayedCardsForHistory { get; }                   // Die Historie der vom eigenen Spieler gespielten Karten.
        List<PlayedCardInfo> OpponentPlayedCardsForHistory { get; }             // Die Historie der vom Gegner gespielten Karten.
        bool IsPreviewingPlayedCard { get; }                                    // Gibt an, ob eine Karte aus der Historie nur zur Vorschau angezeigt wird.
        Guid? SelectedCardInstanceIdInHand { get; }                             // Die Instanz-ID der aktuell in der Hand ausgewählten Karte.
        bool AreCardsRevealed { get; }                                          // Gibt an, ob die Karten aufgedeckt sind (nach dem Spielstart-Countdown).


        // Properties für den mehrstufigen Aktivierungsprozess von Karten.
        CardDto? ActiveCardForBoardSelection { get; }                           // Die Karte, für die gerade eine Brettinteraktion (z.B. Feldauswahl) erforderlich ist.
        bool IsAwaitingRebirthTargetSquareSelection { get; }                    // Gibt an, ob auf die Auswahl eines Zielfeldes für die Wiedergeburt gewartet wird.
        string? FirstSquareSelectedForTeleportOrSwap { get; }                   // Speichert das erste ausgewählte Feld für Karten wie Teleport oder Positionstausch.
        bool IsAwaitingSacrificePawnSelection { get; }                          // Gibt an, ob auf die Auswahl eines Bauern für die Opfergabe gewartet wird.
        PieceType? PieceTypeSelectedForRebirth { get; }                         // Der Figurentyp, der für die Wiedergeburt ausgewählt wurde.
        bool IsAwaitingTurnConfirmation { get; }                                // Gibt an, ob der Client auf die Bestätigung des Servers wartet, dass ein Zug abgeschlossen ist.
        CardDto? GetCardDefinitionById(string cardTypeId);                      // Gibt die Basis-Definition einer Karte anhand ihrer Typ-ID zurück.
        void SetInitialHand(InitialHandDto initialHandDto);                     // Initialisiert den Kartenzustand für ein neues Spiel.
        void AddReceivedCardToHand(CardDto drawnCard, int newDrawPileCount);    // Fügt eine einzelne, neu gezogene Karte der Hand hinzu.
        void HandleCardPlayedByMe(Guid cardInstanceId, string cardTypeId);      // Entfernt eine gespielte Karte aus der Handliste des Spielers.
        void SelectCardForInfoPanel(CardDto? card, bool isPreview);             // Öffnet das Info-Panel für eine ausgewählte Karte.

        Task SetSelectedHandCardAsync(CardDto card, IGameCoreState gameCoreState, IUiState uiState);    // Wählt eine Karte aus der Hand aus, um sie potenziell zu aktivieren.
        void SetIsCardActivationPending(bool isPending);                        // Setzt manuell den Zustand, dass eine Kartenaktivierung läuft.
        void AddToMyPlayedHistory(PlayedCardInfo cardInfo);                     // Fügt eine gespielte Karte zur eigenen sichtbaren Historie hinzu.
        void AddToOpponentPlayedHistory(PlayedCardInfo cardInfo);               // Fügt eine gespielte Karte zur gegnerischen sichtbaren Historie hinzu.
        void ClearPlayedCardsHistory();                                         // Leert die Historie der gespielten Karten.
        void ClearSelectedCardForInfoPanel();                                   // Hebt die Auswahl einer Karte im Info-Panel auf.
        void DeselectActiveHandCard();                                          // Hebt die Auswahl einer Handkarte auf.
        void RevealCards();                                                     // Deckt die Karten visuell auf, typischerweise nach dem Spielstart-Countdown.


        List<CapturedPieceTypeDto>? CapturedPiecesForRebirth { get; }           // Eine Liste der geschlagenen Figuren, die für die "Wiedergeburt"-Karte relevant sind.
        Task LoadCapturedPiecesForRebirthAsync(Guid gameId, Guid playerId, IGameSession gameSession);   // Lädt die geschlagenen Figuren vom Server, die für die "Wiedergeburt"-Karte zur Auswahl stehen.
        void ClearCapturedPiecesForRebirth();                                   // Leert die Liste der geschlagenen Figuren.
        void UpdateHandAndDrawPile(InitialHandDto newHandInfo);                 // Aktualisiert die gesamte Hand und die Anzahl der Karten im Nachziehstapel.


        // Methoden zur Steuerung des Zustandsautomaten für die Kartenaktivierung.
        void StartCardActivation(CardDto card);                                 // Startet den Prozess der Kartenaktivierung.
        void ResetCardActivationState(bool fromCancel, string? messageToKeep = null);   // Setzt den gesamten Kartenaktivierungs-Zustand zurück.
        void SetAwaitingRebirthTargetSquareSelection(PieceType pieceType);      // Setzt den Zustand, dass auf die Auswahl des Zielfeldes für die Wiedergeburt gewartet wird.
        void SetFirstSquareForTeleportOrSwap(string square);                    // Speichert das erste ausgewählte Feld für eine Zwei-Klick-Aktion.
        void SetAwaitingSacrificePawnSelection(bool isAwaiting);                // Setzt das Flag, dass auf die Auswahl eines Bauern für die Opfergabe gewartet wird.
        void SetAwaitingTurnConfirmation(bool isAwaiting);                      // Setzt das Flag, dass der Client auf die Bestätigung des Servers wartet, dass der Zug vorbei ist.

    }
}