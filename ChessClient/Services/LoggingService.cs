namespace ChessClient.Services
{
    // Repräsentiert einen einzelnen Log-Eintrag.
    public record LogEntry(DateTime Timestamp, string RequestInfo, string ResponseInfo);

    // Dienst zum Verwalten und Anzeigen von API-Log-Einträgen.
    public class LoggingService
    {
        // Private Liste der Log-Einträge.
        private readonly List<LogEntry> _entries = new();
        // Öffentliche, schreibgeschützte Ansicht der Log-Einträge.
        public IReadOnlyList<LogEntry> Entries => _entries;
        // Ereignis, das ausgelöst wird, wenn sich die Log-Einträge ändern.
        public event Action? OnChange;
        // Gibt an, ob das Logging pausiert ist.
        public bool IsPaused { get; private set; }
        // Gibt an, ob Polling-Requests in die Logs aufgenommen werden sollen.
        public bool IncludePolling { get; private set; } = true;

        // Fügt einen neuen Log-Eintrag hinzu.
        public void Add(string req, string res, bool isPolling = false)
        {
            if (IsPaused) return;
            if (isPolling && !IncludePolling) return;
            _entries.Insert(0, new LogEntry(DateTime.Now, req, res));
            OnChange?.Invoke();
        }

        // Löscht alle Log-Einträge.
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

        // Schaltet die Aufnahme von Polling-Requests in die Logs um.
        public void TogglePolling()
        {
            IncludePolling = !IncludePolling;
            OnChange?.Invoke();
        }
    }
}