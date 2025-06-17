using System;
using ChessNetwork.DTOs;

namespace ChessClient.State
{
    // Definiert den Vertrag für den State-Container, der alle UI-Animationen verwaltet.
    public interface IAnimationState
    {
        // Wird ausgelöst, wenn sich der Animations-Zustand ändert.
        event Action? StateChanged;

        // --- Zustände für die generische Kartenaktivierungs-Animation ---
        bool IsCardActivationAnimating { get; }
        CardDto? CardForAnimation { get; }
        bool IsOwnCardForAnimation { get; }
        CardDto? LastAnimatedCard { get; } // Um nach der Animation den Kontext zu kennen (z.B. für CardSwap)

        // --- Zustände für die spezifische Kartentausch-Animation ---
        bool IsCardSwapAnimating { get; }
        CardDto? CardGivenForSwap { get; }
        CardDto? CardReceivedForSwap { get; }
        CardSwapAnimationDetailsDto? PendingSwapAnimationDetails { get; }
        bool IsGenericAnimationFinishedForSwap { get; }

        // --- Methoden zur Steuerung der Animationen ---
        void SetLastAnimatedCard(CardDto card);
        void StartCardActivationAnimation(CardDto card, bool isOwnCard);
        void FinishCardActivationAnimation();
        void StartCardSwapAnimation(CardDto cardGiven, CardDto cardReceived);
        void FinishCardSwapAnimation();
        void SetPendingSwapAnimationDetails(CardSwapAnimationDetailsDto? details);
        void SetGenericAnimationFinishedForSwap(bool isFinished);
    }
}