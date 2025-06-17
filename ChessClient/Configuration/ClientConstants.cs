namespace ChessClient.Configuration
{
    // Definiert statische Konstanten, die im gesamten Client verwendet werden.
    public static class ClientConstants
    {
        public const string ChessHubRelativePath = "/chessHub";                 // Relativer Pfad zum SignalR Chess Hub auf dem Server.
        public const string DefaultServerBaseUrl = "https://localhost:7144";    // Fallback-URL für die Server-Adresse, falls nichts in der Konfiguration gefunden wird.
    }
}