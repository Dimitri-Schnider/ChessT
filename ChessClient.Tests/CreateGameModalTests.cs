using Bunit;
using ChessClient.Models;
using ChessClient.Pages.Components.Dialogs;
using ChessLogic;
using ChessNetwork.DTOs;
using Microsoft.AspNetCore.Components.Web;
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
        CreateGameDto? capturedDto = null; // Eine Variable, um die Daten des Events aufzufangen

        // Rendere die Komponente und richte einen Event-Listener ein
        var cut = RenderComponent<CreateGameModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            // Wenn das OnCreateGame-Event ausgelöst wird, speichere die Parameter
            .Add(p => p.OnCreateGame, args => capturedDto = args)
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
        // Überprüfe, ob das Event ausgelöst wurde (d.h. die capturedDto sind nicht mehr null)
        Assert.NotNull(capturedDto);
        // Überprüfe, ob die Daten im Event korrekt sind
        Assert.Equal("Gandalf", capturedDto.PlayerName);
        Assert.Equal(Player.Black, capturedDto.Color);
        Assert.Equal(5, capturedDto.InitialMinutes);
        // Prüfen, ob der Standardwert für den Gegnertyp korrekt übergeben wird.
        Assert.Equal(OpponentType.Human, capturedDto.OpponentType);
    }
}