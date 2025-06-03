namespace ChessClient.Configuration
{
    public static class ClientConstants
    {
        // Die DefaultServerBaseUrl Konstante wurde entfernt oder auskommentiert, da sie nun aus appsettings kommt.
        // public const string DefaultServerBaseUrl = "https://localhost:7144";

        // Relativer Pfad zum SignalR Chess Hub auf dem Server.
        public const string ChessHubRelativePath = "/chessHub";

        // Fallback-URL, falls nichts in der Konfiguration gefunden wird (optional, aber hilfreich für den Übergang)
        public const string DefaultServerBaseUrl = "https://localhost:7144";
    }
}