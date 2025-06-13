namespace ChessNetwork.DTOs
{
    // Definiert die Kategorien, denen Spielkarten zugeordnet werden können.
    public enum CardCategory
    {
        Gameplay, // Beeinflusst direkt Figuren oder Züge auf dem Brett.
        Time,     // Manipuliert die Bedenkzeit der Spieler.
        Utility   // Bietet andere Vorteile, z.B. Kartenmanagement.
    }
}