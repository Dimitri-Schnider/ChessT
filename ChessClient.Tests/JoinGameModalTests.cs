using Bunit;
using Xunit;
using ChessClient.Pages.Components;
using ChessClient.Models;
using System;

namespace ChessClient.Tests
{
    public class JoinGameModalTests : TestContext
    {
        [Fact]
        public void JoinButtonIsDisabledWhenInputsAreEmpty()
        {
            // Arrange
            var cut = RenderComponent<JoinGameModal>(parameters => parameters
                .Add(p => p.IsVisible, true)
            );

            // Act: Finde den Button, wenn noch nichts ausgefüllt ist.
            var joinButton = cut.Find("button.btn-primary");

            // Assert: Der Button muss deaktiviert sein.
            Assert.True(joinButton.HasAttribute("disabled"));

            // Act: Fülle nur den Namen aus.
            cut.Find("#joinPlayerName").Input("Garry");
            joinButton = cut.Find("button.btn-primary");

            // Assert: Der Button muss immer noch deaktiviert sein.
            Assert.True(joinButton.HasAttribute("disabled"));

            // Act: Leere den Namen und fülle nur die GameId aus.
            cut.Find("#joinPlayerName").Input("");
            cut.Find("#joinGameId").Change(Guid.NewGuid().ToString());
            joinButton = cut.Find("button.btn-primary");

            // Assert: Der Button muss immer noch deaktiviert sein.
            Assert.True(joinButton.HasAttribute("disabled"));
        }

        [Fact]
        public void JoinButtonIsEnabledWhenBothInputsAreFilled()
        {
            // Arrange
            var cut = RenderComponent<JoinGameModal>(parameters => parameters
                .Add(p => p.IsVisible, true)
            );

            // Act: Fülle beide Felder aus.
            cut.Find("#joinPlayerName").Input("Anatoly");
            cut.Find("#joinGameId").Change(Guid.NewGuid().ToString());
            var joinButton = cut.Find("button.btn-primary");

            // Assert: Der Button muss jetzt aktiviert sein.
            Assert.False(joinButton.HasAttribute("disabled"));
        }

        [Fact]
        public void OnJoinGameEventIsFiredWithCorrectParameters()
        {
            // Arrange
            JoinGameParameters? capturedParameters = null;
            var gameId = Guid.NewGuid().ToString();
            var playerName = "Bobby";

            var cut = RenderComponent<JoinGameModal>(parameters => parameters
                .Add(p => p.IsVisible, true)
                .Add(p => p.OnJoinGame, args => capturedParameters = args) // Event abfangen
            );

            cut.Find("#joinPlayerName").Input(playerName);
            cut.Find("#joinGameId").Change(gameId);

            // Act
            cut.Find("button.btn-primary").Click();

            // Assert
            Assert.NotNull(capturedParameters);
            Assert.Equal(playerName, capturedParameters.Name);
            Assert.Equal(gameId, capturedParameters.GameId);
        }
    }
}