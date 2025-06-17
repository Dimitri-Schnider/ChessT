using ChessClient.Models;
using ChessNetwork.Configuration;
using ChessLogic;
using ChessNetwork;
using ChessNetwork.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChessClient.State
{
    // Verwaltet den gesamten Zustand, der mit den Spielkarten zusammenhängt: Handkarten, Stapel,
    // gespielte Karten und den komplexen Lebenszyklus einer Kartenaktivierung.
    public class CardState : ICardState
    {
        // Event, das die UI über Änderungen an diesem State informiert.
        public event Action? StateChanged;
        // Löst das StateChanged-Event sicher aus, um UI-Updates anzustossen.
        protected virtual void OnStateChanged() => StateChanged?.Invoke();

        // --- Injizierte Abhängigkeiten zu anderen State-Containern ---
        private readonly IModalState _modalState;
        private readonly IUiState _uiState;
        private readonly IHighlightState _highlightState;

        // Die aktuell im Info-Panel angezeigte Karte.
        public CardDto? SelectedCardForInfoPanel { get; private set; }
        // Gibt an, ob gerade eine Kartenaktivierung läuft (z.B. auf eine Feldauswahl gewartet wird).
        public bool IsCardActivationPending { get; private set; }
        // Die Liste der Karten, die der Spieler aktuell auf der Hand hat.
        public List<CardDto> PlayerHandCards { get; private set; } = [];
        // Die Anzahl der verbleibenden Karten im Nachziehstapel des Spielers.
        public int MyDrawPileCount { get; private set; }
        // Gibt an, ob die Karten aufgedeckt sind (nach dem Spielstart-Countdown).
        public bool AreCardsRevealed { get; private set; }
        // Die Historie der vom eigenen Spieler gespielten Karten.
        public List<PlayedCardInfo> MyPlayedCardsForHistory { get; private set; } = [];
        // Die Historie der vom Gegner gespielten Karten.
        public List<PlayedCardInfo> OpponentPlayedCardsForHistory { get; private set; } = [];
        // Gibt an, ob eine Karte aus der Historie nur zur Vorschau angezeigt wird.
        public bool IsPreviewingPlayedCard { get; private set; }
        // Die Instanz-ID der aktuell in der Hand ausgewählten Karte.
        public Guid? SelectedCardInstanceIdInHand { get; private set; }
        // Eine Liste der geschlagenen Figuren, die für die "Wiedergeburt"-Karte relevant sind.
        public List<CapturedPieceTypeDto>? CapturedPiecesForRebirth { get; private set; }

        // Die Karte, für die gerade eine Brettinteraktion (z.B. Feldauswahl) erforderlich ist.
        public CardDto? ActiveCardForBoardSelection { get; private set; }
        // Gibt an, ob auf die Auswahl eines Zielfeldes für die Wiedergeburt gewartet wird.
        public bool IsAwaitingRebirthTargetSquareSelection { get; private set; }
        // Speichert das erste ausgewählte Feld für Karten wie Teleport oder Positionstausch.
        public string? FirstSquareSelectedForTeleportOrSwap { get; private set; }
        // Gibt an, ob auf die Auswahl eines Bauern für die Opfergabe gewartet wird.
        public bool IsAwaitingSacrificePawnSelection { get; private set; }
        // Der Figurentyp, der für die Wiedergeburt ausgewählt wurde.
        public PieceType? PieceTypeSelectedForRebirth { get; private set; }
        // Gibt an, ob der Client auf die Bestätigung des Servers wartet, dass ein Zug abgeschlossen ist.
        public bool IsAwaitingTurnConfirmation { get; private set; }

        // Konstruktor, der die Abhängigkeiten zu anderen State-Managern injiziert.
        public CardState(IModalState modalState, IUiState uiState, IHighlightState highlightState)
        {
            _modalState = modalState;
            _uiState = uiState;
            _highlightState = highlightState;
        }

        // Startet den Prozess der Kartenaktivierung. Setzt alle relevanten Flags in einen sauberen Anfangszustand.
        public void StartCardActivation(CardDto card)
        {
            IsCardActivationPending = true;
            ActiveCardForBoardSelection = card;
            IsAwaitingRebirthTargetSquareSelection = false;
            FirstSquareSelectedForTeleportOrSwap = null;
            IsAwaitingSacrificePawnSelection = false;
            PieceTypeSelectedForRebirth = null;
            IsAwaitingTurnConfirmation = false;
            _highlightState.ClearAllActionHighlights();
            OnStateChanged();
        }

        // Setzt den gesamten Kartenaktivierungs-Zustand zurück. Dies ist die zentrale Aufräum-Methode.
        public void ResetCardActivationState(bool fromCancel, string? messageToKeep = null)
        {
            var previouslyActiveCard = ActiveCardForBoardSelection;

            IsCardActivationPending = false;
            DeselectActiveHandCard();
            ActiveCardForBoardSelection = null;

            // Schliesst alle relevanten Modals und löscht Highlights.
            _modalState.ClosePieceSelectionModal();
            _modalState.CloseCardInfoPanelModal();
            _highlightState.ClearAllActionHighlights();
            _highlightState.ClearCardTargetSquaresForSelection();

            // Setzt alle spezifischen Auswahl-Zustände zurück.
            PieceTypeSelectedForRebirth = null;
            IsAwaitingRebirthTargetSquareSelection = false;
            FirstSquareSelectedForTeleportOrSwap = null;
            IsAwaitingSacrificePawnSelection = false;

            // Setzt das Flag für die Zug-Bestätigung nur zurück, wenn die Aktion nicht durch den User abgebrochen wurde.
            if (!fromCancel)
            {
                IsAwaitingTurnConfirmation = false;
            }

            // Zeigt eine kontextabhängige Nachricht in der Info-Box an.
            if (!string.IsNullOrEmpty(messageToKeep))
            {
                _ = _uiState.SetCurrentInfoMessageForBoxAsync(messageToKeep, true, 4000);
            }
            else if (fromCancel && previouslyActiveCard != null)
            {
                _ = _uiState.SetCurrentInfoMessageForBoxAsync($"Aktivierung von '{previouslyActiveCard.Name}' abgebrochen.", true, 4000);
            }

            OnStateChanged();
        }

        // Setzt den Zustand, dass auf die Auswahl des Zielfeldes für die Wiedergeburt gewartet wird.
        public void SetAwaitingRebirthTargetSquareSelection(PieceType pieceType)
        {
            PieceTypeSelectedForRebirth = pieceType;
            IsAwaitingRebirthTargetSquareSelection = true;
            OnStateChanged();
        }

        // Speichert das erste ausgewählte Feld für eine Zwei-Klick-Aktion (z.B. Teleport).
        public void SetFirstSquareForTeleportOrSwap(string square)
        {
            FirstSquareSelectedForTeleportOrSwap = square;
            OnStateChanged();
        }

        // Setzt das Flag, dass auf die Auswahl eines Bauern für die Opfergabe gewartet wird.
        public void SetAwaitingSacrificePawnSelection(bool isAwaiting)
        {
            IsAwaitingSacrificePawnSelection = isAwaiting;
            OnStateChanged();
        }

        // Setzt das Flag, dass der Client auf die Bestätigung des Servers wartet, dass der Zug vorbei ist.
        public void SetAwaitingTurnConfirmation(bool isAwaiting)
        {
            if (IsAwaitingTurnConfirmation == isAwaiting) return;
            IsAwaitingTurnConfirmation = isAwaiting;
            OnStateChanged();
        }

        // Gibt die Basis-Definition einer Karte anhand ihrer Typ-ID zurück.
        // Anmerkung: Potenziell veraltet, da der Client meist volle CardDto-Objekte vom Server erhält.
        public CardDto? GetCardDefinitionById(string cardTypeId)
        {
            // Diese Methode ist aktuell leer, da sie nicht mehr benötigt zu werden scheint.
            // Sie bleibt aber erhalten, um Breaking Changes zu vermeiden.
            return null;
        }

        // Aktualisiert die gesamte Hand und die Anzahl der Karten im Nachziehstapel (z.B. nach einem Kartentausch).
        public void UpdateHandAndDrawPile(InitialHandDto newHandInfo)
        {
            PlayerHandCards.Clear();
            if (newHandInfo?.Hand != null) PlayerHandCards.AddRange(newHandInfo.Hand);
            MyDrawPileCount = newHandInfo?.DrawPileCount ?? 0;

            // Hebt die Auswahl auf, falls die ausgewählte Karte nicht mehr in der Hand ist.
            if (SelectedCardInstanceIdInHand.HasValue && !PlayerHandCards.Any(c => c.InstanceId == SelectedCardInstanceIdInHand.Value))
            {
                SelectedCardInstanceIdInHand = null;
            }
            OnStateChanged();
        }

        // Initialisiert den gesamten Karten-Zustand für ein neues Spiel.
        public void SetInitialHand(InitialHandDto initialHandDto)
        {
            PlayerHandCards.Clear();
            if (initialHandDto?.Hand != null) PlayerHandCards.AddRange(initialHandDto.Hand);
            MyDrawPileCount = initialHandDto?.DrawPileCount ?? 0;
            MyPlayedCardsForHistory.Clear();
            OpponentPlayedCardsForHistory.Clear();
            ResetCardActivationState(false);
            AreCardsRevealed = false;
            ClearCapturedPiecesForRebirth();
            OnStateChanged();
        }

        // Deckt die Karten visuell auf, typischerweise nach dem Spielstart-Countdown.
        public void RevealCards()
        {
            if (!AreCardsRevealed)
            {
                AreCardsRevealed = true;
                OnStateChanged();
            }
        }

        // Fügt eine einzelne, neu gezogene Karte der Hand hinzu und aktualisiert die Stapelgrösse.
        public void AddReceivedCardToHand(CardDto drawnCard, int newDrawPileCount)
        {
            // Verhindert das Hinzufügen von Karten mit derselben Instanz-ID (Duplikate).
            if (PlayerHandCards.Any(card => card.InstanceId == drawnCard.InstanceId))
            {
                return;
            }
            // "Spezialkarten" wie "Keine Karten mehr" werden nicht physisch zur Hand hinzugefügt.
            if (!drawnCard.Name.Contains(CardConstants.NoMoreCardsName) && !drawnCard.Name.Contains(CardConstants.ReplacementCardName))
            {
                PlayerHandCards.Add(drawnCard);
            }
            MyDrawPileCount = newDrawPileCount;
            OnStateChanged();
        }

        // Öffnet das Info-Panel für eine ausgewählte Karte, entweder zur Aktivierung oder nur zur Vorschau.
        public void SelectCardForInfoPanel(CardDto? card, bool isPreview)
        {
            if (card != null)
            {
                _modalState.OpenCardInfoPanelModal(card, !isPreview, isPreview);
            }
        }

        // Wählt eine Karte aus der Hand aus, um sie potenziell zu aktivieren (öffnet das Info-Panel).
        public async Task SetSelectedHandCardAsync(CardDto card, IGameCoreState gameCoreState, IUiState uiState)
        {
            if (gameCoreState.MyColor == gameCoreState.CurrentTurnPlayer && !IsCardActivationPending)
            {
                SelectedCardInstanceIdInHand = card.InstanceId;
                _modalState.OpenCardInfoPanelModal(card, true, false);
                await uiState.SetCurrentInfoMessageForBoxAsync($"Karte '{card.Name}' ausgewählt.");
            }
        }

        // Setzt manuell den Zustand, dass eine Kartenaktivierung läuft.
        public void SetIsCardActivationPending(bool isPending)
        {
            if (IsCardActivationPending == isPending) return;
            IsCardActivationPending = isPending;
            OnStateChanged();
        }

        // Entfernt eine gespielte Karte aus der Handliste des Spielers.
        public void HandleCardPlayedByMe(Guid cardInstanceId, string cardTypeId)
        {
            PlayerHandCards.RemoveAll(c => c.InstanceId == cardInstanceId);
            OnStateChanged();
        }

        // Fügt eine gespielte Karte zur eigenen sichtbaren Historie hinzu.
        public void AddToMyPlayedHistory(PlayedCardInfo cardInfo)
        {
            MyPlayedCardsForHistory.Add(cardInfo);
            OnStateChanged();
        }

        // Fügt eine gespielte Karte zur gegnerischen sichtbaren Historie hinzu.
        public void AddToOpponentPlayedHistory(PlayedCardInfo cardInfo)
        {
            OpponentPlayedCardsForHistory.Add(cardInfo);
            OnStateChanged();
        }

        // Leert die Historie der gespielten Karten, z.B. bei einem neuen Spiel.
        public void ClearPlayedCardsHistory()
        {
            MyPlayedCardsForHistory.Clear();
            OpponentPlayedCardsForHistory.Clear();
            OnStateChanged();
        }

        // Hebt die Auswahl einer Karte im Info-Panel auf.
        public void ClearSelectedCardForInfoPanel()
        {
            SelectedCardInstanceIdInHand = null;
            OnStateChanged();
        }

        // Hebt die Auswahl einer Handkarte auf.
        public void DeselectActiveHandCard()
        {
            if (SelectedCardInstanceIdInHand != null)
            {
                SelectedCardInstanceIdInHand = null;
                OnStateChanged();
            }
        }

        // Lädt die geschlagenen Figuren vom Server, die für die "Wiedergeburt"-Karte zur Auswahl stehen.
        public async Task LoadCapturedPiecesForRebirthAsync(Guid gameId, Guid playerId, IGameSession gameSession)
        {
            try
            {
                var captured = await gameSession.GetCapturedPiecesAsync(gameId, playerId);
                // Bauern können nicht wiederbelebt werden.
                CapturedPiecesForRebirth = captured?.Where(p => p.Type != PieceType.Pawn).ToList() ?? [];
            }
            catch
            {
                CapturedPiecesForRebirth = [];
            }
            finally
            {
                OnStateChanged();
            }
        }

        // Leert die Liste der geschlagenen Figuren.
        public void ClearCapturedPiecesForRebirth()
        {
            CapturedPiecesForRebirth = null;
            OnStateChanged();
        }
    }
}