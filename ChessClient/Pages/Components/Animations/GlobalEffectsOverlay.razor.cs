using ChessClient.State;
using Microsoft.AspNetCore.Components;
using System;
using System.Globalization;

namespace ChessClient.Pages.Components.Animations
{
    // Die Code-Behind-Klasse für das GlobalEffectsOverlay.
    // Sie reagiert auf Änderungen im IUiState, um die Animationen zu steuern
    // und generiert die zufälligen Stile für den Konfetti-Effekt.
    public partial class GlobalEffectsOverlay : ComponentBase, IDisposable
    {
        [Inject] private IUiState UiState { get; set; } = default!; // Injiziert den UI-State-Container, um auf Zustandsänderungen wie "Sieg" oder "Niederlage" reagieren zu können.
        private readonly Random _random = new();                    // Ein einzelnes Random-Objekt für die gesamte Lebensdauer der Komponente, um bessere Zufallswerte zu gewährleisten.

        // Lifecycle-Methode: Abonniert das StateChanged-Event des UI-States bei der Initialisierung.
        protected override void OnInitialized()
        {
            UiState.StateChanged += OnUiStateChanged;
        }

        // Wird aufgerufen, wenn sich der UI-State ändert.
        private void OnUiStateChanged()
        {
            // Stösst ein erneutes Rendern der Komponente an, um die Animationen basierend auf dem neuen Zustand ein- oder auszublenden.
            InvokeAsync(StateHasChanged);
        }

        // Generiert und retourniert einen String mit zufälligen CSS-Eigenschaften für ein einzelnes Konfetti-Element.
        protected string GetConfettiStyle(int index)
        {
            double startX = _random.NextDouble() * 100;                 // Zufällige horizontale Startposition.
            double delay = _random.NextDouble() * 2;                    // Zufällige Startverzögerung der Animation.
            double duration = 1.5 + _random.NextDouble() * 2.0;         // Zufällige Dauer der Fall-Animation.
            double rotation = _random.Next(-360, 360);                  // Zufällige Endrotation.
            string color = $"hsl({_random.Next(0, 360)}, 90%, 65%)";    // Zufällige HSL-Farbe.

            // Baut den Inline-Style-String zusammen. CultureInfo.InvariantCulture wird für konsistente Dezimaltrennzeichen (.) verwendet.
            return $"--start-x: {startX.ToString(CultureInfo.InvariantCulture)}vw; " +
                   $"--delay: {delay.ToString(CultureInfo.InvariantCulture)}s; " +
                   $"--duration: {duration.ToString(CultureInfo.InvariantCulture)}s; " +
                   $"--rotation: {rotation.ToString(CultureInfo.InvariantCulture)}deg; " +
                   $"--color: {color};";
        }

        // Gibt die Ressourcen frei, wenn die Komponente zerstört wird.
        public void Dispose()
        {
            // Deregistriert den Event-Handler, um Memory Leaks zu verhindern.
            UiState.StateChanged -= OnUiStateChanged;
            GC.SuppressFinalize(this);
        }
    }
}