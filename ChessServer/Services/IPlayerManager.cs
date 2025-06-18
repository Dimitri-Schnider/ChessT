using ChessLogic;
using ChessNetwork.DTOs;
using System;
using System.Collections.Generic;

namespace ChessServer.Services
{
    // Definiert den Vertrag für einen Dienst, der die Spieler innerhalb einer einzelnen Spielsitzung verwaltet.
    // Dies entkoppelt die Spieler-Logik (IDs, Namen, Farben, Gegner-Typ) von der `GameSession`.
    public interface IPlayerManager
    {
        int PlayerCount { get; }                                                                // Die aktuelle Anzahl der Spieler in der Sitzung.
        bool HasOpponent { get; }                                                               // Gibt an, ob bereits ein Gegner beigetreten ist.
        Guid FirstPlayerId { get; }                                                             // Die ID des ersten Spielers, der der Sitzung beigetreten ist (der Ersteller).
        Player FirstPlayerColor { get; }                                                        // Die Farbe des ersten Spielers.
        OpponentType OpponentType { get; }                                                      // Der Typ des Gegners ("Human" oder "Computer").
        Guid? ComputerPlayerId { get; }                                                         // Die ID des Computergegners, falls vorhanden.
        ComputerDifficulty ComputerDifficulty { get; }                                          // Die Schwierigkeitsstufe des Computergegners, falls vorhanden.
        (Guid PlayerId, Player Color) Join(string playerName, Player? preferredColor = null);   // Fügt einen Spieler der Sitzung hinzu und weist ihm eine Farbe zu.
        Player GetPlayerColor(Guid playerId);                                                   // Ruft die Farbe eines Spielers anhand seiner ID ab.
        Guid? GetPlayerIdByColor(Player color);                                                 // Ruft die ID eines Spielers anhand seiner Farbe ab.
        string? GetPlayerName(Guid playerId);                                                   // Ruft den Namen eines Spielers anhand seiner ID ab.
        OpponentInfoDto? GetOpponentDetails(Guid currentPlayerId);                              // Ruft Informationen über den Gegner des aktuellen Spielers ab.
    }
}