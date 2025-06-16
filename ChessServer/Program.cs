using Chess.Logging;
using ChessServer.Configuration;
using ChessServer.Hubs;
using ChessServer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Dienste für die Anwendung konfigurieren.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient("ChessApi");

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "https://schacht.app",      // Azure-Domain
                "https://localhost:7224",   // ChessClient Kestrel HTTPS
                "http://localhost:5170",    // ChessClient Kestrel HTTP
                "https://localhost:7144",   // ChessServer HTTPS
                "http://localhost:5245",    // ChessServer HTTP
                "https://localhost:7276",   // Alternative Ports
                "http://localhost:7144"     // Alternative Ports
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();          // Wichtig für SignalR
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
     {
         // Diese Zeile sorgt dafür, dass Enums als Text statt als Zahlen behandelt werden.
         options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
     });
builder.Services.AddSignalR();

// Registriert den GameManager als Singleton (eine Instanz für die gesamte Anwendung).
builder.Services.AddSingleton<IGameManager, InMemoryGameManager>();

// Registriere IChessLogger so, dass wenn InMemoryGameManager danach fragt,
// ein ChessLogger<InMemoryGameManager> bereitgestellt wird.
builder.Services.AddSingleton<IChessLogger>(sp =>
    new ChessLogger<InMemoryGameManager>(
        sp.GetRequiredService<ILogger<InMemoryGameManager>>()
    )
);

var app = builder.Build();

// HTTP-Request-Pipeline konfigurieren.
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

// Macht die Program-Klasse für das Testprojekt sichtbar.
public partial class Program { }