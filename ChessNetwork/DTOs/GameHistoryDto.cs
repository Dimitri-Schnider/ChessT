using ChessLogic;
using System;
using System.Collections.Generic;

namespace ChessNetwork.DTOs
{
    // DTO für den detaillierten Verlauf eines Spiels.
    public class GameHistoryDto
    {
        public Guid GameId { get; set; }                                // Eindeutige ID des Spiels.
        public string? PlayerWhiteName { get; set; }                    // Name des weissen Spielers.
        public Guid? PlayerWhiteId { get; set; }                        // ID des weissen Spielers.
        public string? PlayerBlackName { get; set; }                    // Name des schwarzen Spielers.
        public Guid? PlayerBlackId { get; set; }                        // ID des schwarzen Spielers.
        public int InitialTimeMinutes { get; set; }                     // Initiale Bedenkzeit pro Spieler in Minuten.
        public Player? Winner { get; set; }                             // Gewinner des Spiels; null bei laufendem Spiel oder Remis.
        public EndReason? ReasonForGameEnd { get; set; }                // Grund für das Spielende; null bei laufendem Spiel.
        public DateTime DateTimeStartedUtc { get; set; }                // UTC-Zeitstempel des Spielbeginns.
        public DateTime? DateTimeEndedUtc { get; set; }                 // UTC-Zeitstempel des Spielendes; null bei laufendem Spiel.
        public List<PlayedMoveDto> Moves { get; set; } = new();         // Liste aller getätigten Züge.
        public List<PlayedCardDto> PlayedCards { get; set; } = new();   // Liste aller aktivierten Karten.
    }
}