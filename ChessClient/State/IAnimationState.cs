// File: [SolutionDir]\ChessClient\State\IAnimationState.cs
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
        CardDto? LastAnimatedCard { get; }
        CardSwapAnimationDetailsDto? PendingSwapAnimationDetails { get; }
        void SetPendingSwapAnimationDetails(CardSwapAnimationDetailsDto? details);

        void SetLastAnimatedCard(CardDto card);
        void StartCardActivationAnimation(CardDto card, bool isOwnCard);
        void FinishCardActivationAnimation();
        bool IsCardSwapAnimating { get; }
        CardDto? CardGivenForSwap { get; }
        CardDto? CardReceivedForSwap { get; }
        void StartCardSwapAnimation(CardDto cardGiven, CardDto cardReceived);
        void FinishCardSwapAnimation();
    }
}