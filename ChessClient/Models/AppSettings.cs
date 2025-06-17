namespace ChessClient.Models
{
    // Repräsentiert die App-Einstellungen, die aus appsettings.json geladen werden.
    public class AppSettings
    {
        // Die Basis-URL des Schach-Servers.
        public string? ServerBaseUrl { get; set; }
    }
}