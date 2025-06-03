// File: [SolutionDir]/ChessServer/Program.cs
using ChessServer.Hubs;
using ChessServer.Services;
using ChessServer.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Chess.Logging; // Hinzuf�gen f�r IChessLogger und ChessLogger
using Microsoft.Extensions.Logging; // Hinzuf�gen f�r ILogger<T>

var builder = WebApplication.CreateBuilder(args);

// Konfiguriert Dienste f�r die Anwendung.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
builder.Services.AddControllers();
builder.Services.AddSignalR();

// === HIER DIE FEHLENDE REGISTRIERUNG HINZUF�GEN ===
// Registriere IChessLogger so, dass wenn InMemoryGameManager danach fragt,
// ein ChessLogger<InMemoryGameManager> bereitgestellt wird.
builder.Services.AddSingleton<IChessLogger>(sp =>
    new ChessLogger<InMemoryGameManager>(
        sp.GetRequiredService<ILogger<InMemoryGameManager>>()
    )
);
// Wenn andere Dienste IChessLogger mit ihrer eigenen Kategorie ben�tigen,
// m�ssten sie �hnlich registriert werden oder eine generischere Factory verwendet werden.
// F�r GameSession und CardEffects wird der IChessLogger manuell mit der korrekten Kategorie erstellt.

// Registriert den GameManager als Singleton (eine Instanz f�r die gesamte Anwendung).
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
app.UseCors();
app.UseWebSockets();
app.MapControllers();
app.MapHub<ChessHub>(ServerConstants.ChessHubRoute);

app.Run();