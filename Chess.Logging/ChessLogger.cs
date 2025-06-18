using ChessLogic;
using ChessNetwork.DTOs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
namespace Chess.Logging
{
    public class ChessLogger<TCategoryName> : IChessLogger
    {
        private readonly ILogger<TCategoryName> _msLogger;
        // Event-ID Basiswerte für verschiedene Log-Kategorien
        // Diese helfen, die Event-IDs organisiert und eindeutig zu halten.
        private const int ClientLogBaseId = 20000;
        private const int ControllerLogBaseId = 21000;
        private const int HubLogBaseId = 22000;
        private const int SessionLogBaseId = 23000;
        private const int CardEffectsLogBaseId = 24000;
        private const int GameManagerLogBaseId = 25000;
        private const int PvcLogBaseId = 26000;
        private const int TimerLogBaseId = 27000;

        // --- ChessClient.Pages.Chess.razor.cs Logs ---
        private static readonly Action<ILogger, Guid, string, string, Exception?> _logStartingGenericCardSwapAnimationAction =
            LoggerMessage.Define<Guid, string, string>(LogLevel.Information, new EventId(ClientLogBaseId + 1, "ClientStartingGenericCardSwapAnimationLog"), "Starte CardSwapSpecificAnimation für Spieler {PlayerId}. Gegeben: {CardGivenName}, Erhalten: {CardReceivedName}");
        private static readonly Action<ILogger, Exception?> _logCardActivationAnimationFinishedClientAction =
            LoggerMessage.Define(LogLevel.Information, new EventId(ClientLogBaseId + 2, "ClientCardActivationAnimationFinishedLog"), "Generische CardActivationAnimation beendet (Client).");
        private static readonly Action<ILogger, Exception?> _logSpecificCardSwapAnimationFinishedClientAction =
            LoggerMessage.Define(LogLevel.Information, new EventId(ClientLogBaseId + 3, "ClientSpecificCardSwapAnimationFinishedLog"), "Spezifische CardSwapSpecificAnimation beendet (Client).");
        private static readonly Action<ILogger, Player, Player, Exception?> _logUpdatePlayerNamesMismatchAction =
            LoggerMessage.Define<Player, Player>(LogLevel.Warning, new EventId(ClientLogBaseId + 4, "ClientUpdatePlayerNamesMismatchLog"), "[Chess.razor.cs/UpdatePlayerNames] Abgerufene Gegnerfarbe {OpponentColorRetrieved} stimmt nicht mit erwarteter Farbe {OpponentColorExpected} überein.");
        private static readonly Action<ILogger, string, Exception?> _logUpdatePlayerNamesNotFoundAction =
              LoggerMessage.Define<string>(LogLevel.Information, new EventId(ClientLogBaseId + 5, "ClientUpdatePlayerNamesNotFoundLog"), "[Chess.razor.cs/UpdatePlayerNames] Kein Gegner vorhanden, um Namen abzurufen (erwartet bei neuem Spiel): {ErrorMessage}");
        private static readonly Action<ILogger, Exception?> _logUpdatePlayerNamesErrorAction =
            LoggerMessage.Define(LogLevel.Error, new EventId(ClientLogBaseId + 6, "ClientUpdatePlayerNamesErrorLog"), "[Chess.razor.cs/UpdatePlayerNames] Fehler beim Abrufen des Gegnernamens.");
        private static readonly Action<ILogger, string, Guid, Player, Exception?> _logHandlePlayCardActivationAnimationAction =
            LoggerMessage.Define<string, Guid, Player>(LogLevel.Debug, new EventId(ClientLogBaseId + 7, "ClientHandlePlayCardActivationAnimationLog"), "[ChessPage] HandlePlayCardActivationAnimation: CardTypeId: {CardTypeId}, PlayerId: {PlayerId}, Color: {PlayerColor}");
        private static readonly Action<ILogger, string?, bool, Exception?> _logHandleClientAnimationFinishedTriggeredAction =
            LoggerMessage.Define<string?, bool>(LogLevel.Debug, new EventId(ClientLogBaseId + 8, "ClientAnimationFinishedTriggeredLog"), "[ChessPage] HandleClientAnimationFinished (Callback der generischen Animation) ausgelöst. Letzte animierte Karte war '{LastCardId}', PendingSwapDetails ist null: {IsPendingSwapNull}");
        private static readonly Action<ILogger, string, string, bool, Exception?> _logHandleReceiveCardSwapDetailsAction =
            LoggerMessage.Define<string, string, bool>(LogLevel.Debug, new EventId(ClientLogBaseId + 9, "ClientReceiveCardSwapDetailsLog"), "[ChessPage] HandleReceiveCardSwapAnimationDetails: Gegeben: {GivenCardName}, Erhalten: {ReceivedCardName}, Ist generische Anim. aktiv: {IsGenericAnimating}");
        private static readonly Action<ILogger, string, string, Exception?> _logActuallyStartingSpecificSwapAnimAction =
            LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(ClientLogBaseId + 10, "ClientActuallyStartingSpecificSwapAnimLog"), "[ChessPage] Starte CardSwapSpecificAnimation jetzt. Gegeben: {GivenCardName}, Erhalten: {ReceivedCardName}");
        private static readonly Action<ILogger, string, Exception?> _logGenericAnimationStartedForCardAction =
            LoggerMessage.Define<string>(LogLevel.Information, new EventId(ClientLogBaseId + 11, "ClientGenericAnimationStartedForCardLog"), "[ChessPage] Generische CardActivationAnimation gestartet für Karte: {CardName}");
        private static readonly Action<ILogger, string, Exception?> _logClientCriticalServicesNullOnInitAction =
            LoggerMessage.Define<string>(LogLevel.Warning, new EventId(ClientLogBaseId + 12, "ClientCriticalServicesNullLog"), "[ChessPage] Kritische Dienste sind null bei Initialisierung oder Aufruf: {ServiceName}");
        private static readonly Action<ILogger, string, Exception?> _logClientSignalRConnectionWarningAction =
            LoggerMessage.Define<string>(LogLevel.Warning, new EventId(ClientLogBaseId + 13, "ClientSignalRConnectionWarningLog"), "[ChessPage] SignalR Verbindungswarnung: {ErrorMessage}");
        private static readonly Action<ILogger, ChessboardEnabledStatusLogArgs, Exception?> _logIsChessboardEnabledStatusAction =
            LoggerMessage.Define<ChessboardEnabledStatusLogArgs>(LogLevel.Debug, new EventId(ClientLogBaseId + 14, "ClientIsChessboardEnabledStatusLog"), "[ChessPage] IsChessboardEnabled Raw Status: {StatusArgs}");
        private static readonly Action<ILogger, Player, GameStatusDto, string?, string?, int, Exception?> _logHandleHubTurnChangedClientInfoAction =
            LoggerMessage.Define<Player, GameStatusDto, string?, string?, int>(LogLevel.Information, new EventId(ClientLogBaseId + 15, "ClientHandleHubTurnChangedInfoLog"), "[ChessPage] HandleHubTurnChangedAsync received. NextPlayer: {NextPlayer}, StatusForNext: {StatusForNextPlayer}, LastMoveFrom: {LastMoveFrom}, LastMoveTo: {LastMoveTo}, CardEffectsCount: {CardEffectsCount}");
        private static readonly Action<ILogger, bool, string, Exception?> _logAwaitingTurnConfirmationStatusAction =
            LoggerMessage.Define<bool, string>(LogLevel.Debug, new EventId(ClientLogBaseId + 16, "ClientAwaitingTurnConfirmationStatusLog"), "[ChessPage] _isAwaitingTurnConfirmationAfterCard status: {FlagStatus}. Reason/Context: {Context}");
        private static readonly Action<ILogger, Guid, string, Exception?> _logClientAttemptedToAddDuplicateCardInstanceAction =
            LoggerMessage.Define<Guid, string>(LogLevel.Warning, new EventId(ClientLogBaseId + 17, "ClientAttemptedDuplicateCardInstanceLog"), "[CardState] Client hat versucht, Karte mit bereits vorhandener InstanceId {InstanceId} ('{CardName}') hinzuzufügen. Hinzufügen übersprungen.");
        private static readonly Action<ILogger, Guid?, Exception?> _logCardsRevealedAction =
            LoggerMessage.Define<Guid?>(LogLevel.Information, new EventId(ClientLogBaseId + 18, "ClientCardsRevealedLog"), "[ChessPage] Countdown beendet, Karten für Spiel {GameId} werden aufgedeckt."); // NEU

