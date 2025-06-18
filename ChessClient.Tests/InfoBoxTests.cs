using Bunit;
using Xunit;
using System.Threading.Tasks;
using ChessClient.Pages.Components.Info;
using ChessClient.Pages.Components.Dialogs;

namespace ChessClient.Tests
{
    public class InfoBoxTests : TestContext
    {
        [Fact]
        public void RendersCorrectlyWhenMessageIsSet()
        {
            // Arrange
            var message = "Du stehst im Schach!";

            // Act
            var cut = RenderComponent<InfoBox>(parameters => parameters
                .Add(p => p.Message, message)
            );

            // Assert
            // Überprüft, ob die gerenderte Komponente den erwarteten Text enthält.
            var infoBox = cut.Find(".info-box-wrapper");
            Assert.Contains(message, infoBox.TextContent);
        }

        [Fact]
        public async Task DisappearsAutomaticallyAfterDuration()
        {
            // Arrange
            var message = "Gegner ist beigetreten";
            var cut = RenderComponent<InfoBox>(parameters => parameters
                .Add(p => p.Message, message)
                .Add(p => p.AutoHide, true)
                .Add(p => p.DurationMs, 10) // Eine sehr kurze Dauer für den Test
            );

            // Assert: Zuerst prüfen, ob die Box sichtbar ist.
            Assert.Single(cut.FindAll(".info-box-wrapper"));

            // Act: Warte länger als die eingestellte Dauer.
            await Task.Delay(50);

            // Assert: Überprüfe, ob die Komponente aus dem Render-Tree verschwunden ist.
            Assert.Empty(cut.FindAll(".info-box-wrapper"));
        }

        [Fact]
        public void DoesNotDisappearWhenAutoHideIsFalse()
        {
            // Arrange
            var cut = RenderComponent<InfoBox>(parameters => parameters
                .Add(p => p.Message, "Warte auf Gegner...")
                .Add(p => p.AutoHide, false)
            );

            // Assert
            // Die Box muss nach dem Rendern da sein und bleiben.
            Assert.Single(cut.FindAll(".info-box-wrapper"));
        }
    }
}