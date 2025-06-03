using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ChessClient.Services
{
    // Ein DelegatingHandler, der HTTP-Anfragen und -Antworten protokolliert.
    public class LoggingHandler : DelegatingHandler
    {
        // Der Dienst zum Speichern der Logs.
        private readonly LoggingService _logger;

        // Konstruktor, injiziert den LoggingService.
        public LoggingHandler(LoggingService logger) => _logger = logger;
        // Überschreibt die SendAsync-Methode, um Anfragen und Antworten abzufangen.
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Bestimmt den Pfad der Anfrage.
            var path = request.RequestUri?.AbsolutePath ?? "";
            // Erkennt Polling-Requests anhand des Pfads und der Methode.
            bool isPolling =
            request.Method == HttpMethod.Get
                && path.StartsWith("/api/games/", StringComparison.Ordinal)
                && (path.EndsWith("/state", StringComparison.Ordinal)
                || path.EndsWith("/status", StringComparison.Ordinal));
            // Liest den Request-Body (falls vorhanden).
            var reqBody = request.Content is not null
                ? await request.Content.ReadAsStringAsync(cancellationToken)
                : "<no content>";
            var reqInfo = $"{request.Method} {request.RequestUri}\nBody: {reqBody}";

            // Sendet die Anfrage an den inneren Handler und erhält die Antwort.
            var response = await base.SendAsync(request, cancellationToken);

            // Liest den Response-Body (falls vorhanden).
            var resBody = response.Content is not null
                ? await response.Content.ReadAsStringAsync(cancellationToken)
                : "<no content>";
            var resInfo = $"Status {(int)response.StatusCode} {response.StatusCode}\nBody: {resBody}";

            // Fügt die Request- und Response-Informationen zum Logger hinzu.
            _logger.Add(reqInfo, resInfo, isPolling);
            return response;
        }
    }
}