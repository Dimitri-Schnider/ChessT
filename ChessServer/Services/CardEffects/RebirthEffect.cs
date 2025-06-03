using System;
using System.Collections.Generic;
using System.Linq;
using ChessLogic;
using ChessLogic.Utilities;
using ChessNetwork.Configuration;
using ChessNetwork.DTOs;
using Chess.Logging;

namespace ChessServer.Services.CardEffects
{
    public class RebirthEffect : ICardEffect
    {
        private readonly IChessLogger _logger;

        public RebirthEffect(IChessLogger logger)
        {
            _logger = logger;
        }

        private static Piece CreateNewPieceByType(PieceType type, Player color)
        {
            switch (type)
            {
                case PieceType.Queen: return new Queen(color);
                case PieceType.Rook: return new Rook(color);
                case PieceType.Bishop: return new Bishop(color);
                case PieceType.Knight: return new Knight(color);
                default: throw new ArgumentException($"Ungültiger oder nicht unterstützter Figurentyp für Wiederbelebung: {type}");
            }
        }

        public CardActivationResult Execute(GameSession session, Guid playerId, Player playerDataColor,
                                            string cardTypeId,
                                            string? fromSquareAlg,
                                            string? toSquareAlg)
        {
            string? pieceTypeToReviveStringInternal = fromSquareAlg;
            string? targetRevivalSquareAlgInternal = toSquareAlg;

            if (cardTypeId != CardConstants.Wiedergeburt)
            {
                return new CardActivationResult(false, ErrorMessage: $"RebirthEffect fälschlicherweise für Karte {cardTypeId} aufgerufen.");
            }

            if (string.IsNullOrEmpty(pieceTypeToReviveStringInternal) || string.IsNullOrEmpty(targetRevivalSquareAlgInternal))
            {
                return new CardActivationResult(false, ErrorMessage: "Informationen zur wiederzubelebenden Figur oder zum Zielfeld fehlen. Bitte wähle eine Figur und ein Feld.");
            }

            PieceType pieceTypeToRevive;
            if (!Enum.TryParse<PieceType>(pieceTypeToReviveStringInternal, true, out pieceTypeToRevive))
            {
                _logger.LogRebirthEffectFailedString($"Ungültiger Figurentyp: {pieceTypeToReviveStringInternal}", pieceTypeToReviveStringInternal, targetRevivalSquareAlgInternal, session.GameId);
                return new CardActivationResult(false, ErrorMessage: $"Ungültiger Figurentyp angegeben: {pieceTypeToReviveStringInternal}");
            }

            if (pieceTypeToRevive == PieceType.Pawn)
            {
                _logger.LogRebirthEffectFailedEnum("Bauern können nicht wiederbelebt werden.", pieceTypeToRevive, targetRevivalSquareAlgInternal, session.GameId);
                return new CardActivationResult(false, ErrorMessage: "Bauern können nicht wiederbelebt werden.");
            }

            Position targetRevivalSquare;
            try
            {
                targetRevivalSquare = GameSession.ParsePos(targetRevivalSquareAlgInternal);
            }
            catch (ArgumentException ex)
            {
                _logger.LogRebirthEffectFailedEnum($"Ungültiges Zielquadrat: {targetRevivalSquareAlgInternal} ({ex.Message})", pieceTypeToRevive, targetRevivalSquareAlgInternal, session.GameId);
                return new CardActivationResult(false, ErrorMessage: $"Ungültiges Zielquadrat angegeben: {targetRevivalSquareAlgInternal}");
            }

            List<Position> possibleOriginalSquares = PieceHelper.GetOriginalStartSquares(pieceTypeToRevive, playerDataColor);
            if (!possibleOriginalSquares.Any(s => s.Row == targetRevivalSquare.Row && s.Column == targetRevivalSquare.Column))
            {
                _logger.LogRebirthEffectFailedEnum($"{targetRevivalSquareAlgInternal} ist kein gültiges Ursprungsfeld für {pieceTypeToRevive}.", pieceTypeToRevive, targetRevivalSquareAlgInternal, session.GameId);
                return new CardActivationResult(true, ErrorMessage: $"{targetRevivalSquareAlgInternal} ist kein gültiges Ursprungsfeld für {pieceTypeToRevive}. Karte verbraucht.", EndsPlayerTurn: true, BoardUpdatedByCardEffect: false);
            }

            if (!session.HasCapturedPieceOfType(playerDataColor, pieceTypeToRevive))
            {
                _logger.LogRebirthEffectFailedEnum($"Spieler hat keinen geschlagenen {pieceTypeToRevive}.", pieceTypeToRevive, targetRevivalSquareAlgInternal, session.GameId);
                return new CardActivationResult(true, ErrorMessage: $"Du hast keinen geschlagenen {pieceTypeToRevive}, der wiederbelebt werden könnte. Karte verbraucht.", EndsPlayerTurn: true, BoardUpdatedByCardEffect: false);
            }

            if (!session.CurrentGameState.Board.IsEmpty(targetRevivalSquare))
            {
                _logger.LogRebirthEffectFailedEnum($"Zielfeld {targetRevivalSquareAlgInternal} ist besetzt.", pieceTypeToRevive, targetRevivalSquareAlgInternal, session.GameId);
                return new CardActivationResult(true, ErrorMessage: $"Pech gehabt! Das Feld {targetRevivalSquareAlgInternal} ist besetzt. Karte verbraucht.", EndsPlayerTurn: true, BoardUpdatedByCardEffect: false);
            }

            Board boardCopy = session.CurrentGameState.Board.Copy();
            Piece revivedPieceForCheck = CreateNewPieceByType(pieceTypeToRevive, playerDataColor);
            revivedPieceForCheck.HasMoved = true;
            boardCopy[targetRevivalSquare] = revivedPieceForCheck;
            if (boardCopy.IsInCheck(playerDataColor))
            {
                _logger.LogRebirthEffectFailedEnum("Wiederbelebung würde eigenen König ins Schach stellen.", pieceTypeToRevive, targetRevivalSquareAlgInternal, session.GameId);
                return new CardActivationResult(false, ErrorMessage: "Wiederbelebung nicht möglich, da eigener König dadurch ins Schach geraten würde.");
            }

            Piece revivedPiece = CreateNewPieceByType(pieceTypeToRevive, playerDataColor);
            revivedPiece.HasMoved = true;
            session.CurrentGameState.Board[targetRevivalSquare] = revivedPiece;
            session.RemoveCapturedPieceOfType(playerDataColor, pieceTypeToRevive);
            _logger.LogRebirthEffectExecuted(pieceTypeToRevive, targetRevivalSquareAlgInternal, playerDataColor, playerId, session.GameId);
            var affectedSquares = new List<AffectedSquareInfo>
            {
                new AffectedSquareInfo { Square = targetRevivalSquareAlgInternal, Type = "card-rebirth" }
            };
            return new CardActivationResult(true, BoardUpdatedByCardEffect: true, AffectedSquaresByCard: affectedSquares);
        }
    }
}