// File: [SolutionDir]\ChessClient\State\CardState.cs
using System.Globalization;
using ChessClient.Models;
using ChessNetwork.Configuration;
using ChessLogic;
using ChessNetwork;
using ChessNetwork.DTOs;

namespace ChessClient.State
{
    public class CardState : ICardState
    {
        public event Action? StateChanged;
        protected virtual void OnStateChanged()
        {
            StateChanged?.Invoke();
        }


        public CardDto? SelectedCardForInfoPanel { get; private set; }
        public bool IsCardActivationPending { get; private set; }
        public List<CardDto> PlayerHandCards { get; private set; } = [];
        public int MyDrawPileCount { get; private set; }

        public List<PlayedCardInfo> MyPlayedCardsForHistory { get; private set; } = [];
        public List<PlayedCardInfo> OpponentPlayedCardsForHistory { get; private set; } = [];
        public bool IsPreviewingPlayedCard { get; private set; }
        public Guid? SelectedCardInstanceIdInHand { get; private set; }

        public List<CapturedPieceTypeDto>? CapturedPiecesForRebirth { get; private set; }

        private readonly IModalState _modalState;

        public CardState(IModalState modalState) => _modalState = modalState;
        public CardDto? GetCardDefinitionById(string cardTypeId)
        {
            Console.WriteLine($"[CardState WARNUNG] GetCardDefinitionById({cardTypeId}) aufgerufen. Client sollte volle CardDtos erhalten.");
            if (PlayerHandCards.FirstOrDefault(c => c.Id == cardTypeId) is CardDto cardInHand)
            {
                return cardInHand;
            }

            if (MyPlayedCardsForHistory.FirstOrDefault(pci => pci.CardDefinition.Id == cardTypeId) is PlayedCardInfo myPlayedCard)
            {
                return myPlayedCard.CardDefinition;
            }

            if (OpponentPlayedCardsForHistory.FirstOrDefault(pci => pci.CardDefinition.Id == cardTypeId) is PlayedCardInfo oppPlayedCard)
            {
                return oppPlayedCard.CardDefinition;
            }
            return new CardDto { InstanceId = Guid.Empty, Id = cardTypeId, Name = cardTypeId, Description = "Details nicht clientseitig verfügbar.", ImageUrl = CardConstants.DefaultCardBackImageUrl };
        }

        public void UpdateHandAndDrawPile(InitialHandDto newHandInfo)
        {
            PlayerHandCards.Clear();
            if (newHandInfo?.Hand != null)
            {
                PlayerHandCards.AddRange(newHandInfo.Hand);
            }
            MyDrawPileCount = newHandInfo?.DrawPileCount ?? 0;
            if (SelectedCardInstanceIdInHand.HasValue && !PlayerHandCards.Any(c => c.InstanceId == SelectedCardInstanceIdInHand.Value))
            {
                SelectedCardInstanceIdInHand = null;
                SelectedCardForInfoPanel = null;
                IsPreviewingPlayedCard = false;
            }
            Console.WriteLine($"[CardState] Hand and draw pile updated by server. Hand size: {PlayerHandCards.Count}, Draw pile: {MyDrawPileCount}");
            OnStateChanged();
        }

        public void SetInitialHand(InitialHandDto initialHandDto)
        {
            PlayerHandCards.Clear();
            if (initialHandDto?.Hand != null)
            {
                PlayerHandCards.AddRange(initialHandDto.Hand);
            }
            MyDrawPileCount = initialHandDto?.DrawPileCount ?? 0;
            MyPlayedCardsForHistory.Clear();
            OpponentPlayedCardsForHistory.Clear();
            SelectedCardInstanceIdInHand = null;
            SelectedCardForInfoPanel = null;
            IsPreviewingPlayedCard = false;
            IsCardActivationPending = false;
            ClearCapturedPiecesForRebirth();
            Console.WriteLine($"[CardState] Initial hand received. Hand size: {PlayerHandCards.Count}, Draw pile: {MyDrawPileCount}");
            OnStateChanged();
        }

