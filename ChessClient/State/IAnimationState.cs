using System;
using ChessNetwork.DTOs;

namespace ChessClient.State
{
    // Definiert den Vertrag für den State-Container, der alle UI-Animationen verwaltet.
    public interface IAnimationState
    {
        // Wird ausgelöst, wenn sich der Animations-Zustand ändert.
        event Action? StateChanged;

        // Zustände für die generische Kartenaktivierungs-Animation
        bool IsCardActivationAnimating { get; }                             // Gibt an, ob die generische Kartenaktivierungs-Animation läuft.
        CardDto? CardForAnimation { get; }                                  // Die Karte, die in der generischen Animation angezeigt wird.
        bool IsOwnCardForAnimation { get; }                                 // Gibt an, ob die Animation für die eigene Karte oder die des Gegners ist.
        CardDto? LastAnimatedCard { get; }                                  // Um nach der Animation den Kontext zu kennen (z.B. für CardSwap).
        bool IsCardSwapAnimating { get; }                                   // Gibt an, ob die spezifische Kartentausch-Animation läuft.
        CardDto? CardGivenForSwap { get; }                                  // Die Karte, die der Spieler im Tausch abgibt.
        CardDto? CardReceivedForSwap { get; }                               // Die Karte, die der Spieler im Tausch erhält.
        CardSwapAnimationDetailsDto? PendingSwapAnimationDetails { get; }   // Speichert die Details für eine anstehende Tauschanimation.
        bool IsGenericAnimationFinishedForSwap { get; }                     // Flag, um zu signalisieren, dass die generische Animation für einen Tausch beendet ist.

        
        // Merkt sich die zuletzt animierte Karte.
        void SetLastAnimatedCard(CardDto card);
        // Startet die generische Kartenaktivierungs-Animation.
        void StartCardActivationAnimation(CardDto card, bool isOwnCard);
        // Beendet die generische Kartenaktivierungs-Animation und setzt die zugehörigen Zustände zurück.
        void FinishCardActivationAnimation();
        // Startet die spezifische Kartentausch-Animation.
        void StartCardSwapAnimation(CardDto cardGiven, CardDto cardReceived);
        // Beendet die Kartentausch-Animation und setzt die Zustände zurück.
        void FinishCardSwapAnimation();
        // Speichert die Details für eine anstehende Tauschanimation.
        void SetPendingSwapAnimationDetails(CardSwapAnimationDetailsDto? details);
        // Setzt das Flag, dass die generische Animation für den Kartentausch abgeschlossen ist.
        void SetGenericAnimationFinishedForSwap(bool isFinished);
    }
}