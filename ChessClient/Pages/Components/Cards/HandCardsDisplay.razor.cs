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
    // Die Code-Behind-Klasse für die Handkartenanzeige. Verwaltet die Logik für das Scrollen
    // per JS-Interop und die Interaktion mit dem Kartenzustand.
    public partial class HandCardsDisplay : ComponentBase, IDisposable
    {
        [Parameter] public List<CardDto> PlayerHand { get; set; } = new List<CardDto>();    // Die Liste der Handkarten.
        [Parameter] public bool IsSelectionDisabled { get; set; }                           // Gibt an, ob die Kartenauswahl global deaktiviert ist.

        [Inject] private ICardState CardState { get; set; } = null!;
        [Inject] private IGameCoreState GameCoreState { get; set; } = null!;
        [Inject] private IUiState UiState { get; set; } = null!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = null!;

        private ElementReference handCardsContainerRef;                 // Referenz auf den scrollbaren Karten-Container.
        private bool canScrollLeft;                                     // Zustand für den linken Scroll-Button.
        private bool canScrollRight;                                    // Zustand für den rechten Scroll-Button.
        private DotNetObjectReference<HandCardsDisplay>? dotNetHelper;  // Referenz auf diese Instanz für JS-Interop.

        // Initialisiert die Komponente und abonniert Zustandsänderungen.
        protected override void OnInitialized()
        {
            if (CardState != null) CardState.StateChanged += OnCardStateChanged;
            dotNetHelper = DotNetObjectReference.Create(this);
        }

        // Reagiert auf Änderungen im CardState, um die Komponente neu zu rendern.
        private async void OnCardStateChanged()
        {
            await InvokeAsync(StateHasChanged);
        }

        // Behandelt den Klick auf eine Karte und delegiert die Logik an den CardState.
        private async Task HandleCardClick(CardDto card)
        {
            if (IsSelectionDisabled) return;
            if (CardState != null && GameCoreState != null && UiState != null)
            {
                await CardState.SetSelectedHandCardAsync(card, GameCoreState, UiState);
            }
        }

        // Nach dem Rendern wird die JS-Interop für das Scrollen initialisiert.
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                if (handCardsContainerRef.Context != null && dotNetHelper != null && JSRuntime != null)
                {
                    try
                    {
                        // Ruft die JS-Funktion auf, um einen 'scroll'-Listener zum Container hinzuzufügen.
                        await JSRuntime.InvokeVoidAsync("handCardsDisplayInterop.addScrollListener", handCardsContainerRef, dotNetHelper);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in OnAfterRenderAsync trying to call addScrollListener: {ex.Message}");
                    }
                }
            }

            // Stellt sicher, dass der Zustand der Scroll-Buttons nach jedem Rendern korrekt ist.
            if (JSRuntime != null)
            {
                await UpdateScrollButtonStatesAsync();
            }
        }

        // Diese Methode wird von JavaScript aufgerufen, wenn der Benutzer scrollt.
        [JSInvokable]
        public async Task HandleScroll()
        {
            if (JSRuntime != null)
            {
                await UpdateScrollButtonStatesAsync();
            }
        }

        // Fragt den Scroll-Zustand von JS ab und aktualisiert die Sichtbarkeit der Scroll-Buttons.
        private async Task UpdateScrollButtonStatesAsync()
        {
            if (handCardsContainerRef.Context == null || JSRuntime == null) return;

            ScrollStateDto? scrollState = null;
            try
            {
                // Ruft eine JS-Funktion auf, die die Scroll-Eigenschaften des Elements zurückgibt.
                scrollState = await JSRuntime.InvokeAsync<ScrollStateDto>("handCardsDisplayInterop.getScrollState", handCardsContainerRef);
            }
            catch (Exception ex)
            {
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
                // Berechnet, ob nach links oder rechts gescrollt werden kann.
                bool newCanScrollLeft = scrollState.ScrollLeft > 0.5;
                bool newCanScrollRight = scrollState.ScrollLeft < (scrollState.ScrollWidth - scrollState.ClientWidth - 0.5);

                // Aktualisiert die UI nur, wenn sich der Zustand geändert hat, um Endlos-Loops zu vermeiden.
                if (newCanScrollLeft != canScrollLeft || newCanScrollRight != canScrollRight)
                {
                    canScrollLeft = newCanScrollLeft;
                    canScrollRight = newCanScrollRight;
                    await InvokeAsync(StateHasChanged);
                }
            }
        }

        // Scrollt den Container nach links.
        private async Task ScrollLeft()
        {
            if (handCardsContainerRef.Context != null && JSRuntime != null)
            {
                await JSRuntime.InvokeVoidAsync("handCardsDisplayInterop.scrollElementHorizontal", handCardsContainerRef, -120);
                await Task.Delay(160); // Kurze Verzögerung, um die Scroll-Animation abzuwarten, bevor die Buttons aktualisiert werden.
                await UpdateScrollButtonStatesAsync();
            }
        }

        // Scrollt den Container nach rechts.
        private async Task ScrollRight()
        {
            if (handCardsContainerRef.Context != null && JSRuntime != null)
            {
                await JSRuntime.InvokeVoidAsync("handCardsDisplayInterop.scrollElementHorizontal", handCardsContainerRef, 120);
                await Task.Delay(160);
                await UpdateScrollButtonStatesAsync();
            }
        }

        // Räumt alle Abonnements und JS-Interop-Referenzen auf.
        public void Dispose()
        {
            if (CardState != null) CardState.StateChanged -= OnCardStateChanged;
            if (handCardsContainerRef.Context != null && dotNetHelper != null && JSRuntime != null)
            {
                try
                {
                    // Versucht, den JS-Listener zu entfernen.
                    _ = JSRuntime.InvokeVoidAsync("handCardsDisplayInterop.removeScrollListener", handCardsContainerRef).AsTask();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during JS interop in Dispose (removeScrollListener): {ex.Message}");
                }
            }
            dotNetHelper?.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    // DTO zum Übertragen der Scroll-Informationen von JavaScript nach C#.
    public class ScrollStateDto
    {
        public double ScrollLeft { get; set; }
        public double ScrollWidth { get; set; }
        public double ClientWidth { get; set; }
    }
}