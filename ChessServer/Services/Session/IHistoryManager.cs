using ChessLogic;
using ChessNetwork.DTOs;
using System;

namespace ChessServer.Services.Session
{
    // Definiert den Vertrag für einen Dienst, der den Spielverlauf verwaltet.
    public interface IHistoryManager
    {
        void AddMove(PlayedMoveDto move);                                   // Fügt einen ausgeführten Zug zur Historie hinzu.
        void AddPlayedCard(PlayedCardDto card, bool boardWasUpdatedByCard); // Fügt eine gespielte Karte zur Historie hinzu.
        void UpdateOnGameOver(Result result);                               // Aktualisiert die Historie mit dem Endergebnis des Spiels.
        GameHistoryDto GetGameHistory(IPlayerManager playerManager);        // Ruft das vollständige DTO des Spielverlaufs ab.
        int GetMoveCount();                                                 // Ruft die aktuelle Anzahl der Züge ab.
    }
}