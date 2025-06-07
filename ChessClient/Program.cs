// File: [SolutionDir]\ChessClient\Program.cs
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
using Chess.Logging;
using Microsoft.Extensions.Logging;

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
    }

    var httpClient = new HttpClient(loggingHandler)
    {
        BaseAddress = new Uri(serverBaseUrl.EndsWith('/') ? serverBaseUrl : serverBaseUrl + "/")
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
builder.Services.AddScoped<ICardState>(sp => new CardState(
    sp.GetRequiredService<IModalState>(),
    sp.GetRequiredService<IUiState>(),
    sp.GetRequiredService<IHighlightState>()
));

// Registrierung der neuen/erweiterten Dienste
builder.Services.AddScoped<GameOrchestrationService>();
builder.Services.AddScoped<HubSubscriptionService>();

builder.Services.AddScoped<IChessLogger>(sp =>
    new ChessLogger<ChessClient.Pages.Chess>(
        sp.GetRequiredService<ILogger<ChessClient.Pages.Chess>>()
    )
);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
await builder.Build().RunAsync();