        // --- ChessServer.Controllers.GamesController.cs Logs ---
        private static readonly Action<ILogger, Guid, string, string, Exception?> _logMoveErrorAction =
            LoggerMessage.Define<Guid, string, string>(LogLevel.Error, new EventId(ControllerLogBaseId + 1, "CtrlMoveProcessingErrorLog"), "Fehler beim Verarbeiten des Zugs in Spiel {GameId}: {FromSquare}->{ToSquare}");
        private static readonly Action<ILogger, Guid, string, int, Exception?> _logGameCreatedAction =
            LoggerMessage.Define<Guid, string, int>(LogLevel.Information, new EventId(ControllerLogBaseId + 2, "CtrlGameCreatedLog"), "Spiel {GameId} erstellt für Spieler {PlayerName} mit Zeit {InitialMinutes} Minuten");
        private static readonly Action<ILogger, string, Guid, Exception?> _logPlayerJoinedGameAction =
            LoggerMessage.Define<string, Guid>(LogLevel.Information, new EventId(ControllerLogBaseId + 3, "CtrlPlayerJoinedGameLog"), "Spieler {PlayerName} ist Spiel {GameId} beigetreten");
        private static readonly Action<ILogger, Guid, Exception?> _logGameNotFoundOnJoinAttemptAction =
            LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(ControllerLogBaseId + 4, "CtrlGameNotFoundOnJoinAttemptLog"), "Versuch, nicht existierendem Spiel {GameId} beizutreten");
        private static readonly Action<ILogger, Guid, string, Exception?> _logInvalidOperationOnJoinGameAction =
            LoggerMessage.Define<Guid, string>(LogLevel.Warning, new EventId(ControllerLogBaseId + 5, "CtrlInvalidOperationOnJoinGameLog"), "Ungültige Operation beim Beitritt zu Spiel {GameId}: {ErrorMessage}");
        private static readonly Action<ILogger, Guid, Guid, bool, Exception?> _logApplyMoveInfoAction =
            LoggerMessage.Define<Guid, Guid, bool>(LogLevel.Information, new EventId(ControllerLogBaseId + 6, "CtrlApplyMoveInfoLog"), "[GamesController] ApplyMove für Spiel {GameId} von Spieler {PlayerId} war IsValid={IsValid}");
        private static readonly Action<ILogger, Guid, Player, GameStatusDto, string?, string?, string?, Exception?> _logSignalRUpdateInfoAction =
            LoggerMessage.Define<Guid, Player, GameStatusDto, string?, string?, string?>(LogLevel.Information, new EventId(ControllerLogBaseId + 7, "CtrlSignalRUpdateInfoLog"), "[GamesController] Sende SignalR Update für Spiel {GameId} via Hub. Nächster Spieler: {NextPlayerTurn}, Status für ihn: {StatusForNextPlayer}, LastMoveFrom: {LastMoveFrom}, LastMoveTo: {LastMoveTo}, CardEffectSquareCount: {CardEffectSquareCount}");
        private static readonly Action<ILogger, Guid, Exception?> _logOnTurnChangedSentToHubAction =
            LoggerMessage.Define<Guid>(LogLevel.Information, new EventId(ControllerLogBaseId + 8, "CtrlOnTurnChangedSentToHubLog"), "[GamesController] OnTurnChanged für Spiel {GameId} gesendet.");
        private static readonly Action<ILogger, Guid, Exception?> _logOnTimeUpdateSentAfterMoveAction =
            LoggerMessage.Define<Guid>(LogLevel.Information, new EventId(ControllerLogBaseId + 9, "CtrlOnTimeUpdateSentAfterMoveLog"), "[GamesController] OnTimeUpdate für Spiel {GameId} nach Zug gesendet.");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logGameNotFoundOnMoveAction =
            LoggerMessage.Define<Guid, Guid>(LogLevel.Warning, new EventId(ControllerLogBaseId + 10, "CtrlGameNotFoundOnMoveLog"), "[GamesController] Spiel {GameId} nicht gefunden bei Zug von Spieler {PlayerId}");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logInvalidOperationOnMoveAction =
            LoggerMessage.Define<Guid, Guid>(LogLevel.Warning, new EventId(ControllerLogBaseId + 11, "CtrlInvalidOperationOnMoveLog"), "[GamesController] Ungültige Operation bei Zug in Spiel {GameId} von Spieler {PlayerId}");
        private static readonly Action<ILogger, Guid, Exception?> _logGameNotFoundOnTimeRequestAction =
            LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(ControllerLogBaseId + 12, "CtrlGameNotFoundOnTimeRequestLog"), "Spiel {GameId} nicht gefunden beim Abrufen der Zeit.");
        private static readonly Action<ILogger, Guid, Exception?> _logErrorGettingTimeAction =
            LoggerMessage.Define<Guid>(LogLevel.Error, new EventId(ControllerLogBaseId + 13, "CtrlErrorGettingTimeLog"), "Fehler beim Abrufen der Zeit für Spiel {GameId}");
        private static readonly Action<ILogger, Guid, Exception?> _logGameHistoryNullFromManagerAction =
            LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(ControllerLogBaseId + 14, "CtrlGameHistoryNullFromManagerLog"), "Spielverlauf für Spiel {GameId} nicht gefunden (Manager gab null zurück).");
        private static readonly Action<ILogger, Guid, Exception?> _logGameHistoryKeyNotFoundAction =
            LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(ControllerLogBaseId + 15, "CtrlGameHistoryKeyNotFoundLog"), "Spiel {GameId} nicht gefunden beim Anfordern des Spielverlaufs.");
        private static readonly Action<ILogger, Guid, Exception?> _logGameHistoryGenericErrorAction =
            LoggerMessage.Define<Guid>(LogLevel.Error, new EventId(ControllerLogBaseId + 16, "CtrlGameHistoryGenericErrorLog"), "Fehler beim Abrufen des Spielverlaufs für Spiel {GameId}");
        private static readonly Action<ILogger, Guid, Guid, string, Exception?> _logCardActivationAttemptAction =
            LoggerMessage.Define<Guid, Guid, string>(LogLevel.Information, new EventId(ControllerLogBaseId + 17, "CtrlCardActivationAttemptLog"), "[GamesController] Kartenaktivierungsversuch für Spiel {GameId}, Spieler {PlayerId}, Karte {CardTypeId}");
        private static readonly Action<ILogger, Guid, Guid, string, string, Exception?> _logCardActivationFailedControllerAction =
            LoggerMessage.Define<Guid, Guid, string, string>(LogLevel.Warning, new EventId(ControllerLogBaseId + 18, "CtrlCardActivationFailedLog"), "[GamesController] Kartenaktivierung FEHLGESCHLAGEN für Spiel {GameId}, Spieler {PlayerId}, Karte {CardTypeId}. Grund: {Reason}");
        private static readonly Action<ILogger, Guid, Guid, string, Exception?> _logCardActivationSuccessControllerAction =
            LoggerMessage.Define<Guid, Guid, string>(LogLevel.Information, new EventId(ControllerLogBaseId + 19, "CtrlCardActivationSuccessLog"), "[GamesController] Kartenaktivierung ERFOLGREICH für Spiel {GameId}, Spieler {PlayerId}, Karte {CardTypeId}");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logSignalingPlayerToDrawCardAction =
            LoggerMessage.Define<Guid, Guid>(LogLevel.Information, new EventId(ControllerLogBaseId + 20, "CtrlSignalingPlayerToDrawCardLog"), "[GamesController] Signalisiere Spieler {PlayerIdToDraw} in Spiel {GameId}, eine Karte zu ziehen.");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logGettingCapturedPiecesAction =
            LoggerMessage.Define<Guid, Guid>(LogLevel.Debug, new EventId(ControllerLogBaseId + 21, "CtrlGettingCapturedPiecesLog"), "[GamesController] Rufe geschlagene Figuren für Spieler {PlayerId} in Spiel {GameId} ab.");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logGameNotFoundCapturedPiecesAction =
            LoggerMessage.Define<Guid, Guid>(LogLevel.Warning, new EventId(ControllerLogBaseId + 22, "CtrlGameNotFoundCapturedPiecesLog"), "[GamesController] Spiel {GameId} nicht gefunden beim Abrufen der geschlagenen Figuren für Spieler {PlayerId}.");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logErrorGettingCapturedPiecesAction =
            LoggerMessage.Define<Guid, Guid>(LogLevel.Error, new EventId(ControllerLogBaseId + 23, "CtrlErrorGettingCapturedPiecesLog"), "[GamesController] Allgemeiner Fehler beim Abrufen der geschlagenen Figuren für Spiel {GameId}, Spieler {PlayerId}.");
        private static readonly Action<ILogger, Guid, string, Exception?> _logControllerGameNotFoundAction =
            LoggerMessage.Define<Guid, string>(LogLevel.Warning, new EventId(ControllerLogBaseId + 24, "CtrlGameNotFoundForActionLog"), "[GamesController] Spiel {GameId} für Aktion {ActionName} nicht gefunden.");
        private static readonly Action<ILogger, string, string, Guid, Exception?> _logCtrlMoveSentCardToHandAction =
            LoggerMessage.Define<string, string, Guid>(LogLevel.Information, new EventId(ControllerLogBaseId + 25, "CtrlMoveSentCardToHandLog"), "[GamesController] Move: Sent CardAddedToHand (Card: {CardName}) to Connection {ConnectionId} for player {PlayerId}");
        private static readonly Action<ILogger, string, Guid, Exception?> _logCtrlConnIdNotFoundNoMoreCardsAction =
            LoggerMessage.Define<string, Guid>(LogLevel.Warning, new EventId(ControllerLogBaseId + 26, "CtrlConnIdNotFoundNoMoreCardsLog"), "[GamesController] {ActionSource}: ConnectionId for player {PlayerId} not found to send NoMoreCards info.");
        private static readonly Action<ILogger, string, Guid, Exception?> _logCtrlConnIdNotFoundGenericAction =
            LoggerMessage.Define<string, Guid>(LogLevel.Warning, new EventId(ControllerLogBaseId + 27, "CtrlConnIdNotFoundGenericLog"), "[GamesController] {ActionSource}: ConnectionId for player {PlayerId} not found. Sent generic OnPlayerEarnedCardDraw.");
        private static readonly Action<ILogger, string, string, Guid, Exception?> _logCtrlActivateCardSentCardToHandAction =
            LoggerMessage.Define<string, string, Guid>(LogLevel.Information, new EventId(ControllerLogBaseId + 28, "CtrlActivateCardSentCardToHandLog"), "[GamesController] ActivateCard: Sent CardAddedToHand (Card: {CardName}) to Connection {ConnectionId} for player {PlayerId}");
        private static readonly Action<ILogger, Guid, Player, Exception?> _logControllerCouldNotDeterminePlayerIdForStatusAction =
            LoggerMessage.Define<Guid, Player>(LogLevel.Warning, new EventId(ControllerLogBaseId + 29, "CtrlCouldNotDeterminePlayerIdForStatusLog"), "[GamesController] Konnte Spieler-ID für {PlayerColor} in Spiel {GameId} nicht ermitteln, um Status zu senden.");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logControllerErrorGettingOpponentInfoAction =
            LoggerMessage.Define<Guid, Guid>(LogLevel.Error, new EventId(ControllerLogBaseId + 30, "CtrlErrorGettingOpponentInfoLog"), "Fehler beim Abrufen der Gegnerinformationen für Spiel {GameId}, Spieler {PlayerId}");
        private static readonly Action<ILogger, Guid, Guid, string, Exception?> _logControllerErrorGettingLegalMovesAction =
            LoggerMessage.Define<Guid, Guid, string>(LogLevel.Warning, new EventId(ControllerLogBaseId + 31, "CtrlErrorGettingLegalMovesLog"), "Fehler bei LegalMoves für Spiel {GameId}, Spieler {PlayerId}, Von {FromSquare}");
        private static readonly Action<ILogger, Guid, Exception?> _logCtrlConnIdForPlayerNotFoundAction =
            LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(ControllerLogBaseId + 32, "CtrlConnIdForPlayerNotFoundLog"), "[GamesController] GetConnectionIdForPlayerViaHubMap: Konnte ConnectionId für PlayerId {PlayerId} nicht finden.");
        // --- ChessServer.Hubs.ChessHub.cs Logs ---
        private static readonly Action<ILogger, string, Exception?> _logHubClientConnectedAction =
            LoggerMessage.Define<string>(LogLevel.Information, new EventId(HubLogBaseId + 1, "HubClientConnectedLog"), "SignalR Hub: Client verbunden: {ConnectionId}");
        private static readonly Action<ILogger, string, string?, Exception?> _logHubClientDisconnectedAction =
            LoggerMessage.Define<string, string?>(LogLevel.Information, new EventId(HubLogBaseId + 2, "HubClientDisconnectedLog"), "SignalR Hub: Client getrennt: {ConnectionId}, Fehler: {ErrorMessage}");
        private static readonly Action<ILogger, string, string, Exception?> _logHubClientJoiningGameGroupAction =
            LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(HubLogBaseId + 3, "HubClientJoiningGameGroupLog"), "SignalR Hub: Client {ConnectionId} versucht Spielgruppe {GameIdString} beizutreten.");
        private static readonly Action<ILogger, string, string, Exception?> _logHubClientAddedToGameGroupAction =
            LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(HubLogBaseId + 4, "HubClientAddedToGameGroupLog"), "SignalR Hub: Client {ConnectionId} zu Spielgruppe {GameIdString} hinzugefügt.");
        private static readonly Action<ILogger, string, string, int, Exception?> _logHubPlayerJoinedNotificationSentAction =
            LoggerMessage.Define<string, string, int>(LogLevel.Information, new EventId(HubLogBaseId + 5, "HubPlayerJoinedNotificationSentLog"), "SignalR Hub: PlayerJoined-Nachricht für {JoiningPlayerName} in Spiel {GameIdString} gesendet. Spieleranzahl: {PlayerCount}");
        private static readonly Action<ILogger, string, string, Exception?> _logHubClientLeavingGameGroupAction =
            LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(HubLogBaseId + 6, "HubClientLeavingGameGroupLog"), "SignalR Hub: Client {ConnectionId} versucht Spielgruppe {GameIdString} zu verlassen.");
        private static readonly Action<ILogger, string, string, Exception?> _logHubClientRemovedFromGameGroupAction =
            LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(HubLogBaseId + 7, "HubClientRemovedFromGameGroupLog"), "SignalR Hub: Client {ConnectionId} aus Spielgruppe {GameIdString} entfernt.");
        private static readonly Action<ILogger, Guid, string, Exception?> _logHubGameNotFoundForPlayerCountAction =
            LoggerMessage.Define<Guid, string>(LogLevel.Warning, new EventId(HubLogBaseId + 8, "HubGameNotFoundForPlayerCountLog"), "Spiel {GameIdGuid} nicht im GameManager gefunden, um Spielerzahl zu bestimmen, während Client {ConnectionId} beitritt.");
        private static readonly Action<ILogger, string, string, Exception?> _logHubJoinGameInvalidGameIdFormatAction =
            LoggerMessage.Define<string, string>(LogLevel.Warning, new EventId(HubLogBaseId + 9, "HubJoinGameInvalidGameIdFormatLog"), "SignalR Hub: JoinGame mit ungültigem Game-ID Format: {GameIdString} von Client {ConnectionId}");
        private static readonly Action<ILogger, Guid, string, Guid, Exception?> _logHubPlayerRegisteredToHubAction =
            LoggerMessage.Define<Guid, string, Guid>(LogLevel.Information, new EventId(HubLogBaseId + 10, "HubPlayerRegisteredToHubLog"), "SignalR Hub: Spieler {PlayerId} (Connection: {ConnectionId}) für Spiel {GameId} registriert.");
        private static readonly Action<ILogger, string, Exception?> _logHubPlayerDeregisteredFromHubAction =
            LoggerMessage.Define<string>(LogLevel.Information, new EventId(HubLogBaseId + 11, "HubPlayerDeregisteredFromHubLog"), "SignalR Hub: Verbindung {ConnectionId} und zugehöriger Spieler deregistriert.");
        private static readonly Action<ILogger, Guid, string, Guid, int, int, Exception?> _logHubSendingInitialHandAction =
            LoggerMessage.Define<Guid, string, Guid, int, int>(LogLevel.Information, new EventId(HubLogBaseId + 12, "HubSendingInitialHandLog"), "[ChessHub] Sende Starthand an Spieler {PlayerId} (Connection: {ConnectionId}) für Spiel {GameId}. Handgrösse: {HandSize}, Nachziehstapel: {DrawPileCount}");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logHubFailedToSendInitialHandSessionNotFoundAction =
            LoggerMessage.Define<Guid, Guid>(LogLevel.Warning, new EventId(HubLogBaseId + 13, "HubFailedToSendInitialHandSessionNotFoundLog"), "[ChessHub] Konnte GameSession für Spiel {GameId} nicht abrufen, um Starthand an Spieler {PlayerId} zu senden.");
        private static readonly Action<ILogger, string, string, Exception?> _logHubPlayerActuallyJoinedGameAction =
            LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(HubLogBaseId + 14, "HubPlayerActuallyJoinedGameLog"), "SignalR Hub: Spieler {PlayerName} ist Spiel {GameIdString} tatsächlich beigetreten und wird nun der Gruppe hinzugefügt.");
        private static readonly Action<ILogger, Guid, Exception?> _logHubPlayerMappingRemovedOnDisconnectAction =
            LoggerMessage.Define<Guid>(LogLevel.Information, new EventId(HubLogBaseId + 15, "HubPlayerMappingRemovedOnDisconnectLog"), "SignalR Hub: Mapping für PlayerId {PlayerId} bei Disconnect entfernt.");
        private static readonly Action<ILogger, string, Guid, Exception?> _logHubConnectionRemovedFromGameOnDisconnectAction =
              LoggerMessage.Define<string, Guid>(LogLevel.Information, new EventId(HubLogBaseId + 16, "HubConnectionRemovedFromGameOnDisconnectLog"), "SignalR Hub: Connection {ConnectionId} aus Spiel {GameId} bei Disconnect entfernt.");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logHubErrorSendingInitialHandAction =
            LoggerMessage.Define<Guid, Guid>(LogLevel.Error, new EventId(HubLogBaseId + 17, "HubErrorSendingInitialHandLog"), "[ChessHub] Fehler beim Senden der Starthand an Spieler {PlayerId} in Spiel {GameId}.");
        private static readonly Action<ILogger, Guid, Exception?> _logStartGameCountdownAction =
            LoggerMessage.Define<Guid>(LogLevel.Information, new EventId(HubLogBaseId + 18, "HubStartGameCountdownLog"), "[ChessHub] Beide Spieler sind beigetreten. Starte Countdown für Spiel {GameId}."); // NEU
                                                                                                                                                                                                             // --- ChessServer.Services.GameSession.cs Logs ---
        private static readonly Action<ILogger, Guid, Exception?> _logSessionErrorGetNameByColorAction =
            LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(SessionLogBaseId + 1, "SessionErrorGetNameByColorLog"), "[GameSession] Fehler in MakeMove (GetPlayerColor) für Spiel {GameId}");
        private static readonly Action<ILogger, Guid, TimeSpan, TimeSpan, Player?, Exception?> _logSessionSendTimeUpdateAction =
            LoggerMessage.Define<Guid, TimeSpan, TimeSpan, Player?>(LogLevel.Debug, new EventId(SessionLogBaseId + 2, "SessionSendTimeUpdateLog"), "[GameSession] SendTimeUpdate für Spiel {GameId}: W:{WhiteTime} B:{BlackTime} Active:{ActivePlayer}");
        private static readonly Action<ILogger, Guid, Exception?> _logSessionErrorIsPlayerTurnAction =
            LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(SessionLogBaseId + 3, "SessionErrorIsPlayerTurnLog"), "[GameSession] Fehler in IsPlayerTurn bei GetPlayerColor für Spiel {GameId}");
        private static readonly Action<ILogger, Guid, Guid, int, Exception?> _logSessionColorNotDeterminedAction =
            LoggerMessage.Define<Guid, Guid, int>(LogLevel.Warning, new EventId(SessionLogBaseId + 4, "SessionColorNotDeterminedLog"), "[GameSession] Farbe für Spieler {PlayerId} in Spiel {GameId} konnte nicht eindeutig bestimmt werden. Spieleranzahl: {PlayerCount}");
        private static readonly Action<ILogger, Guid, Player, Exception?> _logGameEndedByTimeoutInSessionAction =
            LoggerMessage.Define<Guid, Player>(LogLevel.Information, new EventId(SessionLogBaseId + 5, "GameEndedByTimeoutInSessionLog"), "[GameSession] Spiel {GameId} durch Timeout von Spieler {ExpiredPlayer} beendet.");
        private static readonly Action<ILogger, Guid, Guid, string, Exception?> _logSessionCardActivationAttemptAction =
            LoggerMessage.Define<Guid, Guid, string>(LogLevel.Information, new EventId(SessionLogBaseId + 6, "SessionCardActivationAttemptLog"), "[GameSession] Kartenaktivierungsversuch: Spiel {GameId}, Spieler {PlayerId}, Karte {CardId}");
        private static readonly Action<ILogger, Guid, Guid, string, string, Exception?> _logSessionCardActivationFailedAction =
            LoggerMessage.Define<Guid, Guid, string, string>(LogLevel.Warning, new EventId(SessionLogBaseId + 7, "SessionCardActivationFailedLog"), "[GameSession] Kartenaktivierung FEHLGESCHLAGEN: Spiel {GameId}, Spieler {PlayerId}, Karte {CardId}. Grund: {Reason}");
        private static readonly Action<ILogger, Guid, Guid, string, Exception?> _logSessionCardActivationSuccessAction =
            LoggerMessage.Define<Guid, Guid, string>(LogLevel.Information, new EventId(SessionLogBaseId + 8, "SessionCardActivationSuccessLog"), "[GameSession] Kartenaktivierung ERFOLGREICH: Spiel {GameId}, Spieler {PlayerId}, Karte {CardId}");
        private static readonly Action<ILogger, Guid, Guid, string, Exception?> _logExtraTurnEffectAppliedAction =
            LoggerMessage.Define<Guid, Guid, string>(LogLevel.Information, new EventId(SessionLogBaseId + 9, "LogExtraTurnEffectAppliedLog"), "[GameSession] Extrazug-Effekt angewendet für Spieler {PlayerId} in Spiel {GameId} nach Karte {CardId}.");
        private static readonly Action<ILogger, Guid, Guid, int, Exception?> _logPlayerMoveCountIncreasedAction =
              LoggerMessage.Define<Guid, Guid, int>(LogLevel.Debug, new EventId(SessionLogBaseId + 10, "LogPlayerMoveCountIncreasedLog"), "[GameSession] Zugzähler für Spieler {PlayerId} in Spiel {GameId} erhöht auf {MoveCount}.");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logPlayerCardDrawIndicatedAction =
            LoggerMessage.Define<Guid, Guid>(LogLevel.Information, new EventId(SessionLogBaseId + 11, "LogPlayerCardDrawIndicatedLog"), "[GameSession] Spieler {PlayerId} in Spiel {GameId} soll eine Karte ziehen.");
        private static readonly Action<ILogger, Guid, Guid, string, Exception?> _logNotifyingOpponentOfCardPlayAction =
            LoggerMessage.Define<Guid, Guid, string>(LogLevel.Information, new EventId(SessionLogBaseId + 12, "NotifyingOpponentOfCardPlayLog"), "[GameSession] Benachrichtige alle in Spiel {GameId} über Kartenspiel von Spieler {PlayerId}, Karte {CardId}.");
        private static readonly Action<ILogger, Guid, PieceType, Player, Exception?> _logCapturedPieceAddedAction =
            LoggerMessage.Define<Guid, PieceType, Player>(LogLevel.Debug, new EventId(SessionLogBaseId + 13, "CapturedPieceAddedLog"), "[GameSession] {GameId}: Figur {PieceType} von Spieler {PlayerColor} zur Liste der geschlagenen Figuren hinzugefügt.");
        private static readonly Action<ILogger, Guid, string, string, PieceType?, Exception?> _logPawnPromotionMoveSelectionAction =
            LoggerMessage.Define<Guid, string, string, PieceType?>(LogLevel.Debug, new EventId(SessionLogBaseId + 14, "PawnPromotionMoveSelectionLog"), "[GameSession] MakeMove: Suche nach Bauernumwandlung für Spiel {GameId} von {From} nach {To} zu {PromotionType}.");
        private static readonly Action<ILogger, Guid, string, string, PieceType, Exception?> _logPawnPromotionMoveFoundAction =
              LoggerMessage.Define<Guid, string, string, PieceType>(LogLevel.Information, new EventId(SessionLogBaseId + 15, "PawnPromotionMoveFoundLog"), "[GameSession] MakeMove: Bauernumwandlung für Spiel {GameId} von {From} nach {To} zu {PromotionType} GEFUNDEN und ausgewählt.");
        private static readonly Action<ILogger, Guid, string, string, PieceType?, Exception?> _logPawnPromotionMoveNotFoundAction =
            LoggerMessage.Define<Guid, string, string, PieceType?>(LogLevel.Warning, new EventId(SessionLogBaseId + 16, "PawnPromotionMoveNotFoundLog"), "[GameSession] MakeMove: Bauernumwandlung für Spiel {GameId} von {From} nach {To} zu {PromotionType} NICHT gefunden unter legalen Zügen.");
        private static readonly Action<ILogger, Guid, string?, string?, string, Exception?> _logOnTurnChangedFromSessionAction =
           LoggerMessage.Define<Guid, string?, string?, string>(LogLevel.Debug, new EventId(SessionLogBaseId + 17, "OnTurnChangedFromSessionLog"), "[GameSession] Sende OnTurnChanged für Spiel {GameId}. LastMoveFrom: {LastMoveFrom}, LastMoveTo: {LastMoveTo}, CardEffectType: {CardEffectType}");
        private static readonly Action<ILogger, Guid, Guid, int, Exception?> _logPlayerDeckInitializedAction =
            LoggerMessage.Define<Guid, Guid, int>(LogLevel.Information, new EventId(SessionLogBaseId + 18, "PlayerDeckInitializedLog"), "[GameSession] Deck für Spieler {PlayerId} in Spiel {GameId} initialisiert und gemischt. {DrawPileCount} Karten im Stapel.");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logDrawAttemptUnknownPlayerAction =
            LoggerMessage.Define<Guid, Guid>(LogLevel.Warning, new EventId(SessionLogBaseId + 19, "DrawAttemptUnknownPlayerLog"), "[GameSession] Versuch, Karte für unbekannten Spieler {PlayerId} in Spiel {GameId} zu ziehen.");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logNoDrawPileForPlayerAction =
            LoggerMessage.Define<Guid, Guid>(LogLevel.Error, new EventId(SessionLogBaseId + 20, "NoDrawPileForPlayerLog"), "[GameSession] Kein Nachziehstapel für Spieler {PlayerId} in Spiel {GameId} gefunden!");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logPlayerDrawPileEmptyAction =
            LoggerMessage.Define<Guid, Guid>(LogLevel.Information, new EventId(SessionLogBaseId + 21, "PlayerDrawPileEmptyLog"), "[GameSession] Nachziehstapel für Spieler {PlayerId} in Spiel {GameId} ist leer.");
        private static readonly Action<ILogger, Guid, string, string, Guid, int, Exception?> _logPlayerDrewCardFromOwnDeckAction =
            LoggerMessage.Define<Guid, string, string, Guid, int>(LogLevel.Information, new EventId(SessionLogBaseId + 22, "PlayerDrewCardFromOwnDeckLog"), "[GameSession] Spieler {PlayerId} hat Karte '{CardName}' ({CardId}) aus seinem Deck in Spiel {GameId} gezogen. {RemainingInPlayerDrawPile} Karten in seinem Stapel verbleibend.");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logCannotFindPlayerDrawPileForCountAction =
            LoggerMessage.Define<Guid, Guid>(LogLevel.Warning, new EventId(SessionLogBaseId + 23, "CannotFindPlayerDrawPileForCountLog"), "[GameSession] Konnte Nachziehstapel für Spieler {PlayerId} in Spiel {GameId} nicht finden, um Anzahl zu ermitteln.");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logCurrentPlayerNotFoundForOpponentDetailsAction =
            LoggerMessage.Define<Guid, Guid>(LogLevel.Warning, new EventId(SessionLogBaseId + 24, "CurrentPlayerNotFoundForOpponentDetailsLog"), "[GameSession] GetOpponentDetails: Aktueller Spieler {CurrentPlayerId} nicht in Spiel {GameId} gefunden.");
        private static readonly Action<ILogger, Guid, Player, Guid, Exception?> _logNoOpponentFoundForPlayerAction =
            LoggerMessage.Define<Guid, Player, Guid>(LogLevel.Information, new EventId(SessionLogBaseId + 25, "NoOpponentFoundForPlayerLog"), "[GameSession] GetOpponentDetails: Kein Gegner für Spieler {CurrentPlayerId} (Farbe: {CurrentPlayerColor}) in Spiel {GameId} gefunden.");
        private static readonly Action<ILogger, Guid, Guid, string, string, Exception?> _logCardInstancePlayedAction =
            LoggerMessage.Define<Guid, Guid, string, string>(LogLevel.Debug, new EventId(SessionLogBaseId + 26, "CardInstancePlayedLog"), "[GameSession] Karte mit Instanz-ID {CardInstanceId} (Typ: {CardTypeId}) von Spieler {PlayerId} in Spiel {GameId} gespielt.");
        private static readonly Action<ILogger, Guid, Guid, string, Exception?> _logCardInstanceNotFoundInHandAction =
            LoggerMessage.Define<Guid, Guid, string>(LogLevel.Warning, new EventId(SessionLogBaseId + 27, "CardInstanceNotFoundInHandLog"), "[GameSession] Zu spielende Karteninstanz {CardInstanceId} nicht in Hand von Spieler {PlayerId} in Spiel {GameId} gefunden.");
        private static readonly Action<ILogger, Guid, Guid, Player, string, Exception?> _logPlayerAttemptingCardWhileInCheckAction =
             LoggerMessage.Define<Guid, Guid, Player, string>(LogLevel.Information, new EventId(SessionLogBaseId + 28, "PlayerAttemptingCardWhileInCheckLog"), "[GameSession] Spiel {GameId}: Spieler {PlayerId} (Farbe: {PlayerColor}) versucht Karte {CardTypeId} im Schach zu spielen. Prüfung nach Effekt.");
        private static readonly Action<ILogger, Guid, Guid, string, Exception?> _logPlayerStillInCheckAfterCardTurnNotEndedAction =
            LoggerMessage.Define<Guid, Guid, string>(LogLevel.Information, new EventId(SessionLogBaseId + 29, "PlayerStillInCheckAfterCardTurnNotEndedLog"), "[GameSession] Spiel {GameId}: Spieler {PlayerId} ist nach Kartenaktivierung von '{CardTypeId}' weiterhin im Schach. Zug wird nicht an Gegner übergeben.");
        private static readonly Action<ILogger, Guid, Guid, Player, string, string, Exception?> _logPlayerInCheckTriedInvalidMoveAction =
            LoggerMessage.Define<Guid, Guid, Player, string, string>(LogLevel.Warning, new EventId(SessionLogBaseId + 30, "PlayerInCheckTriedInvalidMoveLog"), "[GameSession] Spiel {GameId}: Spieler {PlayerId} (Farbe: {PlayerColor}) im Schach versuchte ungültigen Zug {From}->{To}.");
        private static readonly Action<ILogger, Guid, Guid, Player, string, string, Exception?> _logPlayerTriedMoveThatDidNotResolveCheckAction =
            LoggerMessage.Define<Guid, Guid, Player, string, string>(LogLevel.Warning, new EventId(SessionLogBaseId + 31, "PlayerTriedMoveThatDidNotResolveCheckLog"), "[GameSession] Spiel {GameId}: Spieler {PlayerId} (Farbe: {PlayerColor}) wählte Zug {From}->{To}, der das Schach nicht pariert.");
        private static readonly Action<ILogger, Guid, Guid, string, string, Exception?> _logExtraTurnFirstMoveCausesCheckAction =
            LoggerMessage.Define<Guid, Guid, string, string>(LogLevel.Warning, new EventId(SessionLogBaseId + 32, "ExtraTurnFirstMoveCausesCheckLog"), "[GameSession] Spiel {GameId}: Spieler {PlayerId} versuchte mit erstem Zug des Extrazugs ({FromSquare}->{ToSquare}) den Gegner Schach zu setzen. Ungültig.");
        private static readonly Action<ILogger, Guid, Player, string, string, Exception?> _logPawnPromotionPendingAfterCardAction =
            LoggerMessage.Define<Guid, Player, string, string>(LogLevel.Information, new EventId(SessionLogBaseId + 33, "LogPawnPromotionPendingAfterCard"), "[GameSession] Spiel {GameId}: Bauernumwandlung für Spieler {PlayerColor} auf Feld {PromotionSquare} nach Karteneffekt {CardTypeId} anstehend. Zug endet nicht.");
        private static readonly Action<ILogger, Guid, Player, Guid?, Guid?, Exception?> _logGetPlayerIdByColorFailedAction =
            LoggerMessage.Define<Guid, Player, Guid?, Guid?>(LogLevel.Debug, new EventId(SessionLogBaseId + 34, "SessionGetPlayerIdByColorFailedLog"), "[GameSession] Spiel {GameId}: GetPlayerIdByColor Anfrage für Farbe {Color}, aber kein Spieler zugewiesen. WhiteId: {WhiteId}, BlackId: {BlackId}");
        
        // EventId 23035 und 23036 sind nicht vergeben. Nächste freie ID: SessionLogBaseId + 37
        
        private static readonly Action<ILogger, Guid, string, double, Exception?> _logComputerTurnDelayAfterCardAction =
               LoggerMessage.Define<Guid, string, double>(LogLevel.Information, new EventId(SessionLogBaseId + 37, "LogComputerTurnDelayAfterCard"), "[GameSession] Karte {CardTypeId} vom Menschen gespielt. Verzögere Computerzug um {DelaySeconds}s in Spiel {GameId}.");
        private static readonly Action<ILogger, Guid, double, Exception?> _logComputerTurnDelayCardSwapAction =
            LoggerMessage.Define<Guid, double>(LogLevel.Information, new EventId(SessionLogBaseId + 38, "LogComputerTurnDelayCardSwap"), "[GameSession] CardSwap vom Menschen gespielt. Längere Verzögerung für Computerzug um {DelaySeconds}s in Spiel {GameId}.");
        private static readonly Action<ILogger, Guid, Player, Exception?> _logComputerTimerPausedForAnimationAction =
            LoggerMessage.Define<Guid, Player>(LogLevel.Debug, new EventId(SessionLogBaseId + 39, "LogComputerTimerPausedForAnimation"), "[GameSession] Timer für Computer ({ComputerColor}) in Spiel {GameId} für Animations-Delay pausiert.");
        private static readonly Action<ILogger, Guid, Player, Exception?> _logComputerTimerResumedAfterAnimationAction =
            LoggerMessage.Define<Guid, Player>(LogLevel.Debug, new EventId(SessionLogBaseId + 40, "LogComputerTimerResumedAfterAnimation"), "[GameSession] Timer für Computer ({ComputerColor}) in Spiel {GameId} nach Animations-Delay fortgesetzt.");
        private static readonly Action<ILogger, Guid, string, Exception?> _logComputerSkippingTurnAfterAnimationDelayAction =
            LoggerMessage.Define<Guid, string>(LogLevel.Warning, new EventId(SessionLogBaseId + 41, "LogComputerSkippingTurnAfterAnimationDelay"), "[GameSession] Spiel: {GameId} beendet oder nicht mehr Computer-Zug nach Animations-Delay für Karte {CardTypeId}. Computerzug wird nicht ausgeführt.");

        // --- ChessServer.Services.CardEffects Logs ---
        private static readonly Action<ILogger, Player, Guid, Guid, Exception?> _logAddTimeEffectAppliedAction =
            LoggerMessage.Define<Player, Guid, Guid>(LogLevel.Information, new EventId(CardEffectsLogBaseId + 1, "AddTimeEffectAppliedLog"), "[AddTimeEffect] Zeitgutschrift (+2 Min) für Spieler {PlayerColor} ({PlayerId}) in Spiel {GameId} angewendet.");
        private static readonly Action<ILogger, Guid, Guid, Guid, Guid, Exception?> _logCardSwapEffectExecutedGuidAction =
            LoggerMessage.Define<Guid, Guid, Guid, Guid>(LogLevel.Information, new EventId(CardEffectsLogBaseId + 2, "CardSwapEffectExecutedLog"), "[CardSwapEffect] Spieler {PlayerId} tauscht Karte mit Instanz-ID {SwappedOutPlayerCardInstanceId} gegen Gegnerkarte mit Instanz-ID {SwappedInOpponentCardInstanceId} in Spiel {GameId}.");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logCardSwapEffectOpponentNoCardsAction =
            LoggerMessage.Define<Guid, Guid>(LogLevel.Warning, new EventId(CardEffectsLogBaseId + 3, "CardSwapEffectOpponentNoCardsLog"), "[CardSwapEffect] Spieler {PlayerId} versuchte Kartentausch in Spiel {GameId}, aber Gegner hat keine Handkarten.");
        private static readonly Action<ILogger, Guid, Guid, Guid, Exception?> _logCardSwapEffectPlayerCardInstanceNotFoundAction =
            LoggerMessage.Define<Guid, Guid, Guid>(LogLevel.Warning, new EventId(CardEffectsLogBaseId + 4, "CardSwapEffectPlayerCardInstanceNotFoundLog"), "[CardSwapEffect] Spieler {PlayerId} versuchte Karte mit Instanz-ID {MissingCardInstanceId} zu tauschen (nicht in Hand) in Spiel {GameId}.");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logExtraZugEffectAppliedAction = 
            LoggerMessage.Define<Guid, Guid>(LogLevel.Information, new EventId(CardEffectsLogBaseId + 5, "ExtraZugCardEffectAppliedLog"), "[ExtraZugEffect] Extrazug für Spieler {PlayerId} in Spiel {GameId} vermerkt.");
        private static readonly Action<ILogger, string?, string?, Guid, Guid, Exception?> _logPositionSwapEffectExecutedAction =
            LoggerMessage.Define<string?, string?, Guid, Guid>(LogLevel.Information, new EventId(CardEffectsLogBaseId + 6, "PositionSwapEffectExecutedLog"), "[PositionSwapEffect] Positionstausch zwischen {FromSquare} und {ToSquare} für Spieler {PlayerId} in Spiel {GameId} ausgeführt.");
        private static readonly Action<ILogger, PieceType, string?, Player, Guid, Guid, Exception?> _logRebirthEffectExecutedAction =
            LoggerMessage.Define<PieceType, string?, Player, Guid, Guid>(LogLevel.Information, new EventId(CardEffectsLogBaseId + 7, "RebirthEffectExecutedLog"), "[RebirthEffect] Figur {PieceType} auf Feld {TargetSquare} für Spieler {PlayerColor} ({PlayerId}) in Spiel {GameId} wiederbelebt.");
        private static readonly Action<ILogger, string, string?, string?, Guid, Exception?> _logRebirthEffectFailedStringAction =
            LoggerMessage.Define<string, string?, string?, Guid>(LogLevel.Warning, new EventId(CardEffectsLogBaseId + 8, "RebirthEffectFailedStringLog"), "[RebirthEffect] Wiederbelebung von Figurentyp-String '{PieceTypeString}' auf {TargetSquare} in Spiel {GameId} fehlgeschlagen: {Reason}");
        private static readonly Action<ILogger, string, PieceType, string?, Guid, Exception?> _logRebirthEffectFailedEnumAction =
            LoggerMessage.Define<string, PieceType, string?, Guid>(LogLevel.Warning, new EventId(CardEffectsLogBaseId + 9, "RebirthEffectFailedEnumLog"), "[RebirthEffect] Wiederbelebung von Figurentyp {PieceTypeEnum} auf {TargetSquare} in Spiel {GameId} fehlgeschlagen: {Reason}");
        private static readonly Action<ILogger, Player, Player, Guid, Guid, Exception?> _logSubtractTimeEffectAppliedAction =
            LoggerMessage.Define<Player, Player, Guid, Guid>(LogLevel.Information, new EventId(CardEffectsLogBaseId + 10, "SubtractTimeEffectAppliedLog"), "[SubtractTimeEffect] Zeitdiebstahl (-2 Min) von Gegner {OpponentColor} durch Spieler {PlayerColor} ({PlayerId}) in Spiel {GameId} angewendet.");
        private static readonly Action<ILogger, string?, string?, Guid, Guid, Exception?> _logTeleportEffectExecutedAction =
            LoggerMessage.Define<string?, string?, Guid, Guid>(LogLevel.Information, new EventId(CardEffectsLogBaseId + 11, "TeleportEffectExecutedLog"), "[TeleportEffect] Teleport von {FromSquare} nach {ToSquare} für Spieler {PlayerId} in Spiel {GameId} ausgeführt.");
        private static readonly Action<ILogger, Player, Player, Guid, Exception?> _logTimeSwapEffectAppliedAction =
            LoggerMessage.Define<Player, Player, Guid>(LogLevel.Information, new EventId(CardEffectsLogBaseId + 12, "TimeSwapEffectAppliedLog"), "[TimeSwapEffect] Zeiten getauscht zwischen Spieler {Player1Color} und {Player2Color} in Spiel {GameId}.");
        private static readonly Action<ILogger, Guid, Guid, string, Exception?> _logSacrificeEffectExecutedAction =
            LoggerMessage.Define<Guid, Guid, string>(LogLevel.Information, new EventId(CardEffectsLogBaseId + 13, "SacrificeEffectExecutedLog"), "[SacrificeEffect] Spiel {GameId}: Spieler {PlayerId} opfert Bauer auf {SacrificedPawnSquare}.");
        private static readonly Action<ILogger, Guid, Guid, string, Exception?> _logSacrificeEffectFailedPawnNotFoundAction =
            LoggerMessage.Define<Guid, Guid, string>(LogLevel.Warning, new EventId(CardEffectsLogBaseId + 14, "SacrificeEffectFailedPawnNotFoundLog"), "[SacrificeEffect] Spiel {GameId}: Opfergabe für Spieler {PlayerId} fehlgeschlagen: Kein Bauer auf Feld {AttemptedSquare} gefunden.");
        private static readonly Action<ILogger, Guid, Guid, string, PieceType?, Exception?> _logSacrificeEffectFailedNotAPawnAction =
            LoggerMessage.Define<Guid, Guid, string, PieceType?>(LogLevel.Warning, new EventId(CardEffectsLogBaseId + 15, "SacrificeEffectFailedNotAPawnLog"), "[SacrificeEffect] Spiel {GameId}: Opfergabe für Spieler {PlayerId} fehlgeschlagen: Figur auf {AttemptedSquare} ist kein Bauer ({ActualType}).");
        private static readonly Action<ILogger, Guid, Guid, string, Player, Exception?> _logSacrificeEffectFailedWrongColorAction =
            LoggerMessage.Define<Guid, Guid, string, Player>(LogLevel.Warning, new EventId(CardEffectsLogBaseId + 16, "SacrificeEffectFailedWrongColorLog"), "[SacrificeEffect] Spiel {GameId}: Opfergabe für Spieler {PlayerId} fehlgeschlagen: Bauer auf {AttemptedSquare} gehört nicht diesem Spieler (Farbe: {PieceColor}).");
        private static readonly Action<ILogger, Guid, Guid, string, Exception?> _logSacrificeEffectFailedWouldCauseCheckAction =
            LoggerMessage.Define<Guid, Guid, string>(LogLevel.Warning, new EventId(CardEffectsLogBaseId + 17, "SacrificeEffectFailedWouldCauseCheckLog"), "[SacrificeEffect] Spiel {GameId}: Opfergabe von Bauer auf {SacrificedPawnSquare} für Spieler {PlayerId} nicht möglich, da eigener König dadurch ins Schach geraten würde.");

        // --- ChessServer.Services.InMemoryGameManager.cs Logs ---
        private static readonly Action<ILogger, Guid, string, Guid, Player, int, Exception?> _logMgrGameCreatedAction =
            LoggerMessage.Define<Guid, string, Guid, Player, int>(LogLevel.Information, new EventId(GameManagerLogBaseId + 1, "MgrGameCreatedLog"), "Spiel {GameId} erstellt von Spieler {PlayerName} ({PlayerId}) mit Farbe {Color} und Startzeit {InitialMinutes} Minuten");
        private static readonly Action<ILogger, string, Guid, Player, Exception?> _logMgrPlayerJoinedGameTimerStartAction =
            LoggerMessage.Define<string, Guid, Player>(LogLevel.Information, new EventId(GameManagerLogBaseId + 2, "MgrPlayerJoinedGameTimerStartLog"), "Zweiter Spieler {PlayerName} ist Spiel {GameId} beigetreten. Starte Timer für {StartPlayer}.");
        private static readonly Action<ILogger, Guid, Exception?> _logMgrGameOverTimerStopAction =
            LoggerMessage.Define<Guid>(LogLevel.Information, new EventId(GameManagerLogBaseId + 3, "MgrGameOverTimerStopLog"), "Spiel {GameId} ist vorbei. Timer wird gestoppt.");
        private static readonly Action<ILogger, Guid, Guid, string, Exception?> _logMgrGameNotFoundForCardActivationAction =
            LoggerMessage.Define<Guid, Guid, string>(LogLevel.Warning, new EventId(GameManagerLogBaseId + 4, "MgrGameNotFoundForCardActivationLog"), "[InMemoryGameManager] Spiel {GameId} nicht gefunden für Kartenaktivierung von Spieler {PlayerId}, Karte {CardTypeId}.");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logMgrGameNotFoundForCapturedPiecesAction =
            LoggerMessage.Define<Guid, Guid>(LogLevel.Warning, new EventId(GameManagerLogBaseId + 5, "MgrGameNotFoundForCapturedPiecesLog"), "[InMemoryGameManager] Spiel {GameId} nicht gefunden beim Abrufen der geschlagenen Figuren für Spieler {PlayerId}.");
        private static readonly Action<ILogger, Guid, Exception?> _logMgrGameNotFoundForGetPlayerIdByColorAction =
            LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(GameManagerLogBaseId + 6, "MgrGameNotFoundForGetPlayerIdByColorLog"), "[InMemoryGameManager] Spiel {GameId} nicht gefunden in GetPlayerIdByColor.");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logMgrGameNotFoundForGetOpponentInfoAction =
            LoggerMessage.Define<Guid, Guid>(LogLevel.Warning, new EventId(GameManagerLogBaseId + 7, "MgrGameNotFoundForGetOpponentInfoLog"), "[InMemoryGameManager] Spiel {GameId} nicht gefunden für GetOpponentInfo von Spieler {CurrentPlayerId}.");
        private static readonly Action<ILogger, Guid, Guid, string, Exception?> _logMgrPlayerHubConnectionRegisteredAction =
            LoggerMessage.Define<Guid, Guid, string>(LogLevel.Information, new EventId(GameManagerLogBaseId + 8, "MgrPlayerHubConnectionRegisteredLog"), "[InMemoryGameManager] Hub-Verbindung für Spieler {PlayerId} zu Spiel {GameId} registriert (Connection: {ConnectionId}).");
        private static readonly Action<ILogger, string, Guid, Exception?> _logMgrPlayerHubConnectionUnregisteredAction =
            LoggerMessage.Define<string, Guid>(LogLevel.Information, new EventId(GameManagerLogBaseId + 9, "MgrPlayerHubConnectionUnregisteredLog"), "[InMemoryGameManager] Hub-Verbindung {ConnectionId} von Spiel {GameId} deregistriert.");
        private static readonly Action<ILogger, Guid, Exception?> _logMgrGameNotFoundForRegisterPlayerHubAction =
            LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(GameManagerLogBaseId + 10, "MgrGameNotFoundForRegisterPlayerHubLog"), "[InMemoryGameManager] Spiel {GameId} nicht gefunden für RegisterPlayerHubConnection.");

        // --- ChessServer PvC Logs ---
        private static readonly Action<ILogger, Guid, string, Player, string, Exception?> _logPvCGameCreatedAction =
            LoggerMessage.Define<Guid, string, Player, string>(LogLevel.Information, new EventId(PvcLogBaseId + 1, "CtrlPvCGameCreatedLog"), "PvC Spiel {GameId} erstellt für Spieler {PlayerName} (Farbe: {PlayerColor}) gegen Computer (Stärke: {ComputerDifficulty})");
        private static readonly Action<ILogger, Guid, string, int, Exception?> _logComputerFetchingMoveAction =
            LoggerMessage.Define<Guid, string, int>(LogLevel.Debug, new EventId(PvcLogBaseId + 2, "SessionComputerFetchingMoveLog"), "[GameSession] Spiel {GameId}: Computer fordert Zug von API an. FEN: {FEN}, Tiefe: {Depth}");
        private static readonly Action<ILogger, Guid, string, string, int, Exception?> _logComputerReceivedMoveAction =
            LoggerMessage.Define<Guid, string, string, int>(LogLevel.Information, new EventId(PvcLogBaseId + 3, "SessionComputerReceivedMoveLog"), "[GameSession] Spiel {GameId}: Computer erhielt Zug '{Move}' von API für FEN {FEN}, Tiefe {Depth}");
        private static readonly Action<ILogger, Guid, string, int, string, Exception?> _logComputerMoveErrorAction =
            LoggerMessage.Define<Guid, string, int, string>(LogLevel.Warning, new EventId(PvcLogBaseId + 4, "SessionComputerMoveErrorLog"), "[GameSession] Spiel {GameId}: Fehler bei Computerzug (FEN: {FEN}, Tiefe: {Depth}). Grund: {ErrorMessage}");
        private static readonly Action<ILogger, Guid, string, string, Exception?> _logComputerMakingMoveAction =
            LoggerMessage.Define<Guid, string, string>(LogLevel.Information, new EventId(PvcLogBaseId + 5, "SessionComputerMakingMoveLog"), "[GameSession] Spiel {GameId}: Computer führt Zug {FromSquare} -> {ToSquare} aus.");
        private static readonly Action<ILogger, Guid, Player, Player, Exception?> _logComputerStartingInitialMoveAction =
            LoggerMessage.Define<Guid, Player, Player>(LogLevel.Debug, new EventId(PvcLogBaseId + 6, "SessionComputerStartingInitialMoveLog"), "[GameSession] Spiel {GameId}: Computer (Farbe: {ComputerColor}, aktuell am Zug: {CurrentPlayer}) startet. Planne initialen Zug.");

        // --- ChessServer.Services.GameTimerService.cs Logs ---
        private static readonly Action<ILogger, Guid, Player?, TimeSpan, TimeSpan, Exception?> _logTimerStartingAction =
            LoggerMessage.Define<Guid, Player?, TimeSpan, TimeSpan>(LogLevel.Information, new EventId(TimerLogBaseId + 1, "TimerStartingLog"), "[GameTimerService] Timer für Spiel {GameId}, Spieler {Player} wird gestartet. W: {WhiteTime}, B: {BlackTime}");
        private static readonly Action<ILogger, Guid, Player?, Exception?> _logTimerSwitchingAction =
            LoggerMessage.Define<Guid, Player?>(LogLevel.Information, new EventId(TimerLogBaseId + 2, "TimerSwitchingLog"), "[GameTimerService] Timer für Spiel {GameId} wird auf Spieler {Player} umgeschaltet.");
        private static readonly Action<ILogger, Player?, double, Guid, Exception?> _logTimerStoppedAndCalculatedAction =
            LoggerMessage.Define<Player?, double, Guid>(LogLevel.Debug, new EventId(TimerLogBaseId + 3, "TimerStoppedAndCalculatedLog"), "[GameTimerService] Timer gestoppt für Spieler {Player} in Spiel {GameId}. Vergangene Zeit: {ElapsedSeconds}s.");
        private static readonly Action<ILogger, Guid, Player?, TimeSpan, TimeSpan, Exception?> _logTimerTickTraceAction =
            LoggerMessage.Define<Guid, Player?, TimeSpan, TimeSpan>(LogLevel.Trace, new EventId(TimerLogBaseId + 4, "TimerTickTraceLog"), "[GameTimerService] Tick für Spieler {Player} in Spiel {GameId}. W: {WhiteTime}, B: {BlackTime}");
        private static readonly Action<ILogger, Player, Guid, Exception?> _logPlayerTimeExpiredAction =
            LoggerMessage.Define<Player, Guid>(LogLevel.Information, new EventId(TimerLogBaseId + 5, "PlayerTimeExpiredLog"), "[GameTimerService] Zeit für Spieler {Player} in Spiel {GameId} abgelaufen.");
        private static readonly Action<ILogger, Guid, Exception?> _logTimerDisposedAction =
            LoggerMessage.Define<Guid>(LogLevel.Trace, new EventId(TimerLogBaseId + 6, "TimerDisposedLog"), "[GameTimerService] Interner Timer gestoppt/entsorgt für Spiel {GameId}.");
        private static readonly Action<ILogger, Guid, Exception?> _logGameOverTimerStoppedAction =
            LoggerMessage.Define<Guid>(LogLevel.Information, new EventId(TimerLogBaseId + 7, "GameOverTimerStoppedLog"), "[GameTimerService] Spiel {GameId} als beendet markiert, Timer gestoppt.");
        private static readonly Action<ILogger, TimeSpan, Player, Guid, TimeSpan, TimeSpan, Exception?> _logTimeAdjustedTimerAction =
           LoggerMessage.Define<TimeSpan, Player, Guid, TimeSpan, TimeSpan>(LogLevel.Information, new EventId(TimerLogBaseId + 8, "TimeAdjustedTimerLog"), "[GameTimerService] {TimeAmount} für Spieler {Player} in Spiel {GameId} angepasst. W: {WhiteTime}, B: {BlackTime}");
        private static readonly Action<ILogger, Player, Player, Guid, TimeSpan, TimeSpan, Exception?> _logTimeSwappedTimerAction =
            LoggerMessage.Define<Player, Player, Guid, TimeSpan, TimeSpan>(LogLevel.Information, new EventId(TimerLogBaseId + 9, "TimeSwappedTimerLog"), "[GameTimerService] Zeiten zwischen Spieler {Player1} und {Player2} in Spiel {GameId} getauscht. W: {WhiteTime}, B: {BlackTime}");
        private static readonly Action<ILogger, Player, Guid, Exception?> _logTimeExpiredAfterManipulationAction =
            LoggerMessage.Define<Player, Guid>(LogLevel.Information, new EventId(TimerLogBaseId + 10, "TimeExpiredAfterManipulationLog"), "[GameTimerService] Zeit für Spieler {Player} in Spiel {GameId} durch Manipulation auf 0 oder weniger gefallen und als abgelaufen markiert.");
        private static readonly Action<ILogger, Guid, Player?, Exception?> _logTimerPausedAction =
            LoggerMessage.Define<Guid, Player?>(LogLevel.Debug, new EventId(TimerLogBaseId + 11, "TimerPausedLog"), "[GameTimerService] Timer für Spiel {GameId}, Spieler {ActivePlayer} pausiert.");
        private static readonly Action<ILogger, Guid, Player?, Exception?> _logTimerResumedAction =
            LoggerMessage.Define<Guid, Player?>(LogLevel.Debug, new EventId(TimerLogBaseId + 12, "TimerResumedLog"), "[GameTimerService] Timer für Spiel {GameId}, Spieler {ActivePlayer} fortgesetzt.");

        public ChessLogger(ILogger<TCategoryName> msLogger)
        {
            _msLogger = msLogger ?? throw new ArgumentNullException(nameof(msLogger));
        }

        // Implementierungen der IChessLogger-Methoden

        public void LogStartingGenericCardSwapAnimation(Guid playerId, string cardGivenName, string cardReceivedName) => _logStartingGenericCardSwapAnimationAction(_msLogger, playerId, cardGivenName, cardReceivedName, null);
        public void LogCardActivationAnimationFinishedClient() => _logCardActivationAnimationFinishedClientAction(_msLogger, null);
        public void LogSpecificCardSwapAnimationFinishedClient() => _logSpecificCardSwapAnimationFinishedClientAction(_msLogger, null);
        public void LogUpdatePlayerNamesMismatch(Player opponentColorRetrieved, Player opponentColorExpected) => _logUpdatePlayerNamesMismatchAction(_msLogger, opponentColorRetrieved, opponentColorExpected, null);
        public void LogUpdatePlayerNamesNotFound(string errorMessage) => _logUpdatePlayerNamesNotFoundAction(_msLogger, errorMessage, null);
        public void LogUpdatePlayerNamesError(Exception ex) => _logUpdatePlayerNamesErrorAction(_msLogger, ex);
        public void LogClientAttemptedToAddDuplicateCardInstance(Guid instanceId, string cardName) => _logClientAttemptedToAddDuplicateCardInstanceAction(_msLogger, instanceId, cardName, null);
        public void LogHandlePlayCardActivationAnimation(string cardTypeId, Guid playerId, Player playerColor) => _logHandlePlayCardActivationAnimationAction(_msLogger, cardTypeId, playerId, playerColor, null);
        public void LogHandleClientAnimationFinishedTriggered(string? lastCardId, bool isPendingSwapNull) => _logHandleClientAnimationFinishedTriggeredAction(_msLogger, lastCardId, isPendingSwapNull, null);
        public void LogHandleReceiveCardSwapDetails(string givenCardName, string receivedCardName, bool isGenericAnimating) => _logHandleReceiveCardSwapDetailsAction(_msLogger, givenCardName, receivedCardName, isGenericAnimating, null);
        public void LogActuallyStartingSpecificSwapAnim(string givenCardName, string receivedCardName) => _logActuallyStartingSpecificSwapAnimAction(_msLogger, givenCardName, receivedCardName, null);
        public void LogGenericCardAnimationStartedForCard(string cardName) => _logGenericAnimationStartedForCardAction(_msLogger, cardName, null);
        public void LogClientCriticalServicesNullOnInit(string serviceName) => _logClientCriticalServicesNullOnInitAction(_msLogger, serviceName, null);
        public void LogClientSignalRConnectionWarning(string errorMessage) => _logClientSignalRConnectionWarningAction(_msLogger, errorMessage, null);
        public void LogMoveProcessingError(Guid gameId, string fromSquare, string toSquare, Exception? ex) => _logMoveErrorAction(_msLogger, gameId, fromSquare, toSquare, ex);
        public void LogGameCreated(Guid gameId, string playerName, int initialMinutes) => _logGameCreatedAction(_msLogger, gameId, playerName, initialMinutes, null);
        public void LogPlayerJoinedGame(string playerName, Guid gameId) => _logPlayerJoinedGameAction(_msLogger, playerName, gameId, null);
        public void LogGameNotFoundOnJoinAttempt(Guid gameId) => _logGameNotFoundOnJoinAttemptAction(_msLogger, gameId, null);
        public void LogInvalidOperationOnJoinGame(Guid gameId, string errorMessage) => _logInvalidOperationOnJoinGameAction(_msLogger, gameId, errorMessage, null);
        public void LogApplyMoveInfo(Guid gameId, Guid playerId, bool isValid) => _logApplyMoveInfoAction(_msLogger, gameId, playerId, isValid, null);
        public void LogSignalRUpdateInfo(Guid gameId, Player nextPlayerTurn, GameStatusDto statusForNextPlayer, string? lastMoveFrom, string? lastMoveTo, string? cardEffectSquareCount) => _logSignalRUpdateInfoAction(_msLogger, gameId, nextPlayerTurn, statusForNextPlayer, lastMoveFrom, lastMoveTo, cardEffectSquareCount, null);
        public void LogOnTurnChangedSentToHub(Guid gameId) => _logOnTurnChangedSentToHubAction(_msLogger, gameId, null);
        public void LogOnTimeUpdateSentAfterMove(Guid gameId) => _logOnTimeUpdateSentAfterMoveAction(_msLogger, gameId, null);
        public void LogGameNotFoundOnMove(Guid gameId, Guid playerId) => _logGameNotFoundOnMoveAction(_msLogger, gameId, playerId, null);
        public void LogInvalidOperationOnMove(Guid gameId, Guid playerId) => _logInvalidOperationOnMoveAction(_msLogger, gameId, playerId, null);
        public void LogGameNotFoundOnTimeRequest(Guid gameId) => _logGameNotFoundOnTimeRequestAction(_msLogger, gameId, null);
        public void LogErrorGettingTime(Guid gameId, Exception? ex) => _logErrorGettingTimeAction(_msLogger, gameId, ex);
        public void LogGameHistoryNullFromManager(Guid gameId) => _logGameHistoryNullFromManagerAction(_msLogger, gameId, null);
        public void LogGameHistoryKeyNotFound(Guid gameId) => _logGameHistoryKeyNotFoundAction(_msLogger, gameId, null);
        public void LogGameHistoryGenericError(Guid gameId, Exception? ex) => _logGameHistoryGenericErrorAction(_msLogger, gameId, ex);
        public void LogCardActivationAttempt(Guid gameId, Guid playerId, string cardTypeId) => _logCardActivationAttemptAction(_msLogger, gameId, playerId, cardTypeId, null);
        public void LogCardActivationFailedController(Guid gameId, Guid playerId, string cardTypeId, string reason) => _logCardActivationFailedControllerAction(_msLogger, gameId, playerId, cardTypeId, reason, null);
        public void LogCardActivationSuccessController(Guid gameId, Guid playerId, string cardTypeId) => _logCardActivationSuccessControllerAction(_msLogger, gameId, playerId, cardTypeId, null);
        public void LogSignalingPlayerToDrawCard(Guid gameId, Guid playerIdToDraw) => _logSignalingPlayerToDrawCardAction(_msLogger, gameId, playerIdToDraw, null);
        public void LogGettingCapturedPieces(Guid gameId, Guid playerId) => _logGettingCapturedPiecesAction(_msLogger, gameId, playerId, null);
        public void LogGameNotFoundCapturedPieces(Guid gameId, Guid playerId) => _logGameNotFoundCapturedPiecesAction(_msLogger, gameId, playerId, null);
        public void LogErrorGettingCapturedPieces(Guid gameId, Guid playerId, Exception? ex) => _logErrorGettingCapturedPiecesAction(_msLogger, gameId, playerId, ex);
        public void LogControllerGameNotFound(Guid gameId, string actionName) => _logControllerGameNotFoundAction(_msLogger, gameId, actionName, null);
        public void LogControllerMoveSentCardToHand(string cardName, string connectionId, Guid playerId) => _logCtrlMoveSentCardToHandAction(_msLogger, cardName, connectionId, playerId, null);
        public void LogControllerConnectionIdNotFoundNoMoreCards(string actionSource, Guid playerId) => _logCtrlConnIdNotFoundNoMoreCardsAction(_msLogger, actionSource, playerId, null);
        public void LogControllerConnectionIdNotFoundGeneric(string actionSource, Guid playerId) => _logCtrlConnIdNotFoundGenericAction(_msLogger, actionSource, playerId, null);
        public void LogControllerActivateCardSentCardToHand(string cardName, string connectionId, Guid playerId) => _logCtrlActivateCardSentCardToHandAction(_msLogger, cardName, connectionId, playerId, null);
        public void LogControllerCouldNotDeterminePlayerIdForStatus(Guid gameId, Player playerColor) => _logControllerCouldNotDeterminePlayerIdForStatusAction(_msLogger, gameId, playerColor, null);
        public void LogControllerErrorGettingOpponentInfo(Guid gameId, Guid playerId, Exception? ex) => _logControllerErrorGettingOpponentInfoAction(_msLogger, gameId, playerId, ex);
        public void LogControllerErrorGettingLegalMoves(Guid gameId, Guid playerId, string fromSquare, Exception? ex) => _logControllerErrorGettingLegalMovesAction(_msLogger, gameId, playerId, fromSquare, ex);
        public void LogControllerConnectionIdForPlayerNotFound(Guid playerId) => _logCtrlConnIdForPlayerNotFoundAction(_msLogger, playerId, null);
        public void LogHubClientConnected(string connectionId) => _logHubClientConnectedAction(_msLogger, connectionId, null);
        public void LogHubClientDisconnected(string connectionId, string? errorMessage, Exception? ex) => _logHubClientDisconnectedAction(_msLogger, connectionId, errorMessage, ex);
        public void LogHubClientJoiningGameGroup(string connectionId, string gameIdString) => _logHubClientJoiningGameGroupAction(_msLogger, connectionId, gameIdString, null);
        public void LogHubClientAddedToGameGroup(string connectionId, string gameIdString) => _logHubClientAddedToGameGroupAction(_msLogger, connectionId, gameIdString, null);
        public void LogHubPlayerJoinedNotificationSent(string joiningPlayerName, string gameIdString, int playerCount) => _logHubPlayerJoinedNotificationSentAction(_msLogger, joiningPlayerName, gameIdString, playerCount, null);
        public void LogHubClientLeavingGameGroup(string connectionId, string gameIdString) => _logHubClientLeavingGameGroupAction(_msLogger, connectionId, gameIdString, null);
        public void LogHubClientRemovedFromGameGroup(string connectionId, string gameIdString) => _logHubClientRemovedFromGameGroupAction(_msLogger, connectionId, gameIdString, null);
        public void LogHubGameNotFoundForPlayerCount(Guid gameIdGuid, string connectionId) => _logHubGameNotFoundForPlayerCountAction(_msLogger, gameIdGuid, connectionId, null);
        public void LogHubJoinGameInvalidGameIdFormat(string gameIdString, string connectionId) => _logHubJoinGameInvalidGameIdFormatAction(_msLogger, gameIdString, connectionId, null);
        public void LogHubPlayerRegisteredToHub(Guid playerId, string connectionId, Guid gameId) => _logHubPlayerRegisteredToHubAction(_msLogger, playerId, connectionId, gameId, null);
        public void LogHubPlayerDeregisteredFromHub(string connectionId) => _logHubPlayerDeregisteredFromHubAction(_msLogger, connectionId, null);
        public void LogHubSendingInitialHand(Guid playerId, string connectionId, Guid gameId, int handSize, int drawPileCount) => _logHubSendingInitialHandAction(_msLogger, playerId, connectionId, gameId, handSize, drawPileCount, null);
        public void LogHubFailedToSendInitialHandSessionNotFound(Guid gameId, Guid playerId) => _logHubFailedToSendInitialHandSessionNotFoundAction(_msLogger, gameId, playerId, null);
        public void LogHubPlayerActuallyJoinedGame(string playerName, string gameIdString) => _logHubPlayerActuallyJoinedGameAction(_msLogger, playerName, gameIdString, null);
        public void LogHubPlayerMappingRemovedOnDisconnect(Guid playerId) => _logHubPlayerMappingRemovedOnDisconnectAction(_msLogger, playerId, null);
        public void LogHubConnectionRemovedFromGameOnDisconnect(string connectionId, Guid gameId) => _logHubConnectionRemovedFromGameOnDisconnectAction(_msLogger, connectionId, gameId, null);
        public void LogHubErrorSendingInitialHand(Guid playerId, Guid gameId, Exception? ex) => _logHubErrorSendingInitialHandAction(_msLogger, playerId, gameId, ex);
        public void LogSessionErrorGetNameByColor(Guid gameId, Exception? ex) => _logSessionErrorGetNameByColorAction(_msLogger, gameId, ex);
        public void LogSessionSendTimeUpdate(Guid gameId, TimeSpan whiteTime, TimeSpan blackTime, Player? activePlayer) => _logSessionSendTimeUpdateAction(_msLogger, gameId, whiteTime, blackTime, activePlayer, null);
        public void LogSessionErrorIsPlayerTurn(Guid gameId, Exception? ex) => _logSessionErrorIsPlayerTurnAction(_msLogger, gameId, ex);
        public void LogSessionColorNotDetermined(Guid gameId, Guid playerId, int playerCount) => _logSessionColorNotDeterminedAction(_msLogger, gameId, playerId, playerCount, null);
        public void LogGameEndedByTimeoutInSession(Guid gameId, Player expiredPlayer) => _logGameEndedByTimeoutInSessionAction(_msLogger, gameId, expiredPlayer, null);
        public void LogSessionCardActivationAttempt(Guid gameId, Guid playerId, string cardId) => _logSessionCardActivationAttemptAction(_msLogger, gameId, playerId, cardId, null);
        public void LogSessionCardActivationFailed(Guid gameId, Guid playerId, string cardId, string reason) => _logSessionCardActivationFailedAction(_msLogger, gameId, playerId, cardId, reason, null);
        public void LogSessionCardActivationSuccess(Guid gameId, Guid playerId, string cardId) => _logSessionCardActivationSuccessAction(_msLogger, gameId, playerId, cardId, null);
        public void LogExtraTurnEffectApplied(Guid gameId, Guid playerId, string cardId) => _logExtraTurnEffectAppliedAction(_msLogger, gameId, playerId, cardId, null);
        public void LogPlayerMoveCountIncreased(Guid gameId, Guid playerId, int moveCount) => _logPlayerMoveCountIncreasedAction(_msLogger, gameId, playerId, moveCount, null);
        public void LogPlayerCardDrawIndicated(Guid gameId, Guid playerId) => _logPlayerCardDrawIndicatedAction(_msLogger, gameId, playerId, null);
        public void LogNotifyingOpponentOfCardPlay(Guid gameId, Guid playerId, string cardId) => _logNotifyingOpponentOfCardPlayAction(_msLogger, gameId, playerId, cardId, null);
        public void LogCapturedPieceAdded(Guid gameId, PieceType pieceType, Player playerColor) => _logCapturedPieceAddedAction(_msLogger, gameId, pieceType, playerColor, null);
        public void LogPawnPromotionMoveSelection(Guid gameId, string from, string toSquare, PieceType? promotionType) => _logPawnPromotionMoveSelectionAction(_msLogger, gameId, from, toSquare, promotionType, null);
        public void LogPawnPromotionMoveFound(Guid gameId, string from, string toSquare, PieceType promotionType) => _logPawnPromotionMoveFoundAction(_msLogger, gameId, from, toSquare, promotionType, null);
        public void LogPawnPromotionMoveNotFound(Guid gameId, string from, string toSquare, PieceType? promotionType) => _logPawnPromotionMoveNotFoundAction(_msLogger, gameId, from, toSquare, promotionType, null);
        public void LogOnTurnChangedFromSession(Guid gameId, string? lastMoveFrom, string? lastMoveTo, string cardEffectType) => _logOnTurnChangedFromSessionAction(_msLogger, gameId, lastMoveFrom, lastMoveTo, cardEffectType, null);
        public void LogPlayerDeckInitialized(Guid playerId, Guid gameId, int drawPileCount) => _logPlayerDeckInitializedAction(_msLogger, playerId, gameId, drawPileCount, null);
        public void LogDrawAttemptUnknownPlayer(Guid playerId, Guid gameId) => _logDrawAttemptUnknownPlayerAction(_msLogger, playerId, gameId, null);
        public void LogNoDrawPileForPlayer(Guid playerId, Guid gameId) => _logNoDrawPileForPlayerAction(_msLogger, playerId, gameId, null);
        public void LogPlayerDrawPileEmpty(Guid playerId, Guid gameId) => _logPlayerDrawPileEmptyAction(_msLogger, playerId, gameId, null);
        public void LogPlayerDrewCardFromOwnDeck(Guid playerId, string cardName, string cardId, Guid gameId, int remainingInPlayerDrawPile) => _logPlayerDrewCardFromOwnDeckAction(_msLogger, playerId, cardName, cardId, gameId, remainingInPlayerDrawPile, null);
        public void LogCannotFindPlayerDrawPileForCount(Guid playerId, Guid gameId) => _logCannotFindPlayerDrawPileForCountAction(_msLogger, playerId, gameId, null);
        public void LogCurrentPlayerNotFoundForOpponentDetails(Guid currentPlayerId, Guid gameId) => _logCurrentPlayerNotFoundForOpponentDetailsAction(_msLogger, currentPlayerId, gameId, null);
        public void LogNoOpponentFoundForPlayer(Guid currentPlayerId, Player currentPlayerColor, Guid gameId) => _logNoOpponentFoundForPlayerAction(_msLogger, currentPlayerId, currentPlayerColor, gameId, null);
        public void LogCardInstancePlayed(Guid cardInstanceId, Guid playerId, string cardTypeId, string gameId) => _logCardInstancePlayedAction(_msLogger, cardInstanceId, playerId, cardTypeId, gameId, null);
        public void LogCardInstanceNotFoundInHand(Guid cardInstanceId, Guid playerId, string gameId) => _logCardInstanceNotFoundInHandAction(_msLogger, cardInstanceId, playerId, gameId, null);
        public void LogAddTimeEffectApplied(Player playerColor, Guid playerId, Guid gameId) => _logAddTimeEffectAppliedAction(_msLogger, playerColor, playerId, gameId, null);
        public void LogCardSwapEffectExecuted(Guid swappedOutPlayerCardInstanceId, Guid swappedInOpponentCardInstanceId, Guid playerId, Guid gameId) => _logCardSwapEffectExecutedGuidAction(_msLogger, swappedOutPlayerCardInstanceId, swappedInOpponentCardInstanceId, playerId, gameId, null);
        public void LogCardSwapEffectOpponentNoCards(Guid playerId, Guid gameId) => _logCardSwapEffectOpponentNoCardsAction(_msLogger, playerId, gameId, null);
        public void LogCardSwapEffectPlayerCardInstanceNotFound(Guid missingCardInstanceId, Guid playerId, Guid gameId) => _logCardSwapEffectPlayerCardInstanceNotFoundAction(_msLogger, missingCardInstanceId, playerId, gameId, null);
        public void LogExtraZugEffectApplied(Guid playerId, Guid gameId) => _logExtraZugEffectAppliedAction(_msLogger, playerId, gameId, null);
        public void LogPositionSwapEffectExecuted(string? fromSquare, string? toSquare, Guid playerId, Guid gameId) => _logPositionSwapEffectExecutedAction(_msLogger, fromSquare, toSquare, playerId, gameId, null);
        public void LogRebirthEffectExecuted(PieceType pieceType, string? targetSquare, Player playerColor, Guid playerId, Guid gameId) => _logRebirthEffectExecutedAction(_msLogger, pieceType, targetSquare, playerColor, playerId, gameId, null);
        public void LogRebirthEffectFailedString(string reason, string? pieceTypeString, string? targetSquare, Guid gameId) => _logRebirthEffectFailedStringAction(_msLogger, reason, pieceTypeString, targetSquare, gameId, null);
        public void LogRebirthEffectFailedEnum(string reason, PieceType pieceTypeEnum, string? targetSquare, Guid gameId) => _logRebirthEffectFailedEnumAction(_msLogger, reason, pieceTypeEnum, targetSquare, gameId, null);
        public void LogSubtractTimeEffectApplied(Player opponentColor, Player playerColor, Guid playerId, Guid gameId) => _logSubtractTimeEffectAppliedAction(_msLogger, opponentColor, playerColor, playerId, gameId, null);
        public void LogTeleportEffectExecuted(string? fromSquare, string? toSquare, Guid playerId, Guid gameId) => _logTeleportEffectExecutedAction(_msLogger, fromSquare, toSquare, playerId, gameId, null);
        public void LogTimeSwapEffectApplied(Player player1Color, Player player2Color, Guid gameId) => _logTimeSwapEffectAppliedAction(_msLogger, player1Color, player2Color, gameId, null);
        public void LogMgrGameCreated(Guid gameId, string playerName, Guid playerId, Player color, int initialMinutes) => _logMgrGameCreatedAction(_msLogger, gameId, playerName, playerId, color, initialMinutes, null);
        public void LogMgrPlayerJoinedGameTimerStart(string playerName, Guid gameId, Player startPlayer) => _logMgrPlayerJoinedGameTimerStartAction(_msLogger, playerName, gameId, startPlayer, null);
        public void LogMgrGameOverTimerStop(Guid gameId) => _logMgrGameOverTimerStopAction(_msLogger, gameId, null);
        public void LogMgrGameNotFoundForCardActivation(Guid gameId, Guid playerId, string cardTypeId) => _logMgrGameNotFoundForCardActivationAction(_msLogger, gameId, playerId, cardTypeId, null);
        public void LogMgrGameNotFoundForCapturedPieces(Guid gameId, Guid playerId) => _logMgrGameNotFoundForCapturedPiecesAction(_msLogger, gameId, playerId, null);
        public void LogMgrGameNotFoundForGetPlayerIdByColor(Guid gameId) => _logMgrGameNotFoundForGetPlayerIdByColorAction(_msLogger, gameId, null);
        public void LogMgrGameNotFoundForGetOpponentInfo(Guid gameId, Guid currentPlayerId) => _logMgrGameNotFoundForGetOpponentInfoAction(_msLogger, gameId, currentPlayerId, null);
        public void LogMgrPlayerHubConnectionRegistered(Guid playerId, Guid gameId, string connectionId) => _logMgrPlayerHubConnectionRegisteredAction(_msLogger, playerId, gameId, connectionId, null);
        public void LogMgrPlayerHubConnectionUnregistered(string connectionId, Guid gameId) => _logMgrPlayerHubConnectionUnregisteredAction(_msLogger, connectionId, gameId, null);
        public void LogMgrGameNotFoundForRegisterPlayerHub(Guid gameId) => _logMgrGameNotFoundForRegisterPlayerHubAction(_msLogger, gameId, null);
        public void LogPlayerAttemptingCardWhileInCheck(Guid gameId, Guid playerId, Player playerColor, string cardTypeId) => _logPlayerAttemptingCardWhileInCheckAction(_msLogger, gameId, playerId, playerColor, cardTypeId, null);
        public void LogPlayerStillInCheckAfterCardTurnNotEnded(Guid gameId, Guid playerId, string cardTypeId) => _logPlayerStillInCheckAfterCardTurnNotEndedAction(_msLogger, gameId, playerId, cardTypeId, null);
        public void LogPlayerInCheckTriedInvalidMove(Guid gameId, Guid playerId, Player playerColor, string fromSquare, string toSquare) => _logPlayerInCheckTriedInvalidMoveAction(_msLogger, gameId, playerId, playerColor, fromSquare, toSquare, null);
        public void LogPlayerTriedMoveThatDidNotResolveCheck(Guid gameId, Guid playerId, Player playerColor, string fromSquare, string toSquare) => _logPlayerTriedMoveThatDidNotResolveCheckAction(_msLogger, gameId, playerId, playerColor, fromSquare, toSquare, null);
        public void LogExtraTurnFirstMoveCausesCheck(Guid gameId, Guid playerId, string fromSquare, string toSquare) => _logExtraTurnFirstMoveCausesCheckAction(_msLogger, gameId, playerId, fromSquare, toSquare, null);
        public void LogSacrificeEffectExecuted(Guid gameId, Guid playerId, string sacrificedPawnSquare) => _logSacrificeEffectExecutedAction(_msLogger, gameId, playerId, sacrificedPawnSquare, null);
        public void LogSacrificeEffectFailedPawnNotFound(Guid gameId, Guid playerId, string attemptedSquare) => _logSacrificeEffectFailedPawnNotFoundAction(_msLogger, gameId, playerId, attemptedSquare, null);
        public void LogSacrificeEffectFailedNotAPawn(Guid gameId, Guid playerId, string attemptedSquare, PieceType? actualType) => _logSacrificeEffectFailedNotAPawnAction(_msLogger, gameId, playerId, attemptedSquare, actualType, null);
        public void LogSacrificeEffectFailedWrongColor(Guid gameId, Guid playerId, string attemptedSquare, Player pieceColor) => _logSacrificeEffectFailedWrongColorAction(_msLogger, gameId, playerId, attemptedSquare, pieceColor, null);
        public void LogSacrificeEffectFailedWouldCauseCheck(Guid gameId, Guid playerId, string sacrificedPawnSquare) => _logSacrificeEffectFailedWouldCauseCheckAction(_msLogger, gameId, playerId, sacrificedPawnSquare, null);
        public void LogPawnPromotionPendingAfterCard(Guid gameId, Player player, string promotionSquare, string cardTypeId) => _logPawnPromotionPendingAfterCardAction(_msLogger, gameId, player, promotionSquare, cardTypeId, null);
        public void LogPvCGameCreated(Guid gameId, string playerName, Player playerColor, string computerDifficulty) => _logPvCGameCreatedAction(_msLogger, gameId, playerName, playerColor, computerDifficulty, null);
        public void LogComputerFetchingMove(Guid gameId, string fen, int depth) => _logComputerFetchingMoveAction(_msLogger, gameId, fen, depth, null);
        public void LogComputerReceivedMove(Guid gameId, string move, string fen, int depth) => _logComputerReceivedMoveAction(_msLogger, gameId, move, fen, depth, null);
        public void LogComputerMoveError(Guid gameId, string fen, int depth, string errorMessage) => _logComputerMoveErrorAction(_msLogger, gameId, fen, depth, errorMessage, null);
        public void LogComputerMakingMove(Guid gameId, string from, string toSquare) => _logComputerMakingMoveAction(_msLogger, gameId, from, toSquare, null);
        public void LogComputerStartingInitialMove(Guid gameId, Player computerColor, Player currentPlayer) => _logComputerStartingInitialMoveAction(_msLogger, gameId, computerColor, currentPlayer, null);
        public void LogGetPlayerIdByColorFailed(Guid gameId, Player color, Guid? whiteId, Guid? blackId) => _logGetPlayerIdByColorFailedAction(_msLogger, gameId, color, whiteId, blackId, null);
        public void LogComputerTurnDelayAfterCard(Guid gameId, string cardTypeId, double delaySeconds) => _logComputerTurnDelayAfterCardAction(_msLogger, gameId, cardTypeId, delaySeconds, null);
        public void LogComputerTurnDelayCardSwap(Guid gameId, double delaySeconds) => _logComputerTurnDelayCardSwapAction(_msLogger, gameId, delaySeconds, null);
        public void LogComputerTimerPausedForAnimation(Guid gameId, Player computerColor) => _logComputerTimerPausedForAnimationAction(_msLogger, gameId, computerColor, null);
        public void LogComputerTimerResumedAfterAnimation(Guid gameId, Player computerColor) => _logComputerTimerResumedAfterAnimationAction(_msLogger, gameId, computerColor, null);
        public void LogComputerSkippingTurnAfterAnimationDelay(Guid gameId, string cardTypeId) => _logComputerSkippingTurnAfterAnimationDelayAction(_msLogger, gameId, cardTypeId, null);
        public void LogIsChessboardEnabledStatus(ChessboardEnabledStatusLogArgs args) => _logIsChessboardEnabledStatusAction(_msLogger, args, null);
        public void LogHandleHubTurnChangedClientInfo(Player nextPlayer, GameStatusDto statusForNextPlayer, string? lastMoveFromServerFrom, string? lastMoveFromServerTo, int cardEffectsCount) => _logHandleHubTurnChangedClientInfoAction(_msLogger, nextPlayer, statusForNextPlayer, lastMoveFromServerFrom, lastMoveFromServerTo, cardEffectsCount, null);
        public void LogAwaitingTurnConfirmationStatus(bool flagStatus, string context) => _logAwaitingTurnConfirmationStatusAction(_msLogger, flagStatus, context, null);
        public void LogCardsRevealed(Guid? gameId) => _logCardsRevealedAction(_msLogger, gameId, null);
        public void LogStartGameCountdown(Guid gameId) => _logStartGameCountdownAction(_msLogger, gameId, null);

        // --- ChessServer.Services.GameTimerService.cs Logs Implementations ---
        public void LogTimerStarting(Guid gameId, Player? player, TimeSpan whiteTime, TimeSpan blackTime) => _logTimerStartingAction(_msLogger, gameId, player, whiteTime, blackTime, null);
        public void LogTimerSwitching(Guid gameId, Player? player) => _logTimerSwitchingAction(_msLogger, gameId, player, null);
        public void LogTimerStoppedAndCalculated(Player? player, double elapsedSeconds, Guid gameId) => _logTimerStoppedAndCalculatedAction(_msLogger, player, elapsedSeconds, gameId, null);
        public void LogTimerTickTrace(Guid gameId, Player? player, TimeSpan whiteTime, TimeSpan blackTime) => _logTimerTickTraceAction(_msLogger, gameId, player, whiteTime, blackTime, null);
        public void LogPlayerTimeExpired(Player player, Guid gameId) => _logPlayerTimeExpiredAction(_msLogger, player, gameId, null);
        public void LogTimerDisposed(Guid gameId) => _logTimerDisposedAction(_msLogger, gameId, null);
        public void LogGameOverTimerStopped(Guid gameId) => _logGameOverTimerStoppedAction(_msLogger, gameId, null);
        public void LogTimeAdjustedTimer(TimeSpan timeAmount, Player player, Guid gameId, TimeSpan whiteTime, TimeSpan blackTime) => _logTimeAdjustedTimerAction(_msLogger, timeAmount, player, gameId, whiteTime, blackTime, null);
        public void LogTimeSwappedTimer(Player player1, Player player2, Guid gameId, TimeSpan whiteTime, TimeSpan blackTime) => _logTimeSwappedTimerAction(_msLogger, player1, player2, gameId, whiteTime, blackTime, null);
        public void LogTimeExpiredAfterManipulation(Player player, Guid gameId) => _logTimeExpiredAfterManipulationAction(_msLogger, player, gameId, null);
        public void LogTimerPaused(Guid gameId, Player? activePlayer) => _logTimerPausedAction(_msLogger, gameId, activePlayer, null);
        public void LogTimerResumed(Guid gameId, Player? activePlayer) => _logTimerResumedAction(_msLogger, gameId, activePlayer, null);

    }
}