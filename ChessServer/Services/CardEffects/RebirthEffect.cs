using Chess.Logging;
using ChessLogic;
using ChessLogic.Utilities;
using ChessNetwork.Configuration;
using ChessNetwork.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessServer.Services.CardEffects
{
    // Implementiert den Karteneffekt "Wiedergeburt", der eine geschlagene Figur wiederbelebt.
    public class RebirthEffect : ICardEffect
    {
        private readonly IChessLogger _logger;
        public RebirthEffect(IChessLogger logger) { _logger = logger; }

        // Hilfsmethode, um ein neues Piece-Objekt basierend auf Typ und Farbe zu erstellen.
        private static Piece CreateNewPieceByType(PieceType type, Player color) => type switch
        {
            PieceType.Queen => new Queen(color),
            PieceType.Rook => new Rook(color),
            PieceType.Bishop => new Bishop(color),
            PieceType.Knight => new Knight(color),
            _ => throw new ArgumentException($"Ungültiger Typ für Wiederbelebung: {type}"),
        };

        // Führt den Wiedergeburts-Effekt aus.
        public CardActivationResult Execute(CardExecutionContext context)
        {
            var pieceTypeToRevive = context.RequestDto.PieceTypeToRevive;
            var targetSquare = context.RequestDto.TargetRevivalSquare;

            if (context.RequestDto.CardTypeId != CardConstants.Wiedergeburt || !pieceTypeToRevive.HasValue || targetSquare == null)
                return new CardActivationResult(false, ErrorMessage: "Ungültige Anfrage für Wiedergeburt.");

            var pieceType = pieceTypeToRevive.Value;
            var targetPos = PositionParser.ParsePos(targetSquare);

            List<Position> possibleOriginalSquares = PieceHelper.GetOriginalStartSquares(pieceType, context.PlayerColor);
            if (!possibleOriginalSquares.Any(s => s.Row == targetPos.Row && s.Column == targetPos.Column))
            {
                _logger.LogRebirthEffectFailedEnum($"{targetSquare} ist kein gültiges Ursprungsfeld für {pieceType}.", pieceType, targetSquare, context.Session.GameId);
                return new CardActivationResult(true, ErrorMessage: $"{targetSquare} ist kein gültiges Ursprungsfeld für {pieceType}. Karte verbraucht.", EndsPlayerTurn: true);
            }

            if (!context.Session.CardManager.GetCapturedPieceTypesOfPlayer(context.PlayerColor).Any(p => p.Type == pieceType))
            {
                _logger.LogRebirthEffectFailedEnum($"Spieler hat keinen geschlagenen {pieceType}.", pieceType, targetSquare, context.Session.GameId);
                return new CardActivationResult(true, ErrorMessage: $"Du hast keinen geschlagenen {pieceType} zum Wiederbeleben.", EndsPlayerTurn: true);
            }

            if (!context.Session.CurrentGameState.Board.IsEmpty(targetPos))
                return new CardActivationResult(true, ErrorMessage: $"Feld {targetSquare} ist besetzt.", EndsPlayerTurn: true);

            Board boardCopy = context.Session.CurrentGameState.Board.Copy();
            boardCopy[targetPos] = CreateNewPieceByType(pieceType, context.PlayerColor);
            if (boardCopy.IsInCheck(context.PlayerColor))
                return new CardActivationResult(false, ErrorMessage: "Wiederbelebung nicht möglich, da König im Schach stehen würde.");

            var newPiece = CreateNewPieceByType(pieceType, context.PlayerColor);
            context.Session.CurrentGameState.Board[targetPos] = newPiece;
            context.Session.CardManager.RemoveCapturedPieceOfType(context.PlayerColor, pieceType);

            context.HistoryManager.AddMove(new PlayedMoveDto
            {
                PlayerId = context.PlayerId,
                PlayerColor = context.PlayerColor,
                From = "graveyard",
                To = targetSquare,
                ActualMoveType = MoveType.Rebirth,
                PieceMoved = $"{newPiece.Color} {newPiece.Type}",
                TimestampUtc = DateTime.UtcNow,
                TimeTaken = TimeSpan.Zero,
                RemainingTimeWhite = context.Session.TimerService.GetCurrentTimeForPlayer(Player.White),
                RemainingTimeBlack = context.Session.TimerService.GetCurrentTimeForPlayer(Player.Black)
            });
            _logger.LogRebirthEffectExecuted(pieceType, targetSquare, context.PlayerColor, context.PlayerId, context.Session.GameId);
            var affectedSquares = new List<AffectedSquareInfo> { new() { Square = targetSquare, Type = "card-rebirth" } };
            return new CardActivationResult(true, BoardUpdatedByCardEffect: true, AffectedSquaresByCard: affectedSquares);
        }
    }
}