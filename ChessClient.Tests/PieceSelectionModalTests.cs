using Bunit;
using Xunit;
using ChessClient.Pages.Components;
using ChessClient.Models;
using ChessLogic;
using System.Collections.Generic;
using System.Linq;

namespace ChessClient.Tests
{
    public class PieceSelectionModalTests : TestContext
    {
        [Fact]
        public void RendersChoicesAndDisablesInvalidOnes()
        {
            // Arrange
            var choices = new List<PieceSelectionChoiceInfo>
            {
                new(PieceType.Queen, true, "Wiederbeleben als Dame"),
                new(PieceType.Rook, false, "Alle Startfelder für Turm sind besetzt.")
            };

            var cut = RenderComponent<PieceSelectionModal>(parameters => parameters
                .Add(p => p.IsVisible, true)
                .Add(p => p.Title, "Figur wiederbeleben")
                .Add(p => p.Choices, choices)
                .Add(p => p.PlayerColor, Player.White)
            );

            // Act
            var choiceElements = cut.FindAll(".piece-choice");
            var queenChoice = choiceElements.First(c => c.TextContent.Contains("Queen"));
            var rookChoice = choiceElements.First(c => c.TextContent.Contains("Rook"));

            // Assert
            Assert.Equal(2, choiceElements.Count);

            // Die Dame-Option sollte normal sein.
            Assert.DoesNotContain("disabled-choice", queenChoice.ClassList);

            // Die Turm-Option sollte deaktiviert sein und den Tooltip anzeigen.
            Assert.Contains("disabled-choice", rookChoice.ClassList);
            Assert.Equal("Alle Startfelder für Turm sind besetzt.", rookChoice.GetAttribute("title"));
        }
    }
}