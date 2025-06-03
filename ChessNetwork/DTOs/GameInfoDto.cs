using System;
using ChessLogic;

namespace ChessNetwork.DTOs
{
    // DTO für grundlegende Spielinformationen.
    // Wird z.B. für beitretende Spieler verwendet.
    public record GameInfoDto(
        Guid CreatorId,         // ID des Spielerstellers.
        Player CreatorColor,    // Ursprünglich gewählte Farbe des Erstellers.
        bool HasOpponent        // Gibt an, ob ein Gegner beigetreten ist.
    );
}