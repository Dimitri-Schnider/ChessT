using ChessLogic;
using ChessNetwork.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChessClient.Services.Connectivity
{
    // Verwaltet die Echtzeit-Kommunikation mit dem Server über einen SignalR Hub.
    public class ChessHubService : IAsyncDisposable
    {
        // Die SignalR-Verbindung.
        private HubConnection? _hubConnection;

        // Definiert Events, die ausgelöst werden, wenn Nachrichten vom Hub empfangen werden.
        public event Action<BoardDto, Player, GameStatusDto, string?, string?, List<AffectedSquareInfo>?>? OnTurnChanged;
        public event Action<TimeUpdateDto>? OnTimeUpdate;
        public event Action<string, int>? OnPlayerJoined;
        public event Action<string, int>? OnPlayerLeft;
        public event Action<Guid, CardDto>? OnCardPlayed;
        public event Action<Guid>? OnPlayerEarnedCardDraw;
        public event Action<InitialHandDto>? OnUpdateHandContents;
        public event Action<CardDto, Guid, Player>? OnPlayCardActivationAnimation;
        public event Action<InitialHandDto>? OnReceiveInitialHand;
        public event Action<CardDto, int>? OnCardAddedToHand;
        public event Action<CardSwapAnimationDetailsDto>? OnReceiveCardSwapAnimationDetails;
        public event Action? OnStartGameCountdown;

        // Gibt an, ob eine aktive Verbindung zum Hub besteht.
        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

        // Leerer Konstruktor. 
        public ChessHubService() { }

        // Methode, um eine Spiel-Gruppe auf dem Hub zu verlassen.
        public Task LeaveGameGroupAsync(Guid gameId)
        {
            if (_hubConnection?.State == HubConnectionState.Connected)
            {
                // Ruft die "LeaveGame" Methode auf dem Server-Hub auf.
                return _hubConnection.InvokeAsync("LeaveGame", gameId.ToString());
            }
            Console.WriteLine("WARNUNG: Versuch, eine Gruppe zu verlassen, während Hub nicht verbunden ist.");
            return Task.CompletedTask;
        }

        // Registriert den Spieler bei einem spezifischen Spiel auf dem Hub.
        public Task RegisterPlayerWithHubAsync(Guid gameId, Guid playerId)
        {
            if (_hubConnection?.State == HubConnectionState.Connected)
            {
                return _hubConnection.InvokeAsync("RegisterConnection", gameId, playerId);
            }
            Console.WriteLine("WARNUNG: Versuch, Spieler zu registrieren, während Hub nicht verbunden ist.");
            return Task.CompletedTask;
        }

        // Baut die Hub-Verbindung auf, registriert alle Event-Handler und startet die Verbindung.
        public async Task StartAsync(string hubUrl)
        {
            if (_hubConnection == null || _hubConnection.State == HubConnectionState.Disconnected)
            {
                // Baut eine neue Hub-Verbindung auf.
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(hubUrl)
                    .Build();

                // Registriert die Handler für die verschiedenen Server-Nachrichten.
                _hubConnection.On<BoardDto, Player, GameStatusDto, string?, string?, List<AffectedSquareInfo>?>("OnTurnChanged",
                    (boardDto, nextPlayer, statusDto, lastMoveFrom, lastMoveTo, cardEffectSquares) =>
                        OnTurnChanged?.Invoke(boardDto, nextPlayer, statusDto, lastMoveFrom, lastMoveTo, cardEffectSquares));

                _hubConnection.On<TimeUpdateDto>("OnTimeUpdate", dto => OnTimeUpdate?.Invoke(dto));
                _hubConnection.On<string, int>("PlayerJoined", (playerName, playerCount) => OnPlayerJoined?.Invoke(playerName, playerCount));
                _hubConnection.On<Guid, CardDto>("OnCardPlayed", (playingPlayerId, playedCardDto) => OnCardPlayed?.Invoke(playingPlayerId, playedCardDto));
                _hubConnection.On<Guid>("OnPlayerEarnedCardDraw", (playerId) => OnPlayerEarnedCardDraw?.Invoke(playerId));
                _hubConnection.On<CardDto, Guid, Player>("PlayCardActivationAnimation", (cardDto, playerId, playerColor) => OnPlayCardActivationAnimation?.Invoke(cardDto, playerId, playerColor));
                _hubConnection.On<InitialHandDto>("ReceiveInitialHand", (initialHandDto) => OnReceiveInitialHand?.Invoke(initialHandDto));
                _hubConnection.On<CardDto, int>("CardAddedToHand", (drawnCard, count) => OnCardAddedToHand?.Invoke(drawnCard, count));
                _hubConnection.On<InitialHandDto>("UpdateHandContents", (updatedHandInfo) => OnUpdateHandContents?.Invoke(updatedHandInfo));
                _hubConnection.On<CardSwapAnimationDetailsDto>("ReceiveCardSwapAnimationDetails", (details) => OnReceiveCardSwapAnimationDetails?.Invoke(details));
                _hubConnection.On("StartGameCountdown", () => OnStartGameCountdown?.Invoke());

                // Definiert das Verhalten bei Verbindungsverlust (automatischer Wiederverbindungsversuch).
                _hubConnection.Closed += async (error) => {
                    await Task.Delay(new Random().Next(0, 5) * 1000);
                    if (_hubConnection != null && _hubConnection.State == HubConnectionState.Disconnected)
                    {
                        try { await StartAsync(hubUrl); } catch (Exception ex) { Console.WriteLine($"Reconnect failed: {ex.Message}"); }
                    }
                };

                // Startet die Verbindung zum Hub.
                await _hubConnection.StartAsync();
            }
        }

        // Gibt alle Ressourcen frei und beendet die Hub-Verbindung sauber.
        public async ValueTask DisposeAsync()
        {
            if (_hubConnection is not null)
            {
                // Deregistriert alle Handler.
                _hubConnection.Remove("OnTurnChanged");
                _hubConnection.Remove("OnTimeUpdate");
                _hubConnection.Remove("PlayerJoined");
                _hubConnection.Remove("PlayerLeft");
                _hubConnection.Remove("OnCardPlayed");
                _hubConnection.Remove("OnPlayerEarnedCardDraw");
                _hubConnection.Remove("PlayCardActivationAnimation");
                _hubConnection.Remove("ReceiveInitialHand");
                _hubConnection.Remove("CardAddedToHand");
                _hubConnection.Remove("UpdateHandContents");
                _hubConnection.Remove("ReceiveCardSwapAnimationDetails");
                _hubConnection.Remove("StartGameCountdown");

                // Stoppt und entsorgt die Verbindung.
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
            }
            GC.SuppressFinalize(this);
        }
    }
}