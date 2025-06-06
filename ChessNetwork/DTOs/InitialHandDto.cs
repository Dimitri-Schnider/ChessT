using System.Collections.Generic;

namespace ChessNetwork.DTOs
{
    // DTO zur Übermittlung der Starthand eines Spielers und der Grösse seines Nachziehstapels.
    public record InitialHandDto(

        List<CardDto> Hand, // Eine Liste der Karten, die der Spieler auf der Hand hat.
        int DrawPileCount   // Die Anzahl der verbleibenden Karten im Nachziehstapel des Spielers.
    );
}