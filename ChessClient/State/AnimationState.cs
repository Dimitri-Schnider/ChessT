using System;
using ChessNetwork.DTOs;

namespace ChessClient.State
{
    // Implementierung der IAnimationState-Schnittstelle.
    // Verwaltet den Zustand für alle visuellen Vollbild-Animationen.
    public class AnimationState : IAnimationState
    {
        public event Action? StateChanged;                                                      // Event, das UI-Komponenten über Zustandsänderungen informiert.
        protected virtual void OnStateChanged() => StateChanged?.Invoke();                      // Löst das StateChanged-Event sicher aus.
        public bool IsCardActivationAnimating { get; private set; }                             // Gibt an, ob die generische Kartenaktivierungs-Animation läuft.
        public CardDto? CardForAnimation { get; private set; }                                  // Die Karte, die in der generischen Animation angezeigt wird.
        public bool IsOwnCardForAnimation { get; private set; }                                 // Gibt an, ob die Animation für die eigene Karte oder die des Gegners ist.
        public bool IsCardSwapAnimating { get; private set; }                                   // Gibt an, ob die spezifische Kartentausch-Animation läuft.
        public CardDto? CardGivenForSwap { get; private set; }                                  // Die Karte, die der Spieler im Tausch abgibt.
        public CardDto? CardReceivedForSwap { get; private set; }                               // Die Karte, die der Spieler im Tausch erhält.
        public CardDto? LastAnimatedCard { get; private set; }                                  // Speichert die zuletzt animierte Karte, um den Kontext nach der Animation zu kennen.
        public CardSwapAnimationDetailsDto? PendingSwapAnimationDetails { get; private set; }   // Speichert die Details für eine anstehende Tauschanimation, falls die generische Animation noch läuft.
        public bool IsGenericAnimationFinishedForSwap { get; private set; }                     // Flag, um zu signalisieren, dass die generische Animation für einen Tausch beendet ist und die spezifische starten kann.

        // Setzt das Flag, dass die generische Animation für den Kartentausch abgeschlossen ist.
        public void SetGenericAnimationFinishedForSwap(bool isFinished)
        {
            IsGenericAnimationFinishedForSwap = isFinished;
        }

        // Speichert die Details für eine anstehende Tauschanimation.
        public void SetPendingSwapAnimationDetails(CardSwapAnimationDetailsDto? details)
        {
            PendingSwapAnimationDetails = details;
            OnStateChanged();
        }

        // Merkt sich die zuletzt animierte Karte.
        public void SetLastAnimatedCard(CardDto card)
        {
            LastAnimatedCard = card;
        }

        // Startet die generische Kartenaktivierungs-Animation.
        public void StartCardActivationAnimation(CardDto card, bool isOwnCard)
        {
            CardForAnimation = card;
            IsOwnCardForAnimation = isOwnCard;
            IsCardActivationAnimating = true;
            OnStateChanged();
        }

        // Beendet die generische Kartenaktivierungs-Animation und setzt die zugehörigen Zustände zurück.
        public void FinishCardActivationAnimation()
        {
            IsCardActivationAnimating = false;
            LastAnimatedCard = null;
            OnStateChanged();
        }

        // Startet die spezifische Kartentausch-Animation.
        public void StartCardSwapAnimation(CardDto cardGiven, CardDto cardReceived)
        {
            CardGivenForSwap = cardGiven;
            CardReceivedForSwap = cardReceived;
            IsCardSwapAnimating = true;
            OnStateChanged();
        }

        // Beendet die Kartentausch-Animation und setzt die Zustände zurück.
        public void FinishCardSwapAnimation()
        {
            IsCardSwapAnimating = false;
            CardGivenForSwap = null;
            CardReceivedForSwap = null;
            OnStateChanged();
        }
    }
}