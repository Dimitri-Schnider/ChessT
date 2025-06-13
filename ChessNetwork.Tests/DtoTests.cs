using ChessLogic;
using ChessNetwork.DTOs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Xunit;

namespace ChessNetwork.Tests
{
    /// <summary>
    /// Diese Klasse testet die korrekte Erstellung und Initialisierung
    /// der Data Transfer Objects (DTOs) aus dem ChessNetwork-Projekt.
    /// Zusätzlich werden die Validierungsattribute der DTOs überprüft.
    /// </summary>
    public class DtoTests
    {
        // Hilfsmethode, um die Validierungsattribute eines Objekts zu testen.
        private (bool IsValid, List<ValidationResult> Results) ValidateModel(object model)
        {
            var validationResults = new List<ValidationResult>();
            var vc = new ValidationContext(model, null, null);
            var isValid = Validator.TryValidateObject(model, vc, validationResults, true);
            return (isValid, validationResults);
        }

        [Fact]
        public void CreateGameDtoCreationInitializesPropertiesCorrectly()
        {
            // Arrange
            var expectedPlayerName = "Magnus";
            var expectedColor = Player.White;
            var expectedMinutes = 15;
            var expectedOpponentType = "Human";
            var expectedDifficulty = "Medium";

            // Act
            var createGameDto = new CreateGameDto
            {
                PlayerName = expectedPlayerName,
                Color = expectedColor,
                InitialMinutes = expectedMinutes,
                OpponentType = expectedOpponentType,
                ComputerDifficulty = expectedDifficulty
            };

            // Assert
            Assert.Equal(expectedPlayerName, createGameDto.PlayerName);
            Assert.Equal(expectedColor, createGameDto.Color);
            Assert.Equal(expectedMinutes, createGameDto.InitialMinutes);
            Assert.Equal(expectedOpponentType, createGameDto.OpponentType);
            Assert.Equal(expectedDifficulty, createGameDto.ComputerDifficulty);
        }

        [Fact]
        public void MoveDtoWithValidDataIsValid()
        {
            // Arrange
            var moveDto = new MoveDto("e2", "e4", Guid.NewGuid());

            // Act
            var validation = ValidateModel(moveDto);

            // Assert
            Assert.True(validation.IsValid);
        }

        [Theory]
        [InlineData("a9")] // Ungültige Zeile
        [InlineData("i1")] // Ungültige Spalte
        [InlineData("e4 ")] // Leerzeichen
        [InlineData("e")]   // Unvollständig
        public void MoveDtoWithInvalidFromSquareIsInvalid(string from)
        {
            // Arrange
            var moveDto = new MoveDto(from, "e4", Guid.NewGuid());

            // Act
            var validation = ValidateModel(moveDto);

            // Assert
            Assert.False(validation.IsValid);
            Assert.Contains(validation.Results, r => r.MemberNames.Contains("From"));
        }

        [Fact]
        public void MoveDtoWithEmptyPlayerIdIsInvalid()
        {
            // Arrange
            var moveDto = new MoveDto("a1", "h8", Guid.Empty);

            // Act
            var validation = ValidateModel(moveDto);

            // Assert
            Assert.False(validation.IsValid);
            Assert.Contains(validation.Results, r => r.MemberNames.Contains("PlayerId"));
        }

        [Fact]
        public void ActivateCardRequestDtoWithInvalidSquareIsInvalid()
        {
            // Arrange
            var requestDto = new ActivateCardRequestDto
            {
                CardInstanceId = Guid.NewGuid(),
                CardTypeId = "teleport",
                FromSquare = "z9" // Ungültige Koordinate
            };

            // Act
            var validation = ValidateModel(requestDto);

            // Assert
            Assert.False(validation.IsValid);
            Assert.Contains(validation.Results, r => r.MemberNames.Contains("FromSquare"));
        }

        [Fact]
        public void ActivateCardRequestDtoWithMissingRequiredFieldsIsInvalid()
        {
            // Arrange
            var requestDto = new ActivateCardRequestDto
            {
                CardInstanceId = Guid.Empty, // Wird von IValidatableObject abgefangen
                CardTypeId = ""  // Wird von IValidatableObject abgefangen
            };

            // Act
            var validation = ValidateModel(requestDto);

            // Assert
            Assert.False(validation.IsValid);
            Assert.Equal(2, validation.Results.Count); // Erwartet zwei Validierungsfehler
            Assert.Contains(validation.Results, r => r.MemberNames.Contains("CardInstanceId"));
            Assert.Contains(validation.Results, r => r.MemberNames.Contains("CardTypeId"));
        }

