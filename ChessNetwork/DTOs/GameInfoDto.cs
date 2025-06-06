using ChessLogic;
using System;

namespace ChessNetwork.DTOs
{
    // DTO für grundlegende Spielinformationen.
    public record GameInfoDto(

        Guid CreatorId,         // Die ID des Spielers, der das Spiel erstellt hat.
        Player CreatorColor,    // Die ursprünglich vom Ersteller gewählte Farbe.
        bool HasOpponent        // Gibt an, ob bereits ein Gegner dem Spiel beigetreten ist.
    );
}