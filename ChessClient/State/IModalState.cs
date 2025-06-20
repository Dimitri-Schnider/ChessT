using System;
using ChessLogic;
using ChessNetwork.DTOs;
using System.Collections.Generic;
using ChessClient.Models;

namespace ChessClient.State
{
    // Definiert den Vertrag für den State-Container, der alle Modal-Dialoge verwaltet.
    public interface IModalState
    {
        event Action? StateChanged;                                         // Event zur Benachrichtigung über Zustandsänderungen.
        bool ShowCreateGameModal { get; }                                   // Zeigt das "Spiel erstellen"-Modal.
        bool ShowJoinGameModal { get; }                                     // Zeigt das "Spiel beitreten"-Modal.
        bool ShowInviteLinkModal { get; }                                   // Zeigt das "Einladungslink"-Modal.
        bool ShowPieceSelectionModal { get; }                               // Zeigt das "Figurenauswahl"-Modal (für Umwandlung oder Wiedergeburt).
        bool ShowPawnPromotionModalSpecifically { get; }                    // Gibt an, ob das Figurenauswahl-Modal speziell für eine Bauernumwandlung offen ist.
        bool ShowCardInfoPanelModal { get; }                                // Zeigt das "Karten-Info"-Panel.
        bool ShowErrorModal { get; }                                        // Zeigt das "Fehler"-Modal.
        string GameIdInputForJoinModal { get; }                             // Die Spiel-ID im "Spiel beitreten"-Modal.
        string InviteLink { get; }                                          // Der zu teilende Einladungslink.
        string ErrorModalMessage { get; }                                   // Die im Fehler-Modal angezeigte Nachricht.
        string PieceSelectionModalTitle { get; }                            // Der Titel des Figurenauswahl-Modals.
        string PieceSelectionModalPrompt { get; }                           // Die Aufforderung im Figurenauswahl-Modal.
        List<PieceSelectionChoiceInfo>? PieceSelectionModalChoices { get; } // Die Liste der wählbaren Figuren.
        Player PieceSelectionModalPlayerColor { get; }                      // Die Farbe der Figuren zur Auswahl.
        bool PieceSelectionModalShowCancelButton { get; }                   // Gibt an, ob der Abbrechen-Button angezeigt wird.
        MoveDto? PendingPromotionMove { get; }                              // Speichert den Zug, der auf eine Bauernumwandlung wartet.
        CardDto? CardForInfoPanelModal { get; }                             // Die im Info-Panel angezeigte Karte.
        bool IsCardInInfoPanelModalActivatable { get; }                     // Gibt an, ob die Karte im Info-Panel aktivierbar ist.
        bool IsCardInInfoPanelModalPreviewOnly { get; }                     // Gibt an, ob die Karte nur als Vorschau dient.

        // Methoden zur Steuerung der Modals
        void OpenErrorModal(string message, bool closeOtherModals = true); // Öffnet das Fehler-Modal.
        void CloseErrorModal(); // Schliesst das Fehler-Modal.
        void OpenCreateGameModal(); // Öffnet das "Spiel erstellen"-Modal.
        void CloseCreateGameModal(); // Schliesst das "Spiel erstellen"-Modal.
        void UpdateCreateGameModalArgs(string name, Player color, int timeMinutes); // Aktualisiert die Argumente des "Spiel erstellen"-Modals.
        void OpenJoinGameModal(string? initialGameId = null); // Öffnet das "Spiel beitreten"-Modal.
        void CloseJoinGameModal(); // Schliesst das "Spiel beitreten"-Modal.
        void UpdateJoinGameModalArgs(string name, string gameId); // Aktualisiert die Argumente des "Spiel beitreten"-Modals.
        void OpenInviteLinkModal(string inviteLink); // Öffnet das Einladungs-Modal.
        void CloseInviteLinkModal(); // Schliesst das Einladungs-Modal.
        void OpenPieceSelectionModal(string title, string prompt, List<PieceSelectionChoiceInfo> choices, Player playerColor, bool showCancelButton = true); // Öffnet das Figurenauswahl-Modal.
        void ClosePieceSelectionModal(); // Schliesst das Figurenauswahl-Modal.
        void OpenPawnPromotionModal(MoveDto pendingMove, Player myColor); // Öffnet das Modal speziell für eine Bauernumwandlung.
        void ClosePawnPromotionModal(); // Schliesst das Bauernumwandlungs-Modal.
        void ClearPendingPromotionMove(); // Leert den zwischengespeicherten Umwandlungszug.
        void OpenCardInfoPanelModal(CardDto card, bool isActivatable, bool isPreviewOnly); // Öffnet das Karten-Info-Panel.
        void CloseCardInfoPanelModal(); // Schliesst das Karten-Info-Panel.
    }
}