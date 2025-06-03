using System;
using System.Collections.Generic;
using ChessLogic;

namespace ChessNetwork.DTOs
{
    // DTO für den Verlauf eines Schachspiels.
    public class GameHistoryDto
    {
        // Eindeutige ID des Spiels.
        public Guid GameId { get; set; }
        // Name des weissen Spielers.
        public string? PlayerWhiteName { get; set; }
        // ID des weissen Spielers.
        public Guid? PlayerWhiteId { get; set; }
        // Name des schwarzen Spielers.
        public string? PlayerBlackName { get; set; }
        // ID des schwarzen Spielers.
        public Guid? PlayerBlackId { get; set; }
        // Initiale Bedenkzeit pro Spieler in Minuten.
        public int InitialTimeMinutes { get; set; }
        // Gewinner des Spiels; null bei laufendem Spiel oder Remis.
        public Player? Winner { get; set; }
        // Grund für das Spielende; null bei laufendem Spiel.
        public EndReason? ReasonForGameEnd { get; set; }
        // UTC-Zeitstempel des Spielbeginns.
        public DateTime DateTimeStartedUtc { get; set; }
        // UTC-Zeitstempel des Spielendes; null bei laufendem Spiel.
        public DateTime? DateTimeEndedUtc { get; set; }
        // Liste aller getätigten Züge.
        public List<PlayedMoveDto> Moves { get; set; } = new List<PlayedMoveDto>();
        // Liste aller aktivierten Karten.
        public List<PlayedCardDto> PlayedCards { get; set; } = new List<PlayedCardDto>();
    }
}