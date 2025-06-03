// File: [SolutionDir]/ChessServer/Program.cs
using ChessServer.Hubs;
using ChessServer.Services;
using ChessServer.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Chess.Logging; // Hinzufügen für IChessLogger und ChessLogger
using Microsoft.Extensions.Logging; // Hinzufügen für ILogger<T>

var builder = WebApplication.CreateBuilder(args);

// Konfiguriert Dienste für die Anwendung.
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

// === HIER DIE FEHLENDE REGISTRIERUNG HINZUFÜGEN ===
// Registriere IChessLogger so, dass wenn InMemoryGameManager danach fragt,
// ein ChessLogger<InMemoryGameManager> bereitgestellt wird.
builder.Services.AddSingleton<IChessLogger>(sp =>
    new ChessLogger<InMemoryGameManager>(
        sp.GetRequiredService<ILogger<InMemoryGameManager>>()
    )
);
// Wenn andere Dienste IChessLogger mit ihrer eigenen Kategorie benötigen,
// müssten sie ähnlich registriert werden oder eine generischere Factory verwendet werden.
// Für GameSession und CardEffects wird der IChessLogger manuell mit der korrekten Kategorie erstellt.

// Registriert den GameManager als Singleton (eine Instanz für die gesamte Anwendung).
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