using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ChessClient.Services
{
    // Ein DelegatingHandler, der HTTP-Anfragen und -Antworten für Debugging-Zwecke protokolliert.
    public class LoggingHandler : DelegatingHandler
    {
        private readonly LoggingService _logger;

        // Injiziert den LoggingService.
        public LoggingHandler(LoggingService logger) => _logger = logger;

        // Fängt ausgehende HTTP-Anfragen ab, um sie zu protokollieren.
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Erkennt, ob es sich um eine wiederholte Polling-Anfrage handelt.
            var path = request.RequestUri?.AbsolutePath ?? "";
            bool isPolling =
            request.Method == HttpMethod.Get
                && path.StartsWith("/api/games/", StringComparison.Ordinal)
                && (path.EndsWith("/state", StringComparison.Ordinal)
                || path.EndsWith("/status", StringComparison.Ordinal));

            // Protokolliert die Anfrage-Informationen.
            var reqBody = request.Content is not null ? await request.Content.ReadAsStringAsync(cancellationToken) : "<no content>";
            var reqInfo = $"{request.Method} {request.RequestUri}\nBody: {reqBody}";

            // Leitet die Anfrage an den nächsten Handler in der Kette weiter.
            var response = await base.SendAsync(request, cancellationToken);

            // Protokolliert die Antwort-Informationen.
            var resBody = response.Content is not null ? await response.Content.ReadAsStringAsync(cancellationToken) : "<no content>";
            var resInfo = $"Status {(int)response.StatusCode} {response.StatusCode}\nBody: {resBody}";

            // Fügt den Eintrag dem Logging-Dienst hinzu.
            _logger.Add(reqInfo, resInfo, isPolling);

            return response;
        }
    }
}