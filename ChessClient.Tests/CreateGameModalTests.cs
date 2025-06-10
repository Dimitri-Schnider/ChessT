// In ChessClient.Tests/CreateGameModalTests.cs

using Bunit; // Das bUnit Test-Framework
using ChessClient.Models; // Der Namespace für CreateGameParameters
using ChessClient.Pages.Components; // Der Namespace deiner Komponente
using ChessLogic; // Der Namespace für den Player Enum
using Microsoft.AspNetCore.Components.Web; // Für MouseEventArgs
using Xunit;

namespace ChessClient.Tests;

// TestContext von bUnit erben, um die Testumgebung bereitzustellen
public class CreateGameModalTests : TestContext
{
    [Fact]
    public void CreateGameButtonIsDisabledWhenPlayerNameIsEmpty()
    {
        // Arrange
        // Rendere die Komponente. "RenderComponent" kommt von bUnit.
        var cut = RenderComponent<CreateGameModal>(parameters => parameters
            .Add(p => p.IsVisible, true) // Mache das Modal für den Test sichtbar
        );

        // Act
        // Finde den "Spiel erstellen"-Button im gerenderten HTML
        var createButton = cut.Find("button.btn-primary");

        // Assert
        // Überprüfe, ob der Button das "disabled"-Attribut hat
        Assert.True(createButton.HasAttribute("disabled"));
    }

    [Fact]
    public void OnCreateGameIsInvokedWithCorrectParametersWhenFormIsValid()
    {
        // Arrange
        CreateGameParameters? capturedParameters = null; // Eine Variable, um die Daten des Events aufzufangen

        // Rendere die Komponente und richte einen Event-Listener ein
        var cut = RenderComponent<CreateGameModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            // Wenn das OnCreateGame-Event ausgelöst wird, speichere die Parameter
            .Add(p => p.OnCreateGame, args => capturedParameters = args)
        );

        // Finde die Eingabefelder und den Button
        var nameInput = cut.Find("#playerName");
        var colorSelect = cut.Find("#playerColor");
        var timeSelect = cut.Find("#initialTime");
        var createButton = cut.Find("button.btn-primary");

        // Act
        // Simuliere Benutzereingaben
        nameInput.Input("Gandalf"); // Tippe einen Namen ein
        colorSelect.Change(Player.Black); // Wähle Schwarz als Farbe
        timeSelect.Change(5); // Wähle 5 Minuten

        // Simuliere einen Klick auf den Button
        createButton.Click();

        // Assert
        // Überprüfe, ob das Event ausgelöst wurde (d.h. die capturedParameters sind nicht mehr null)
        Assert.NotNull(capturedParameters);
        // Überprüfe, ob die Daten im Event korrekt sind
        Assert.Equal("Gandalf", capturedParameters.Name);
        Assert.Equal(Player.Black, capturedParameters.Color);
        Assert.Equal(5, capturedParameters.TimeMinutes);
    }
}