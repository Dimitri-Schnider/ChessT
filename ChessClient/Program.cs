// File: [SolutionDir]/ChessClient/Program.cs
using ChessClient;
using ChessClient.Services;
using ChessNetwork;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System;
using System.Net.Http;
using ChessClient.Configuration;
using ChessClient.State;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Chess.Logging; // Hinzufügen
using Microsoft.Extensions.Logging; // Hinzufügen

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddSingleton<LoggingService>();
builder.Services.AddTransient<LoggingHandler>();
builder.Services.AddScoped<IGameSession>(sp =>
{
    var loggingHandler = sp.GetRequiredService<LoggingHandler>();
    loggingHandler.InnerHandler = new HttpClientHandler();

    var configuration = sp.GetRequiredService<IConfiguration>();
    string? serverBaseUrlFromConfig = configuration.GetValue<string>("ServerBaseUrl");
    string serverBaseUrl = ClientConstants.DefaultServerBaseUrl;

    if (!string.IsNullOrEmpty(serverBaseUrlFromConfig))
    {
        serverBaseUrl = serverBaseUrlFromConfig;
        Console.WriteLine("INFO (HttpGameSession): ServerBaseUrl aus Konfiguration geladen: " + serverBaseUrl);
    }
    else
    {
        Console.WriteLine("WARNUNG (HttpGameSession): ServerBaseUrl nicht in Konfiguration gefunden. Fallback auf ClientConstants.DefaultServerBaseUrl: " + serverBaseUrl);
    }

    var httpClient = new HttpClient(loggingHandler)
    {
        BaseAddress = new Uri(serverBaseUrl.EndsWith('/') ?
                              serverBaseUrl : serverBaseUrl + "/")
    };
    return new HttpGameSession(httpClient);
});

builder.Services.AddSingleton<ChessHubService>();
builder.Services.AddScoped<ModalService>();

// State Container mit Interfaces registrieren
builder.Services.AddScoped<IUiState, UiState>();
builder.Services.AddScoped<IModalState, ModalState>();
builder.Services.AddScoped<IGameCoreState, GameCoreState>();
builder.Services.AddScoped<IHighlightState, HighlightState>();
builder.Services.AddScoped<IAnimationState, AnimationState>();
builder.Services.AddScoped<ICardState, CardState>();

builder.Services.AddScoped<GameOrchestrationService>();

// === HIER DIE FEHLENDE REGISTRIERUNG HINZUFÜGEN für ChessClient ===
builder.Services.AddScoped<IChessLogger>(sp =>
    new ChessLogger<ChessClient.Pages.Chess>( // Kategorie ist die Chess-Seite
        sp.GetRequiredService<ILogger<ChessClient.Pages.Chess>>()
    )
);


builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
await builder.Build().RunAsync();