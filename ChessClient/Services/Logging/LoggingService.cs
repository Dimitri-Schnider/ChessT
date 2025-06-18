namespace ChessClient.Services.Logging
{
    // Definiert einen einzelnen Log-Eintrag mit Zeitstempel und Details.
    public record LogEntry(DateTime Timestamp, string RequestInfo, string ResponseInfo);

    // Ein einfacher In-Memory-Dienst zum Speichern und Anzeigen von API-Logs.
    public class LoggingService
    {
        // Die interne Liste der Log-Einträge.
        private readonly List<LogEntry> _entries = new();

        // Eine öffentliche, schreibgeschützte Ansicht der Logs für die UI.
        public IReadOnlyList<LogEntry> Entries => _entries;

        // Event, das die UI über Änderungen informiert.
        public event Action? OnChange;

        // Gibt an, ob das Logging pausiert ist.
        public bool IsPaused { get; private set; }

        // Gibt an, ob Polling-Anfragen (z.B. für Status-Updates) angezeigt werden sollen.
        public bool IncludePolling { get; private set; } = true;

        // Fügt einen neuen Log-Eintrag hinzu.
        public void Add(string req, string res, bool isPolling = false)
        {
            // Ignoriert den Eintrag, wenn das Logging pausiert ist.
            if (IsPaused) return;
            // Ignoriert Polling-Anfragen, wenn der Filter aktiv ist.
            if (isPolling && !IncludePolling) return;

            // Fügt den neuen Eintrag am Anfang der Liste ein (neueste zuerst).
            _entries.Insert(0, new LogEntry(DateTime.Now, req, res));
            OnChange?.Invoke(); // Benachrichtigt die UI über die Änderung.
        }

        // Löscht alle vorhandenen Log-Einträge.
        public void Clear()
        {
            _entries.Clear();
            OnChange?.Invoke();
        }

        // Schaltet den Pausenzustand des Loggings um.
        public void TogglePause()
        {
            IsPaused = !IsPaused;
            OnChange?.Invoke();
        }

        // Schaltet den Filter für Polling-Anfragen um.
        public void TogglePolling()
        {
            IncludePolling = !IncludePolling;
            OnChange?.Invoke();
        }
    }
}