        [Fact]
        public void CardDtoInitializesCorrectly()
        {
            // Arrange & Act
            var card = new CardDto
            {
                InstanceId = Guid.NewGuid(),
                Id = "test_card",
                Name = "Test Card",
                Description = "A card for testing.",
                ImageUrl = "img/test.png",
                AnimationDelayMs = 1000
            };

            // Assert
            Assert.NotEqual(Guid.Empty, card.InstanceId);
            Assert.Equal("test_card", card.Id);
            Assert.Equal("Test Card", card.Name);
            Assert.Equal("A card for testing.", card.Description);
            Assert.Equal("img/test.png", card.ImageUrl);
            Assert.Equal(1000, card.AnimationDelayMs);
        }

        [Fact]
        public void MoveResultDtoInitializesCorrectly()
        {
            // Arrange & Act
            var board = new BoardDto(new PieceDto?[8][]);
            var squares = new List<AffectedSquareInfo> { new() { Square = "a1", Type = "test" } };
            var result = new MoveResultDto
            {
                IsValid = true,
                ErrorMessage = null,
                NewBoard = board,
                IsYourTurn = false,
                Status = GameStatusDto.Check,
                PlayerIdToSignalCardDraw = Guid.NewGuid(),
                NewlyDrawnCard = new CardDto { Id = "new_card", Name = "New", Description = "Desc", ImageUrl = "url" },
                LastMoveFrom = "e2",
                LastMoveTo = "e4",
                CardEffectSquares = squares
            };

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
            Assert.Same(board, result.NewBoard);
            Assert.False(result.IsYourTurn);
            Assert.Equal(GameStatusDto.Check, result.Status);
            Assert.NotNull(result.PlayerIdToSignalCardDraw);
            Assert.NotNull(result.NewlyDrawnCard);
            Assert.Equal("e2", result.LastMoveFrom);
            Assert.Equal("e4", result.LastMoveTo);
            Assert.Same(squares, result.CardEffectSquares);
        }

        [Fact]
        public void GameHistoryDtoInitializesCorrectly()
        {
            // Arrange & Act
            var history = new GameHistoryDto
            {
                GameId = Guid.NewGuid(),
                PlayerWhiteName = "Alice",
                PlayerBlackName = "Bob",
                Winner = Player.White,
                ReasonForGameEnd = EndReason.Checkmate,
                DateTimeStartedUtc = DateTime.UtcNow,
                DateTimeEndedUtc = DateTime.UtcNow.AddMinutes(20),
                Moves = new List<PlayedMoveDto> { new() { MoveNumber = 1, From = "e2", To = "e4" } },
                PlayedCards = new List<PlayedCardDto> { new() { CardId = "teleport", CardName = "Teleport", PlayerName = "Alice" } }
            };

            // Assert
            Assert.NotEqual(Guid.Empty, history.GameId);
            Assert.Equal("Alice", history.PlayerWhiteName);
            Assert.Equal("Bob", history.PlayerBlackName);
            Assert.Equal(Player.White, history.Winner);
            Assert.Equal(EndReason.Checkmate, history.ReasonForGameEnd);
            Assert.True(history.DateTimeEndedUtc > history.DateTimeStartedUtc);
            Assert.Single(history.Moves);
            Assert.Single(history.PlayedCards);
        }

        [Fact]
        public void ServerCardActivationResultDtoInitializesCorrectly()
        {
            // Arrange & Act
            var result = new ServerCardActivationResultDto
            {
                Success = true,
                CardId = "card_swap",
                EndsPlayerTurn = false,
                BoardUpdatedByCardEffect = true,
                PawnPromotionPendingAt = new PositionDto(0, 4)
            };

            // Assert
            Assert.True(result.Success);
            Assert.Equal("card_swap", result.CardId);
            Assert.False(result.EndsPlayerTurn);
            Assert.True(result.BoardUpdatedByCardEffect);
            Assert.NotNull(result.PawnPromotionPendingAt);
            Assert.Equal(0, result.PawnPromotionPendingAt.Row);
        }

        [Fact]
        public void TimeUpdateDtoInitializesCorrectly()
        {
            // Arrange & Act
            var timeUpdate = new TimeUpdateDto(
                TimeSpan.FromSeconds(150),
                TimeSpan.FromSeconds(120),
                Player.Black
            );

            // Assert
            Assert.Equal(150, timeUpdate.WhiteTime.TotalSeconds);
            Assert.Equal(120, timeUpdate.BlackTime.TotalSeconds);
            Assert.Equal(Player.Black, timeUpdate.PlayerWhoseTurnItIs);
        }
    }
}