        public void AddReceivedCardToHand(CardDto drawnCard, int newDrawPileCount)
        {
            if (drawnCard != null)
            {
                // *** BEGINN DER ÄNDERUNG ***
                // Prüfen, ob eine Karte mit derselben InstanceId bereits in der Hand ist.
                if (PlayerHandCards.Any(card => card.InstanceId == drawnCard.InstanceId))
                {
                    Console.WriteLine($"[CardState WARNUNG] Versuch, Karte mit bereits vorhandener InstanceId {drawnCard.InstanceId} ('{drawnCard.Name}') hinzuzufügen. Hinzufügen übersprungen.");
                    // Optional: MyDrawPileCount trotzdem aktualisieren, wenn die Server-Info als autoritativ gilt.
                    if (MyDrawPileCount != newDrawPileCount)
                    {
                        MyDrawPileCount = newDrawPileCount;
                        OnStateChanged(); // Nur wenn sich der Zähler geändert hat
                    }
                    return; // Verhindert das Hinzufügen des Duplikats
                }
                // *** ENDE DER ÄNDERUNG ***

                if (!drawnCard.Name.Contains(CardConstants.NoMoreCardsName, StringComparison.Ordinal) &&
                   !drawnCard.Name.Contains(CardConstants.ReplacementCardName, StringComparison.Ordinal))
                {
                    PlayerHandCards.Add(drawnCard);
                }
            }
            MyDrawPileCount = newDrawPileCount;
            Console.WriteLine($"[CardState] Card '{drawnCard?.Name}' (Instance: {drawnCard?.InstanceId}) added to hand by server. Hand size: {PlayerHandCards.Count}, Draw pile: {MyDrawPileCount}");
            OnStateChanged();
        }

        public void SelectCardForInfoPanel(CardDto? card, bool isPreview)
        {
            if (card != null)
            {
                _modalState.OpenCardInfoPanelModal(card, !isPreview, isPreview);
            }
            // Die alten Properties werden nicht mehr direkt hier gesetzt, das ModalState übernimmt.
            // OnStateChanged() wird durch _modalState ausgelöst, falls nötig.
        }

        public async Task SetSelectedHandCardAsync(CardDto card, IGameCoreState gameCoreState, IUiState uiState)
        {
            if (gameCoreState.CurrentPlayerInfo != null && !gameCoreState.OpponentJoined)
            {
                await uiState.SetCurrentInfoMessageForBoxAsync("Warte, bis dein Gegner beigetreten ist, um Karten auszuwaehlen.");
                // Kein OnStateChanged() hier, da keine *dieses* Zustands relevante Änderung
                return;
            }

            bool changed = false;
            if (gameCoreState.CurrentPlayerInfo != null && gameCoreState.MyColor == gameCoreState.CurrentTurnPlayer &&
                !IsCardActivationPending && string.IsNullOrEmpty(gameCoreState.EndGameMessage))
            {
                if (SelectedCardInstanceIdInHand != card.InstanceId)
                {
                    SelectedCardInstanceIdInHand = card.InstanceId;
                    changed = true;
                }
                bool isActivatable = gameCoreState.MyColor == gameCoreState.CurrentTurnPlayer && !IsCardActivationPending && string.IsNullOrEmpty(gameCoreState.EndGameMessage);
                _modalState.OpenCardInfoPanelModal(card, isActivatable, false); // Löst sein eigenes StateChanged aus
                await uiState.SetCurrentInfoMessageForBoxAsync(string.Format(CultureInfo.CurrentCulture, "Karte '{0}' ausgewaehlt. Bestätige im Modal.", card.Name));
                // Löst sein eigenes StateChanged aus
            }
            else
            {
                if (SelectedCardInstanceIdInHand != null)
                {
                    SelectedCardInstanceIdInHand = null;
                    changed = true;
                }
                if (string.IsNullOrEmpty(uiState.CurrentInfoMessageForBox) || uiState.CurrentInfoMessageForBox == "Warte, bis dein Gegner beigetreten ist, um Karten auszuwaehlen.")
                {
                    if (gameCoreState.CurrentPlayerInfo == null || gameCoreState.MyColor != gameCoreState.CurrentTurnPlayer)
                    {
                        await uiState.SetCurrentInfoMessageForBoxAsync("Du kannst gerade keine Karte auswaehlen oder spielen.");
                    }
                }
            }
            if (changed) OnStateChanged();
        }

