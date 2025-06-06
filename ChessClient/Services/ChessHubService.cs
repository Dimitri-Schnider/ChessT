using ChessLogic;
using ChessNetwork.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChessClient.Services
{
    public class ChessHubService : IAsyncDisposable
    {
        private HubConnection? _hubConnection;
        public event Action<BoardDto, Player, GameStatusDto, string?, string?, List<AffectedSquareInfo>?>? OnTurnChanged;
        public event Action<TimeUpdateDto>? OnTimeUpdate;
        public event Action<string, int>? OnPlayerJoined;
        public event Action<string, int>? OnPlayerLeft;
        public event Action<Guid, CardDto>? OnCardPlayed;
        public event Action<Guid>? OnPlayerEarnedCardDraw;
        public event Action<InitialHandDto>? OnUpdateHandContents;
        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;
        public event Action<CardDto, Guid, Player>? OnPlayCardActivationAnimation;
        public event Action<InitialHandDto>? OnReceiveInitialHand;
        public event Action<CardDto, int>? OnCardAddedToHand;
        public event Action<CardSwapAnimationDetailsDto>? OnReceiveCardSwapAnimationDetails;
        public event Action? OnStartGameCountdown; // NEU

        public ChessHubService(NavigationManager navManager) { }

        public Task JoinGame(string gameId, string playerName)
        {
            if (_hubConnection == null || _hubConnection.State != HubConnectionState.Connected)
            { throw new InvalidOperationException("Hub connection is not established."); }
            return _hubConnection.InvokeAsync("JoinGame", gameId, playerName);
        }

        public Task RegisterPlayerWithHubAsync(Guid gameId, Guid playerId)
        {
            if (_hubConnection?.State == HubConnectionState.Connected)
            {
                return _hubConnection.InvokeAsync("RegisterConnection", gameId, playerId);
            }
            Console.WriteLine("WARNUNG: Versuch, Spieler zu registrieren, während Hub nicht verbunden ist.");
            return Task.CompletedTask;
        }

        public async Task StartAsync(string hubUrl)
        {
            if (_hubConnection == null || _hubConnection.State == HubConnectionState.Disconnected)
            {
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(hubUrl)
                    .Build();
                _hubConnection.On<BoardDto, Player, GameStatusDto, string?, string?, List<AffectedSquareInfo>?>("OnTurnChanged",
                    (boardDto, nextPlayer, statusDto, lastMoveFrom, lastMoveTo, cardEffectSquares) =>
                        OnTurnChanged?.Invoke(boardDto, nextPlayer, statusDto, lastMoveFrom, lastMoveTo, cardEffectSquares));
                _hubConnection.On<TimeUpdateDto>("OnTimeUpdate", dto => OnTimeUpdate?.Invoke(dto));
                _hubConnection.On<string, int>("PlayerJoined", (playerName, playerCount) => OnPlayerJoined?.Invoke(playerName, playerCount));
                _hubConnection.On<string, int>("PlayerLeft", (playerName, playerCount) => OnPlayerLeft?.Invoke(playerName, playerCount));
                _hubConnection.On<Guid, CardDto>("OnCardPlayed",
                    (playingPlayerId, playedCardDto) => OnCardPlayed?.Invoke(playingPlayerId, playedCardDto));
                _hubConnection.On<Guid>("OnPlayerEarnedCardDraw",
                    (playerId) => OnPlayerEarnedCardDraw?.Invoke(playerId));
                _hubConnection.On<CardDto, Guid, Player>("PlayCardActivationAnimation",
                    (cardDtoForAnimation, playerIdActivating, playerColorActivating) => OnPlayCardActivationAnimation?.Invoke(cardDtoForAnimation, playerIdActivating, playerColorActivating));
                _hubConnection.On<InitialHandDto>("ReceiveInitialHand",
                    (initialHandDto) => OnReceiveInitialHand?.Invoke(initialHandDto));
                _hubConnection.On<CardDto, int>("CardAddedToHand",
                    (drawnCard, newDrawPileCount) => OnCardAddedToHand?.Invoke(drawnCard, newDrawPileCount));
                _hubConnection.On<InitialHandDto>("UpdateHandContents",
                            (updatedHandInfo) => OnUpdateHandContents?.Invoke(updatedHandInfo));
                _hubConnection.On<CardSwapAnimationDetailsDto>("ReceiveCardSwapAnimationDetails",
                    (details) => OnReceiveCardSwapAnimationDetails?.Invoke(details));

                _hubConnection.On("StartGameCountdown", () => OnStartGameCountdown?.Invoke());

                _hubConnection.Closed += async (error) => {
                    await Task.Delay(new Random().Next(0, 5) * 1000);
                    if (_hubConnection != null && _hubConnection.State == HubConnectionState.Disconnected)
                    {
                        try { await StartAsync(hubUrl); } catch (Exception ex) { Console.WriteLine($"Reconnect failed: {ex.Message}"); }
                    }
                };
                await _hubConnection.StartAsync();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_hubConnection is not null)
            {
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

                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
            }
            GC.SuppressFinalize(this);
        }
    }
}