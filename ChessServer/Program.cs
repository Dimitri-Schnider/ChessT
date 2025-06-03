// File: [SolutionDir]\ChessServer\Program.cs
// File: [SolutionDir]/ChessServer/Program.cs
using ChessServer.Hubs;
using ChessServer.Services;
using ChessServer.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Chess.Logging; // Hinzufuegen fuer IChessLogger und ChessLogger
using Microsoft.Extensions.Logging; // Hinzufuegen fuer ILogger<T>

var builder = WebApplication.CreateBuilder(args);
// Konfiguriert Dienste fuer die Anwendung.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS-Konfiguration anpassen
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://schacht.app", // Deine Azure-Domain
                           "https://localhost:7224", // Typischer ChessClient Kestrel HTTPS Port (pruefe deine launchSettings.json)
                           "http://localhost:5170",  // Typischer ChessClient Kestrel HTTP Port (pruefe deine launchSettings.json)
                           "https://localhost:7144", // Dein ChessServer Port (falls Client und Server auf derselben Maschine fuer lokale Tests laufen)
                           "http://localhost:5245")  // Dein ChessServer HTTP Port (falls Client und Server auf derselben Maschine fuer lokale Tests laufen)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Wichtig für SignalR mit Authentifizierung oder sitzungsbasierten Szenarien
    });
});
builder.Services.AddControllers();
builder.Services.AddSignalR();

// === HIER DIE FEHLENDE REGISTRIERUNG HINZUFUEGEN ===
// Registriere IChessLogger so, dass wenn InMemoryGameManager danach fragt,
// ein ChessLogger<InMemoryGameManager> bereitgestellt wird.
builder.Services.AddSingleton<IChessLogger>(sp =>
    new ChessLogger<InMemoryGameManager>(
        sp.GetRequiredService<ILogger<InMemoryGameManager>>()
    )
);
// Wenn andere Dienste IChessLogger mit ihrer eigenen Kategorie benoetigen,
// muessten sie aehnlich registriert werden oder eine generischere Factory verwendet werden.
// Fuer GameSession und CardEffects wird der IChessLogger manuell mit der korrekten Kategorie erstellt.
// Registriert den GameManager als Singleton (eine Instanz fuer die gesamte Anwendung).
builder.Services.AddSingleton<IGameManager, InMemoryGameManager>();

var app = builder.Build();
// Konfiguriert die HTTP-Request-Pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Chess API V1");
    });
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors(); // Stelle sicher, dass UseCors() hier aufgerufen wird, typischerweise nach UseRouting() und vor UseAuthorization()/UseEndpoints()
app.UseWebSockets();
app.MapControllers();
app.MapHub<ChessHub>(ServerConstants.ChessHubRoute);

app.Run();