        public void SetIsCardActivationPending(bool isPending)
        {
            if (IsCardActivationPending == isPending) return;
            IsCardActivationPending = isPending;
            OnStateChanged();
        }

        public void HandleCardPlayedByMe(Guid cardInstanceId, string cardTypeId)
        {
            int removedCount = PlayerHandCards.RemoveAll(c => c.InstanceId == cardInstanceId);
            bool changed = removedCount > 0;

            Console.WriteLine($"[CardState] Karte mit Instanz-ID {cardInstanceId} (Typ: {cardTypeId}) von mir gespielt und aus Hand entfernt. Aktuelle Handgrösse: {PlayerHandCards.Count}");
            if (changed) OnStateChanged();
        }

        public void AddToMyPlayedHistory(PlayedCardInfo cardInfo)
        {
            MyPlayedCardsForHistory.Add(cardInfo);
            OnStateChanged();
        }

        public void AddToOpponentPlayedHistory(PlayedCardInfo cardInfo)
        {
            OpponentPlayedCardsForHistory.Add(cardInfo);
            bool changed = false;
            if (SelectedCardForInfoPanel?.Id == cardInfo.CardDefinition.Id && (PlayerHandCards.Count == 0 || !PlayerHandCards.Exists(c => c.Id == cardInfo.CardDefinition.Id)))
            {
                SelectedCardForInfoPanel = null;
                IsPreviewingPlayedCard = false;
                changed = true;
            }
            if (changed || OpponentPlayedCardsForHistory.Count == 1) OnStateChanged();
        }

        public void ClearPlayedCardsHistory()
        {
            if (MyPlayedCardsForHistory.Count == 0 && OpponentPlayedCardsForHistory.Count == 0) return;
            MyPlayedCardsForHistory.Clear();
            OpponentPlayedCardsForHistory.Clear();
            OnStateChanged();
        }

        public void ClearSelectedCardForInfoPanel()
        {
            bool changed = false;
            if (SelectedCardForInfoPanel != null) { SelectedCardForInfoPanel = null; changed = true; }
            if (SelectedCardInstanceIdInHand != null) { SelectedCardInstanceIdInHand = null; changed = true; }
            if (IsPreviewingPlayedCard) { IsPreviewingPlayedCard = false; changed = true; }
            if (changed) OnStateChanged();
        }

        public void DeselectActiveHandCard()
        {
            if (SelectedCardInstanceIdInHand == null) return;
            SelectedCardInstanceIdInHand = null;
            OnStateChanged();
        }

        public async Task LoadCapturedPiecesForRebirthAsync(Guid gameId, Guid playerId, IGameSession gameSession)
        {
            try
            {
                IEnumerable<CapturedPieceTypeDto> captured = await gameSession.GetCapturedPiecesAsync(gameId, playerId);
                var newCapturedList = captured?.Where(p => p.Type != PieceType.Pawn).ToList() ?? [];
                bool listChanged = CapturedPiecesForRebirth == null || !CapturedPiecesForRebirth.SequenceEqual(newCapturedList);
                CapturedPiecesForRebirth = newCapturedList;
                if (listChanged) OnStateChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CardState] LoadCapturedPiecesForRebirthAsync: FEHLER beim Laden der geschlagenen Figuren: {ex.Message}");
                if (CapturedPiecesForRebirth == null || CapturedPiecesForRebirth.Count > 0)
                {
                    CapturedPiecesForRebirth = [];
                    OnStateChanged();
                }
            }
        }

        public void ClearCapturedPiecesForRebirth()
        {
            if (CapturedPiecesForRebirth != null && CapturedPiecesForRebirth.Count > 0)
            {
                CapturedPiecesForRebirth = null;
                OnStateChanged();
            }
        }
    }
}