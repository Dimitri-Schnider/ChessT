using Bunit;
using ChessClient.Pages.Components.Info;
using ChessLogic;
using System.Collections.Generic;
using Xunit;

namespace ChessClient.Tests
{
    // Erbt von TestContext, um die bUnit-Funktionalität zu erhalten
    public class TimersDisplayTests : TestContext
    {
        [Fact]
        public void RendersCorrectlyAndHighlightsActivePlayer()
        {
            // Arrange
            var playerNames = new Dictionary<Player, string>
            {
                { Player.White, "Magnus" },
                { Player.Black, "Hikaru" }
            };

            // 1. Initiales Rendern mit Weiss am Zug.
            var cut = RenderComponent<TimersDisplay>(parameters => parameters
                .Add(p => p.PlayerNames, playerNames)
                .Add(p => p.WhiteTimeDisplay, "04:55")
                .Add(p => p.BlackTimeDisplay, "05:00")
                .Add(p => p.CurrentTurnPlayer, Player.White)
            );

            // Act & Assert für den ersten Zustand
            var initialTimerBoxes = cut.FindAll(".timer-box");
            var initialWhiteTimerBox = initialTimerBoxes[0];
            var initialBlackTimerBox = initialTimerBoxes[1];

            Assert.Contains("Magnus", initialWhiteTimerBox.TextContent);
            Assert.Contains("04:55", initialWhiteTimerBox.TextContent);
            Assert.Contains("timer-active", initialWhiteTimerBox.ClassList);
            Assert.DoesNotContain("timer-active", initialBlackTimerBox.ClassList);

            // 2. Parameter ändern und Komponente neu rendern lassen.
            cut.SetParametersAndRender(parameters => parameters
                .Add(p => p.CurrentTurnPlayer, Player.Black) // Jetzt ist Schwarz am Zug
            );

            // 3. WICHTIG: Elemente nach dem Re-Rendering erneut abfragen.
            var updatedTimerBoxes = cut.FindAll(".timer-box");
            var updatedWhiteTimerBox = updatedTimerBoxes[0];
            var updatedBlackTimerBox = updatedTimerBoxes[1];

            // 4. Assert für den zweiten, aktualisierten Zustand.
            Assert.DoesNotContain("timer-active", updatedWhiteTimerBox.ClassList);
            Assert.Contains("timer-active", updatedBlackTimerBox.ClassList);
        }
    }
}