// File: [SolutionDir]/ChessClient/State/AnimationState.cs
using System;
using ChessNetwork.DTOs;

namespace ChessClient.State
{
    public class AnimationState : IAnimationState
    {
        public event Action? StateChanged;
        protected virtual void OnStateChanged() => StateChanged?.Invoke();

        public bool IsCardActivationAnimating { get; private set; }
        public CardDto? CardForAnimation { get; private set; }
        public bool IsOwnCardForAnimation { get; private set; }

        public bool IsCardSwapAnimating { get; private set; }
        public CardDto? CardGivenForSwap { get; private set; }
        public CardDto? CardReceivedForSwap { get; private set; }


        public AnimationState()
        {
        }

        public void StartCardActivationAnimation(CardDto card, bool isOwnCard)
        {
            CardForAnimation = card;
            IsOwnCardForAnimation = isOwnCard;
            IsCardActivationAnimating = true;
            OnStateChanged();
        }

        public void FinishCardActivationAnimation()
        {
            IsCardActivationAnimating = false;
            OnStateChanged();
        }

        public void StartCardSwapAnimation(CardDto cardGiven, CardDto cardReceived)
        {
            CardGivenForSwap = cardGiven;
            CardReceivedForSwap = cardReceived;
            IsCardSwapAnimating = true;
            OnStateChanged();
        }

        public void FinishCardSwapAnimation()
        {
            IsCardSwapAnimating = false;
            CardGivenForSwap = null;
            CardReceivedForSwap = null;
            OnStateChanged();
        }
    }
}