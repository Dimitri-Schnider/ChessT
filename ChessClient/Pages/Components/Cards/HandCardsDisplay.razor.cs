// File: [SolutionDir]\ChessClient\Pages\Components\Cards\HandCardsDisplay.razor.cs
using ChessNetwork.DTOs;
using ChessClient.State;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace ChessClient.Pages.Components.Cards
{
    public partial class HandCardsDisplay : ComponentBase, IDisposable
    {
        [Parameter] public List<CardDto> PlayerHand { get; set; } = new List<CardDto>();
        [Parameter] public bool IsSelectionDisabled { get; set; }

        [Inject] private ICardState CardState { get; set; } = null!;
        [Inject] private IGameCoreState GameCoreState { get; set; } = null!;
        [Inject] private IUiState UiState { get; set; } = null!;

        // Stelle sicher, dass IJSRuntime nur einmal injiziert wird.
        [Inject] private IJSRuntime JSRuntime { get; set; } = null!;

        private ElementReference handCardsContainerRef;
        private bool canScrollLeft;
        private bool canScrollRight;
        private DotNetObjectReference<HandCardsDisplay>? dotNetHelper;

        // private bool _isFirstRender; // Entfernt, da nicht im aktuellen Snippet verwendet und Fehlerquelle war

        protected override void OnInitialized()
        {
            if (CardState != null) CardState.StateChanged += OnCardStateChanged;
            dotNetHelper = DotNetObjectReference.Create(this);
        }

        private async void OnCardStateChanged()
        {
            await InvokeAsync(StateHasChanged);
            if (JSRuntime != null) // Prüfen, ob JSRuntime verfügbar ist
            {
                // Es ist besser, UpdateScrollButtonStatesAsync nach dem Rendern aufzurufen,
                // wenn sich die Kartenliste geändert hat. Dies geschieht in OnAfterRenderAsync.
                // Der direkte Aufruf hier könnte veraltete DOM-Dimensionen sehen.
                // Stattdessen stellen wir sicher, dass OnAfterRenderAsync die Aktualisierung vornimmt.
            }
        }

        private async Task HandleCardClick(CardDto card)
        {
            if (IsSelectionDisabled) return;
            if (CardState != null && GameCoreState != null && UiState != null)
            {
                await CardState.SetSelectedHandCardAsync(card, GameCoreState, UiState);
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                if (handCardsContainerRef.Context != null && dotNetHelper != null && JSRuntime != null)
                {
                    try
                    {
                        await JSRuntime.InvokeVoidAsync("handCardsDisplayInterop.addScrollListener", handCardsContainerRef, dotNetHelper);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in OnAfterRenderAsync trying to call addScrollListener: {ex.Message}");
                    }
                }
            }

            if (JSRuntime != null)
            {
                await UpdateScrollButtonStatesAsync();
            }
        }

        [JSInvokable]
        public async Task HandleScroll()
        {
            if (JSRuntime != null)
            {
                await UpdateScrollButtonStatesAsync();
            }
        }

        private async Task UpdateScrollButtonStatesAsync()
        {
            // Stelle sicher, dass JSRuntime hier nicht null ist, bevor du es verwendest.
            if (handCardsContainerRef.Context == null || JSRuntime == null) return;

            ScrollStateDto? scrollState = null;
            try
            {
                scrollState = await JSRuntime.InvokeAsync<ScrollStateDto>("handCardsDisplayInterop.getScrollState", handCardsContainerRef);
            }
            catch (Exception ex)
            {
                // Dieser Fehler kann auftreten, wenn die Komponente disposed wird, während ein JS-Aufruf noch aussteht.
                if (ex is JSDisconnectedException || ex.Message.Contains("JavaScript interop calls cannot be issued because the component is not connected to the browser"))
                {
                    Console.WriteLine($"UpdateScrollButtonStatesAsync: Component is likely disposed or disconnecting. Error: {ex.Message}");
                }
                else
                {
                    Console.WriteLine($"Error calling getScrollState in UpdateScrollButtonStatesAsync: {ex.Message}");
                }
                return;
            }

            if (scrollState != null)
            {
                bool newCanScrollLeft = scrollState.ScrollLeft > 0.5;
                bool newCanScrollRight = scrollState.ScrollLeft < (scrollState.ScrollWidth - scrollState.ClientWidth - 0.5);

                if (newCanScrollLeft != canScrollLeft || newCanScrollRight != canScrollRight)
                {
                    canScrollLeft = newCanScrollLeft;
                    canScrollRight = newCanScrollRight;
                    // StateHasChanged nur aufrufen, wenn sich der Zustand wirklich geändert hat, um Loops zu vermeiden.
                    await InvokeAsync(StateHasChanged);
                }
            }
        }

        private async Task ScrollLeft()
        {
            if (handCardsContainerRef.Context != null && JSRuntime != null)
            {
                await JSRuntime.InvokeVoidAsync("handCardsDisplayInterop.scrollElementHorizontal", handCardsContainerRef, -120);
                await Task.Delay(160);
                await UpdateScrollButtonStatesAsync();
            }
        }

        private async Task ScrollRight()
        {
            if (handCardsContainerRef.Context != null && JSRuntime != null)
            {
                await JSRuntime.InvokeVoidAsync("handCardsDisplayInterop.scrollElementHorizontal", handCardsContainerRef, 120);
                await Task.Delay(160);
                await UpdateScrollButtonStatesAsync();
            }
        }

        public void Dispose()
        {
            if (CardState != null) CardState.StateChanged -= OnCardStateChanged;

            if (handCardsContainerRef.Context != null && dotNetHelper != null && JSRuntime != null)
            {
                // Es ist sicherer, InvokeVoidAsync nicht mehr in Dispose aufzurufen, wenn die Komponente bereits
                // dabei ist, sich zu zerlegen, da JS-Interop-Aufrufe fehlschlagen können.
                // Der JS-Code sollte idealerweise so robust sein, dass er damit umgehen kann, wenn
                // dotNetHelper.invokeMethodAsync fehlschlägt, weil die .NET-Seite disposed wurde.
                // Für eine saubere Entfernung des Listeners könnte man im JS prüfen, ob der dotNetHelper noch "lebt",
                // was aber schwierig ist. Alternativ kann man den Fehler im JS try-catch abfangen.
                try
                {
                    // Versuche, den Listener zu entfernen. Akzeptiere, dass es fehlschlagen kann.
                    _ = JSRuntime.InvokeVoidAsync("handCardsDisplayInterop.removeScrollListener", handCardsContainerRef).AsTask();
                }
                catch (Exception ex)
                {
                    // Hier nur loggen, keinen Fehler werfen, der Dispose stört.
                    Console.WriteLine($"Error during JS interop in Dispose (removeScrollListener): {ex.Message}");
                }
            }
            dotNetHelper?.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    public class ScrollStateDto
    {
        public double ScrollLeft { get; set; }
        public double ScrollWidth { get; set; }
        public double ClientWidth { get; set; }
    }
}