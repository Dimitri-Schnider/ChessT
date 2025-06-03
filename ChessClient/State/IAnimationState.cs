using System;
using ChessNetwork.DTOs;

namespace ChessClient.State
{
    public interface IAnimationState
    {
        event Action? StateChanged;
        bool IsCardActivationAnimating { get; }
        CardDto? CardForAnimation { get; }
        bool IsOwnCardForAnimation { get; }

        void StartCardActivationAnimation(CardDto card, bool isOwnCard);
        void FinishCardActivationAnimation();

        // Für Kartentausch Animation
        bool IsCardSwapAnimating { get; }
        CardDto? CardGivenForSwap { get; }
        CardDto? CardReceivedForSwap { get; }
        void StartCardSwapAnimation(CardDto cardGiven, CardDto cardReceived);
        void FinishCardSwapAnimation();
    }
}