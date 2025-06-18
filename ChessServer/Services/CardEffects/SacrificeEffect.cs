using Chess.Logging;
using ChessLogic;
using ChessLogic.Utilities;
using ChessNetwork.Configuration;
using ChessNetwork.DTOs;
using System;
using System.Collections.Generic;

namespace ChessServer.Services.CardEffects
{
    // Implementiert den Karteneffekt, bei dem ein Bauer geopfert wird, um eine neue Karte zu ziehen.
    public class SacrificeEffect : ICardEffect
    {
        private readonly IChessLogger _logger;
        public SacrificeEffect(IChessLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Führt den Opfer-Effekt aus.
        public CardActivationResult Execute(GameSession session, Guid playerId, Player playerDataColor,
                                            IHistoryManager historyManager,
                                            string cardTypeId,
                                            string? fromSquareAlg,
                                            string? toSquareAlg)
        {
            if (cardTypeId != CardConstants.SacrificeEffect)
            {
                return new CardActivationResult(false, ErrorMessage: $"SacrificeEffect fälschlicherweise für Karte {cardTypeId} aufgerufen.");
            }

            if (string.IsNullOrEmpty(fromSquareAlg))
            {
                _logger.LogSacrificeEffectFailedPawnNotFound(session.GameId, playerId, fromSquareAlg ?? "NULL");
                return new CardActivationResult(false, ErrorMessage: "Kein Bauer für Opfergabe ausgewählt.");
            }

            Position pawnPos;
            try
            {
                pawnPos = GameSession.ParsePos(fromSquareAlg);
            }
            catch (ArgumentException ex)
            {
                _logger.LogSacrificeEffectFailedPawnNotFound(session.GameId, playerId, fromSquareAlg);
                return new CardActivationResult(false, ErrorMessage: $"Ungültige Koordinate für Bauernopfer: {ex.Message}");
            }

            Piece? pieceToSacrifice = session.CurrentGameState.Board[pawnPos];

            if (pieceToSacrifice == null)
            {
                _logger.LogSacrificeEffectFailedPawnNotFound(session.GameId, playerId, fromSquareAlg);
                return new CardActivationResult(false, ErrorMessage: $"Keine Figur auf dem Feld {fromSquareAlg} für Opfergabe gefunden.");
            }

            // Validiert, dass die geopferte Figur ein Bauer der eigenen Farbe ist.
            if (pieceToSacrifice.Type != PieceType.Pawn)
            {
                _logger.LogSacrificeEffectFailedNotAPawn(session.GameId, playerId, fromSquareAlg, pieceToSacrifice.Type);
                return new CardActivationResult(false, ErrorMessage: $"Die Figur auf {fromSquareAlg} ist kein Bauer und kann nicht geopfert werden.");
            }

            if (pieceToSacrifice.Color != playerDataColor)
            {
                _logger.LogSacrificeEffectFailedWrongColor(session.GameId, playerId, fromSquareAlg, pieceToSacrifice.Color);
                return new CardActivationResult(false, ErrorMessage: $"Der Bauer auf {fromSquareAlg} gehört nicht dir.");
            }

            // Führt eine hypothetische Opferung durch, um auf Selbst-Schach zu prüfen.
            Board boardCopy = session.CurrentGameState.Board.Copy();
            boardCopy[pawnPos] = null;
            if (boardCopy.IsInCheck(playerDataColor))
            {
                _logger.LogSacrificeEffectFailedWouldCauseCheck(session.GameId, playerId, fromSquareAlg);
                return new CardActivationResult(false, ErrorMessage: "Opfergabe nicht möglich, da dies deinen König ins Schach stellen würde.");
            }

            // Protokolliert die Aktion im Spielverlauf.
            historyManager.AddMove(new PlayedMoveDto
            {
                PlayerId = playerId,
                PlayerColor = playerDataColor,
                From = fromSquareAlg,
                To = "off-board", // Spezieller Wert für Opfergabe
                ActualMoveType = MoveType.Sacrifice,
                PieceMoved = $"{pieceToSacrifice.Color} {pieceToSacrifice.Type}",
                TimestampUtc = DateTime.UtcNow,
                TimeTaken = TimeSpan.Zero,
                RemainingTimeWhite = session.TimerService.GetCurrentTimeForPlayer(Player.White),
                RemainingTimeBlack = session.TimerService.GetCurrentTimeForPlayer(Player.Black)
            });
            // Entfernt den Bauern vom Brett.
            session.CurrentGameState.Board[pawnPos] = null;
            _logger.LogSacrificeEffectExecuted(session.GameId, playerId, fromSquareAlg);
            // Gibt ein erfolgreiches Ergebnis zurück und signalisiert, dass der Spieler eine neue Karte ziehen soll.
            return new CardActivationResult(
                Success: true,
                EndsPlayerTurn: true,
                PlayerIdToSignalDraw: playerId,
                BoardUpdatedByCardEffect: true,
                AffectedSquaresByCard: new List<AffectedSquareInfo> { new AffectedSquareInfo { Square = fromSquareAlg, Type = "card-sacrifice" } }
            );
        }
    }
}