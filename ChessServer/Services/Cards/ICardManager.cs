using ChessLogic;
using ChessNetwork.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChessServer.Services.Cards
{
    // Definiert den Vertrag für einen Dienst, der alle Aspekte des Kartensystems in einer Spielsitzung verwaltet.
    // Dies entkoppelt die Kartenlogik von der `GameSession`-Klasse.
    public interface ICardManager
    {
        void InitializeDecksForPlayer(Guid playerId, int initialTimeMinutes);                           // Initialisiert die Decks für einen Spieler zu Beginn des Spiels.
        List<CardDto> GetPlayerHand(Guid playerId);                                                     // Ruft die aktuellen Handkarten eines Spielers ab.
        int GetDrawPileCount(Guid playerId);                                                            // Ruft die Anzahl der verbleibenden Karten im Nachziehstapel eines Spielers ab.
        IEnumerable<CapturedPieceTypeDto> GetCapturedPieceTypesOfPlayer(Player playerColor);            // Ruft die Typen der vom Gegner geschlagenen Figuren eines Spielers ab (relevant für "Wiedergeburt").
        CardDto? GetCardDefinitionForAnimation(string cardTypeId);                                      // Holt die Basis-Definition einer Karte für Animationszwecke.
        Task<ServerCardActivationResultDto> ActivateCard(Guid playerId, ActivateCardRequestDto dto);    // Startet den Prozess der Kartenaktivierung und gibt das Ergebnis zurück.
        void IncrementPlayerMoveCount(Guid playerId);                                                   // Erhöht den Zugzähler eines Spielers (relevant für das Ziehen von Karten).
        (bool ShouldDraw, CardDto? DrawnCard) CheckAndProcessCardDraw(Guid playerId);                   // Prüft, ob ein Spieler eine Karte ziehen darf, und führt den Zug ggf. aus.
        void AddCapturedPiece(Player ownerColor, PieceType pieceType);                                  // Fügt eine geschlagene Figur der Sammlung hinzu.
        bool IsCardUsableGlobal(Guid playerId, string cardTypeId);                                      // Prüft, ob eine global limitierte Karte (z.B. "Extrazug") von einem Spieler bereits verwendet wurde.
        void MarkCardAsUsedGlobal(Guid playerId, string cardTypeId);                                    // Markiert eine global limitierte Karte als verbraucht.
        void SetPendingCardEffectForNextMove(Guid playerId, string cardTypeId);                         // Setzt einen Karteneffekt als anstehend für den nächsten Zug (z.B. "Extrazug").
        string? PeekPendingCardEffect(Guid playerId);                                                   // Überprüft, ob ein Effekt für den nächsten Zug ansteht, ohne ihn zu entfernen.
        void ClearPendingCardEffect(Guid playerId);                                                     // Entfernt einen anstehenden Effekt, nachdem er angewendet wurde.
        bool RemoveCardFromPlayerHand(Guid playerId, Guid cardInstanceIdToRemove);                      // Entfernt eine spezifische Karte aus der Hand eines Spielers.
        void AddCardToPlayerHand(Guid playerId, CardDto cardToAdd);                                     // Fügt eine Karte zur Hand eines Spielers hinzu (z.B. beim Kartentausch).
        void RemoveCapturedPieceOfType(Player ownerColor, PieceType type);                              // Entfernt eine wiederbelebte Figur aus der Sammlung der geschlagenen Figuren.
        CardDto? DrawCardForPlayer(Guid playerId);                                                      // Zieht die oberste Karte vom Deck eines Spielers und fügt sie seiner Hand hinzu.
    }
}