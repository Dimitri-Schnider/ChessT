using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using ChessLogic;
using ChessNetwork.DTOs;

namespace Chess.Logging
{
    public class ChessLogger<TCategoryName> : IChessLogger
    {
        private readonly ILogger<TCategoryName> _msLogger;

        // Event-ID Zuweisungsbereiche unverändert

        // --- ChessClient.Pages.Chess.razor.cs Logs ---
        private static readonly Action<ILogger, Guid, string, string, Exception?> _logStartingGenericCardSwapAnimationAction =
            LoggerMessage.Define<Guid, string, string>(LogLevel.Information, new EventId(20001, "ClientStartingGenericCardSwapAnimationLog"), "Starte CardSwapSpecificAnimation für Spieler {PlayerId}. Gegeben: {CardGivenName}, Erhalten: {CardReceivedName}");
        private static readonly Action<ILogger, Exception?> _logCardActivationAnimationFinishedClientAction =
            LoggerMessage.Define(LogLevel.Information, new EventId(20002, "ClientCardActivationAnimationFinishedLog"), "Generische CardActivationAnimation beendet (Client).");
        private static readonly Action<ILogger, Exception?> _logSpecificCardSwapAnimationFinishedClientAction =
            LoggerMessage.Define(LogLevel.Information, new EventId(20003, "ClientSpecificCardSwapAnimationFinishedLog"), "Spezifische CardSwapSpecificAnimation beendet (Client).");
        private static readonly Action<ILogger, Player, Player, Exception?> _logUpdatePlayerNamesMismatchAction =
            LoggerMessage.Define<Player, Player>(LogLevel.Warning, new EventId(20004, "ClientUpdatePlayerNamesMismatchLog"), "[Chess.razor.cs/UpdatePlayerNames] Abgerufene Gegnerfarbe {OpponentColorRetrieved} stimmt nicht mit erwarteter Farbe {OpponentColorExpected} überein.");
        private static readonly Action<ILogger, string, Exception?> _logUpdatePlayerNamesNotFoundAction =
              LoggerMessage.Define<string>(LogLevel.Information, new EventId(20005, "ClientUpdatePlayerNamesNotFoundLog"), "[Chess.razor.cs/UpdatePlayerNames] Kein Gegner vorhanden, um Namen abzurufen (erwartet bei neuem Spiel): {ErrorMessage}");
        private static readonly Action<ILogger, Exception?> _logUpdatePlayerNamesErrorAction =
            LoggerMessage.Define(LogLevel.Error, new EventId(20006, "ClientUpdatePlayerNamesErrorLog"), "[Chess.razor.cs/UpdatePlayerNames] Fehler beim Abrufen des Gegnernamens.");
        private static readonly Action<ILogger, string, Guid, Player, Exception?> _logHandlePlayCardActivationAnimationAction =
            LoggerMessage.Define<string, Guid, Player>(LogLevel.Debug, new EventId(20007, "ClientHandlePlayCardActivationAnimationLog"), "[ChessPage] HandlePlayCardActivationAnimation: CardTypeId: {CardTypeId}, PlayerId: {PlayerId}, Color: {PlayerColor}");
        private static readonly Action<ILogger, string?, bool, Exception?> _logHandleClientAnimationFinishedTriggeredAction =
            LoggerMessage.Define<string?, bool>(LogLevel.Debug, new EventId(20008, "ClientAnimationFinishedTriggeredLog"), "[ChessPage] HandleClientAnimationFinished (Callback der generischen Animation) ausgelöst. Letzte animierte Karte war '{LastCardId}', PendingSwapDetails ist null: {IsPendingSwapNull}");
        private static readonly Action<ILogger, string, string, bool, Exception?> _logHandleReceiveCardSwapDetailsAction =
            LoggerMessage.Define<string, string, bool>(LogLevel.Debug, new EventId(20009, "ClientReceiveCardSwapDetailsLog"), "[ChessPage] HandleReceiveCardSwapAnimationDetails: Gegeben: {GivenCardName}, Erhalten: {ReceivedCardName}, Ist generische Anim. aktiv: {IsGenericAnimating}");
        private static readonly Action<ILogger, string, string, Exception?> _logActuallyStartingSpecificSwapAnimAction =
            LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(20010, "ClientActuallyStartingSpecificSwapAnimLog"), "[ChessPage] Starte CardSwapSpecificAnimation jetzt. Gegeben: {GivenCardName}, Erhalten: {ReceivedCardName}");
        private static readonly Action<ILogger, string, Exception?> _logGenericAnimationStartedForCardAction =
            LoggerMessage.Define<string>(LogLevel.Information, new EventId(20011, "ClientGenericAnimationStartedForCardLog"), "[ChessPage] Generische CardActivationAnimation gestartet für Karte: {CardName}");
        private static readonly Action<ILogger, string, Exception?> _logClientCriticalServicesNullOnInitAction =
            LoggerMessage.Define<string>(LogLevel.Warning, new EventId(20012, "ClientCriticalServicesNullLog"), "[ChessPage] Kritische Dienste sind null bei Initialisierung oder Aufruf: {ServiceName}");
        private static readonly Action<ILogger, string, Exception?> _logClientSignalRConnectionWarningAction =
            LoggerMessage.Define<string>(LogLevel.Warning, new EventId(20013, "ClientSignalRConnectionWarningLog"), "[ChessPage] SignalR Verbindungswarnung: {ErrorMessage}");

        // --- ChessServer.Controllers.GamesController.cs Logs ---
        // (Unverändert von vorheriger Version, da keine Fehler gemeldet wurden)
        private static readonly Action<ILogger, Guid, string, string, Exception?> _logMoveErrorAction =
            LoggerMessage.Define<Guid, string, string>(LogLevel.Error, new EventId(21001, "CtrlMoveProcessingErrorLog"), "Fehler beim Verarbeiten des Zugs in Spiel {GameId}: {FromSquare}->{ToSquare}");
        private static readonly Action<ILogger, Guid, string, int, Exception?> _logGameCreatedAction =
            LoggerMessage.Define<Guid, string, int>(LogLevel.Information, new EventId(21002, "CtrlGameCreatedLog"), "Spiel {GameId} erstellt für Spieler {PlayerName} mit Zeit {InitialMinutes} Minuten");
        private static readonly Action<ILogger, string, Guid, Exception?> _logPlayerJoinedGameAction =
            LoggerMessage.Define<string, Guid>(LogLevel.Information, new EventId(21003, "CtrlPlayerJoinedGameLog"), "Spieler {PlayerName} ist Spiel {GameId} beigetreten");
        private static readonly Action<ILogger, Guid, Exception?> _logGameNotFoundOnJoinAttemptAction =
            LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(21004, "CtrlGameNotFoundOnJoinAttemptLog"), "Versuch, nicht existierendem Spiel {GameId} beizutreten");
        private static readonly Action<ILogger, Guid, string, Exception?> _logInvalidOperationOnJoinGameAction =
            LoggerMessage.Define<Guid, string>(LogLevel.Warning, new EventId(21005, "CtrlInvalidOperationOnJoinGameLog"), "Ungültige Operation beim Beitritt zu Spiel {GameId}: {ErrorMessage}");
        private static readonly Action<ILogger, Guid, Guid, bool, Exception?> _logApplyMoveInfoAction =
            LoggerMessage.Define<Guid, Guid, bool>(LogLevel.Information, new EventId(21006, "CtrlApplyMoveInfoLog"), "[GamesController] ApplyMove für Spiel {GameId} von Spieler {PlayerId} war IsValid={IsValid}");
        private static readonly Action<ILogger, Guid, Player, GameStatusDto, string?, string?, string?, Exception?> _logSignalRUpdateInfoAction =
            LoggerMessage.Define<Guid, Player, GameStatusDto, string?, string?, string?>(LogLevel.Information, new EventId(21007, "CtrlSignalRUpdateInfoLog"), "[GamesController] Sende SignalR Update für Spiel {GameId} via Hub. Nächster Spieler: {NextPlayerTurn}, Status für ihn: {StatusForNextPlayer}, LastMoveFrom: {LastMoveFrom}, LastMoveTo: {LastMoveTo}, CardEffectSquareCount: {CardEffectSquareCount}");
        private static readonly Action<ILogger, Guid, Exception?> _logOnTurnChangedSentToHubAction =
            LoggerMessage.Define<Guid>(LogLevel.Information, new EventId(21008, "CtrlOnTurnChangedSentToHubLog"), "[GamesController] OnTurnChanged für Spiel {GameId} gesendet.");
        private static readonly Action<ILogger, Guid, Exception?> _logOnTimeUpdateSentAfterMoveAction =
            LoggerMessage.Define<Guid>(LogLevel.Information, new EventId(21009, "CtrlOnTimeUpdateSentAfterMoveLog"), "[GamesController] OnTimeUpdate für Spiel {GameId} nach Zug gesendet.");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logGameNotFoundOnMoveAction =
            LoggerMessage.Define<Guid, Guid>(LogLevel.Warning, new EventId(21010, "CtrlGameNotFoundOnMoveLog"), "[GamesController] Spiel {GameId} nicht gefunden bei Zug von Spieler {PlayerId}");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logInvalidOperationOnMoveAction =
            LoggerMessage.Define<Guid, Guid>(LogLevel.Warning, new EventId(21011, "CtrlInvalidOperationOnMoveLog"), "[GamesController] Ungültige Operation bei Zug in Spiel {GameId} von Spieler {PlayerId}");
        private static readonly Action<ILogger, Guid, Exception?> _logGameNotFoundOnTimeRequestAction =
            LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(21012, "CtrlGameNotFoundOnTimeRequestLog"), "Spiel {GameId} nicht gefunden beim Abrufen der Zeit.");
        private static readonly Action<ILogger, Guid, Exception?> _logErrorGettingTimeAction =
            LoggerMessage.Define<Guid>(LogLevel.Error, new EventId(21013, "CtrlErrorGettingTimeLog"), "Fehler beim Abrufen der Zeit für Spiel {GameId}");
        private static readonly Action<ILogger, Guid, Exception?> _logGameHistoryNullFromManagerAction =
            LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(21014, "CtrlGameHistoryNullFromManagerLog"), "Spielverlauf für Spiel {GameId} nicht gefunden (Manager gab null zurück).");
        private static readonly Action<ILogger, Guid, Exception?> _logGameHistoryKeyNotFoundAction =
            LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(21015, "CtrlGameHistoryKeyNotFoundLog"), "Spiel {GameId} nicht gefunden beim Anfordern des Spielverlaufs.");
        private static readonly Action<ILogger, Guid, Exception?> _logGameHistoryGenericErrorAction =
            LoggerMessage.Define<Guid>(LogLevel.Error, new EventId(21016, "CtrlGameHistoryGenericErrorLog"), "Fehler beim Abrufen des Spielverlaufs für Spiel {GameId}");
        private static readonly Action<ILogger, Guid, Guid, string, Exception?> _logCardActivationAttemptAction =
            LoggerMessage.Define<Guid, Guid, string>(LogLevel.Information, new EventId(21017, "CtrlCardActivationAttemptLog"), "[GamesController] Kartenaktivierungsversuch für Spiel {GameId}, Spieler {PlayerId}, Karte {CardTypeId}");
        private static readonly Action<ILogger, Guid, Guid, string, string, Exception?> _logCardActivationFailedControllerAction =
            LoggerMessage.Define<Guid, Guid, string, string>(LogLevel.Warning, new EventId(21018, "CtrlCardActivationFailedLog"), "[GamesController] Kartenaktivierung FEHLGESCHLAGEN für Spiel {GameId}, Spieler {PlayerId}, Karte {CardTypeId}. Grund: {Reason}");
        private static readonly Action<ILogger, Guid, Guid, string, Exception?> _logCardActivationSuccessControllerAction =
            LoggerMessage.Define<Guid, Guid, string>(LogLevel.Information, new EventId(21019, "CtrlCardActivationSuccessLog"), "[GamesController] Kartenaktivierung ERFOLGREICH für Spiel {GameId}, Spieler {PlayerId}, Karte {CardTypeId}");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logSignalingPlayerToDrawCardAction =
            LoggerMessage.Define<Guid, Guid>(LogLevel.Information, new EventId(21020, "CtrlSignalingPlayerToDrawCardLog"), "[GamesController] Signalisiere Spieler {PlayerIdToDraw} in Spiel {GameId}, eine Karte zu ziehen.");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logGettingCapturedPiecesAction =
            LoggerMessage.Define<Guid, Guid>(LogLevel.Debug, new EventId(21021, "CtrlGettingCapturedPiecesLog"), "[GamesController] Rufe geschlagene Figuren für Spieler {PlayerId} in Spiel {GameId} ab.");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logGameNotFoundCapturedPiecesAction =
            LoggerMessage.Define<Guid, Guid>(LogLevel.Warning, new EventId(21022, "CtrlGameNotFoundCapturedPiecesLog"), "[GamesController] Spiel {GameId} nicht gefunden beim Abrufen der geschlagenen Figuren für Spieler {PlayerId}.");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logErrorGettingCapturedPiecesAction =
            LoggerMessage.Define<Guid, Guid>(LogLevel.Error, new EventId(21023, "CtrlErrorGettingCapturedPiecesLog"), "[GamesController] Allgemeiner Fehler beim Abrufen der geschlagenen Figuren für Spiel {GameId}, Spieler {PlayerId}.");
        private static readonly Action<ILogger, Guid, string, Exception?> _logControllerGameNotFoundAction =
            LoggerMessage.Define<Guid, string>(LogLevel.Warning, new EventId(21024, "CtrlGameNotFoundForActionLog"), "[GamesController] Spiel {GameId} für Aktion {ActionName} nicht gefunden.");
        private static readonly Action<ILogger, string, string, Guid, Exception?> _logCtrlMoveSentCardToHandAction =
            LoggerMessage.Define<string, string, Guid>(LogLevel.Information, new EventId(21025, "CtrlMoveSentCardToHandLog"), "[GamesController] Move: Sent CardAddedToHand (Card: {CardName}) to Connection {ConnectionId} for player {PlayerId}");
        private static readonly Action<ILogger, string, Guid, Exception?> _logCtrlConnIdNotFoundNoMoreCardsAction =
            LoggerMessage.Define<string, Guid>(LogLevel.Warning, new EventId(21026, "CtrlConnIdNotFoundNoMoreCardsLog"), "[GamesController] {ActionSource}: ConnectionId for player {PlayerId} not found to send NoMoreCards info.");
        private static readonly Action<ILogger, string, Guid, Exception?> _logCtrlConnIdNotFoundGenericAction =
            LoggerMessage.Define<string, Guid>(LogLevel.Warning, new EventId(21027, "CtrlConnIdNotFoundGenericLog"), "[GamesController] {ActionSource}: ConnectionId for player {PlayerId} not found. Sent generic OnPlayerEarnedCardDraw.");
        private static readonly Action<ILogger, string, string, Guid, Exception?> _logCtrlActivateCardSentCardToHandAction =
            LoggerMessage.Define<string, string, Guid>(LogLevel.Information, new EventId(21028, "CtrlActivateCardSentCardToHandLog"), "[GamesController] ActivateCard: Sent CardAddedToHand (Card: {CardName}) to Connection {ConnectionId} for player {PlayerId}");
        private static readonly Action<ILogger, Guid, Player, Exception?> _logControllerCouldNotDeterminePlayerIdForStatusAction =
            LoggerMessage.Define<Guid, Player>(LogLevel.Warning, new EventId(21029, "CtrlCouldNotDeterminePlayerIdForStatusLog"), "[GamesController] Konnte Spieler-ID für {PlayerColor} in Spiel {GameId} nicht ermitteln, um Status zu senden.");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logControllerErrorGettingOpponentInfoAction =
            LoggerMessage.Define<Guid, Guid>(LogLevel.Error, new EventId(21030, "CtrlErrorGettingOpponentInfoLog"), "Fehler beim Abrufen der Gegnerinformationen für Spiel {GameId}, Spieler {PlayerId}");
        private static readonly Action<ILogger, Guid, Guid, string, Exception?> _logControllerErrorGettingLegalMovesAction =
            LoggerMessage.Define<Guid, Guid, string>(LogLevel.Warning, new EventId(21031, "CtrlErrorGettingLegalMovesLog"), "Fehler bei LegalMoves für Spiel {GameId}, Spieler {PlayerId}, Von {FromSquare}");
        private static readonly Action<ILogger, Guid, Exception?> _logCtrlConnIdForPlayerNotFoundAction =
            LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(21032, "CtrlConnIdForPlayerNotFoundLog"), "[GamesController] GetConnectionIdForPlayerViaHubMap: Konnte ConnectionId für PlayerId {PlayerId} nicht finden.");

        // --- ChessServer.Hubs.ChessHub.cs Logs ---
        // (Unverändert von vorheriger Version)
        private static readonly Action<ILogger, string, Exception?> _logHubClientConnectedAction =
            LoggerMessage.Define<string>(LogLevel.Information, new EventId(22001, "HubClientConnectedLog"), "SignalR Hub: Client verbunden: {ConnectionId}");
        private static readonly Action<ILogger, string, string?, Exception?> _logHubClientDisconnectedAction =
            LoggerMessage.Define<string, string?>(LogLevel.Information, new EventId(22002, "HubClientDisconnectedLog"), "SignalR Hub: Client getrennt: {ConnectionId}, Fehler: {ErrorMessage}");
        private static readonly Action<ILogger, string, string, Exception?> _logHubClientJoiningGameGroupAction =
            LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(22003, "HubClientJoiningGameGroupLog"), "SignalR Hub: Client {ConnectionId} versucht Spielgruppe {GameIdString} beizutreten.");
        private static readonly Action<ILogger, string, string, Exception?> _logHubClientAddedToGameGroupAction =
            LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(22004, "HubClientAddedToGameGroupLog"), "SignalR Hub: Client {ConnectionId} zu Spielgruppe {GameIdString} hinzugefügt.");
        private static readonly Action<ILogger, string, string, int, Exception?> _logHubPlayerJoinedNotificationSentAction =
            LoggerMessage.Define<string, string, int>(LogLevel.Information, new EventId(22005, "HubPlayerJoinedNotificationSentLog"), "SignalR Hub: PlayerJoined-Nachricht für {JoiningPlayerName} in Spiel {GameIdString} gesendet. Spieleranzahl: {PlayerCount}");
        private static readonly Action<ILogger, string, string, Exception?> _logHubClientLeavingGameGroupAction =
            LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(22006, "HubClientLeavingGameGroupLog"), "SignalR Hub: Client {ConnectionId} versucht Spielgruppe {GameIdString} zu verlassen.");
        private static readonly Action<ILogger, string, string, Exception?> _logHubClientRemovedFromGameGroupAction =
            LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(22007, "HubClientRemovedFromGameGroupLog"), "SignalR Hub: Client {ConnectionId} aus Spielgruppe {GameIdString} entfernt.");
        private static readonly Action<ILogger, Guid, string, Exception?> _logHubGameNotFoundForPlayerCountAction =
            LoggerMessage.Define<Guid, string>(LogLevel.Warning, new EventId(22008, "HubGameNotFoundForPlayerCountLog"), "Spiel {GameIdGuid} nicht im GameManager gefunden, um Spielerzahl zu bestimmen, während Client {ConnectionId} beitritt.");
        private static readonly Action<ILogger, string, string, Exception?> _logHubJoinGameInvalidGameIdFormatAction =
            LoggerMessage.Define<string, string>(LogLevel.Warning, new EventId(22009, "HubJoinGameInvalidGameIdFormatLog"), "SignalR Hub: JoinGame mit ungültigem Game-ID Format: {GameIdString} von Client {ConnectionId}");
        private static readonly Action<ILogger, Guid, string, Guid, Exception?> _logHubPlayerRegisteredToHubAction =
            LoggerMessage.Define<Guid, string, Guid>(LogLevel.Information, new EventId(22010, "HubPlayerRegisteredToHubLog"), "SignalR Hub: Spieler {PlayerId} (Connection: {ConnectionId}) für Spiel {GameId} registriert.");
        private static readonly Action<ILogger, string, Exception?> _logHubPlayerDeregisteredFromHubAction =
            LoggerMessage.Define<string>(LogLevel.Information, new EventId(22011, "HubPlayerDeregisteredFromHubLog"), "SignalR Hub: Verbindung {ConnectionId} und zugehöriger Spieler deregistriert.");
        private static readonly Action<ILogger, Guid, string, Guid, int, int, Exception?> _logHubSendingInitialHandAction =
            LoggerMessage.Define<Guid, string, Guid, int, int>(LogLevel.Information, new EventId(22012, "HubSendingInitialHandLog"), "[ChessHub] Sende Starthand an Spieler {PlayerId} (Connection: {ConnectionId}) für Spiel {GameId}. Handgrösse: {HandSize}, Nachziehstapel: {DrawPileCount}");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logHubFailedToSendInitialHandSessionNotFoundAction =
            LoggerMessage.Define<Guid, Guid>(LogLevel.Warning, new EventId(22013, "HubFailedToSendInitialHandSessionNotFoundLog"), "[ChessHub] Konnte GameSession für Spiel {GameId} nicht abrufen, um Starthand an Spieler {PlayerId} zu senden.");
        private static readonly Action<ILogger, string, string, Exception?> _logHubPlayerActuallyJoinedGameAction =
            LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(22014, "HubPlayerActuallyJoinedGameLog"), "SignalR Hub: Spieler {PlayerName} ist Spiel {GameIdString} tatsächlich beigetreten und wird nun der Gruppe hinzugefügt.");
        private static readonly Action<ILogger, Guid, Exception?> _logHubPlayerMappingRemovedOnDisconnectAction =
            LoggerMessage.Define<Guid>(LogLevel.Information, new EventId(22015, "HubPlayerMappingRemovedOnDisconnectLog"), "SignalR Hub: Mapping für PlayerId {PlayerId} bei Disconnect entfernt.");
        private static readonly Action<ILogger, string, Guid, Exception?> _logHubConnectionRemovedFromGameOnDisconnectAction =
              LoggerMessage.Define<string, Guid>(LogLevel.Information, new EventId(22016, "HubConnectionRemovedFromGameOnDisconnectLog"), "SignalR Hub: Connection {ConnectionId} aus Spiel {GameId} bei Disconnect entfernt.");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logHubErrorSendingInitialHandAction =
            LoggerMessage.Define<Guid, Guid>(LogLevel.Error, new EventId(22017, "HubErrorSendingInitialHandLog"), "[ChessHub] Fehler beim Senden der Starthand an Spieler {PlayerId} in Spiel {GameId}.");


        // --- ChessServer.Services.GameSession.cs Logs ---
        private static readonly Action<ILogger, Guid, Exception?> _logSessionErrorGetNameByColorAction =
            LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(23001, "SessionErrorGetNameByColorLog"), "[GameSession] Fehler in MakeMove (GetPlayerColor) für Spiel {GameId}");
        private static readonly Action<ILogger, Guid, TimeSpan, TimeSpan, Player?, Exception?> _logSessionSendTimeUpdateAction =
            LoggerMessage.Define<Guid, TimeSpan, TimeSpan, Player?>(LogLevel.Debug, new EventId(23002, "SessionSendTimeUpdateLog"), "[GameSession] SendTimeUpdate für Spiel {GameId}: W:{WhiteTime} B:{BlackTime} Active:{ActivePlayer}");
        private static readonly Action<ILogger, Guid, Exception?> _logSessionErrorIsPlayerTurnAction =
            LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(23003, "SessionErrorIsPlayerTurnLog"), "[GameSession] Fehler in IsPlayerTurn bei GetPlayerColor für Spiel {GameId}");
        private static readonly Action<ILogger, Guid, Guid, int, Exception?> _logSessionColorNotDeterminedAction =
            LoggerMessage.Define<Guid, Guid, int>(LogLevel.Warning, new EventId(23004, "SessionColorNotDeterminedLog"), "[GameSession] Farbe für Spieler {PlayerId} in Spiel {GameId} konnte nicht eindeutig bestimmt werden. Spieleranzahl: {PlayerCount}");
        private static readonly Action<ILogger, Guid, Player, Exception?> _logGameEndedByTimeoutInSessionAction =
            LoggerMessage.Define<Guid, Player>(LogLevel.Information, new EventId(23005, "GameEndedByTimeoutInSessionLog"), "[GameSession] Spiel {GameId} durch Timeout von Spieler {ExpiredPlayer} beendet.");
        private static readonly Action<ILogger, Guid, Guid, string, Exception?> _logSessionCardActivationAttemptAction =
            LoggerMessage.Define<Guid, Guid, string>(LogLevel.Information, new EventId(23006, "SessionCardActivationAttemptLog"), "[GameSession] Kartenaktivierungsversuch: Spiel {GameId}, Spieler {PlayerId}, Karte {CardId}");
        private static readonly Action<ILogger, Guid, Guid, string, string, Exception?> _logSessionCardActivationFailedAction =
            LoggerMessage.Define<Guid, Guid, string, string>(LogLevel.Warning, new EventId(23007, "SessionCardActivationFailedLog"), "[GameSession] Kartenaktivierung FEHLGESCHLAGEN: Spiel {GameId}, Spieler {PlayerId}, Karte {CardId}. Grund: {Reason}");
        private static readonly Action<ILogger, Guid, Guid, string, Exception?> _logSessionCardActivationSuccessAction =
            LoggerMessage.Define<Guid, Guid, string>(LogLevel.Information, new EventId(23008, "SessionCardActivationSuccessLog"), "[GameSession] Kartenaktivierung ERFOLGREICH: Spiel {GameId}, Spieler {PlayerId}, Karte {CardId}");
        private static readonly Action<ILogger, Guid, Guid, Player, string, Exception?> _logPlayerAttemptingCardWhileInCheckAction =
             LoggerMessage.Define<Guid, Guid, Player, string>(LogLevel.Information, new EventId(23028, "PlayerAttemptingCardWhileInCheckLog"), "[GameSession] Spiel {GameId}: Spieler {PlayerId} (Farbe: {PlayerColor}) versucht Karte {CardTypeId} im Schach zu spielen. Prüfung nach Effekt.");
        private static readonly Action<ILogger, Guid, Guid, string, Exception?> _logPlayerStillInCheckAfterCardTurnNotEndedAction =
            LoggerMessage.Define<Guid, Guid, string>(LogLevel.Information, new EventId(23029, "PlayerStillInCheckAfterCardTurnNotEndedLog"), "[GameSession] Spiel {GameId}: Spieler {PlayerId} ist nach Kartenaktivierung von '{CardTypeId}' weiterhin im Schach. Zug wird nicht an Gegner übergeben.");
        private static readonly Action<ILogger, Guid, Guid, Player, string, string, Exception?> _logPlayerInCheckTriedInvalidMoveAction =
            LoggerMessage.Define<Guid, Guid, Player, string, string>(LogLevel.Warning, new EventId(23030, "PlayerInCheckTriedInvalidMoveLog"), "[GameSession] Spiel {GameId}: Spieler {PlayerId} (Farbe: {PlayerColor}) im Schach versuchte ungültigen Zug {From}->{To}.");
        private static readonly Action<ILogger, Guid, Guid, Player, string, string, Exception?> _logPlayerTriedMoveThatDidNotResolveCheckAction =
            LoggerMessage.Define<Guid, Guid, Player, string, string>(LogLevel.Warning, new EventId(23031, "PlayerTriedMoveThatDidNotResolveCheckLog"), "[GameSession] Spiel {GameId}: Spieler {PlayerId} (Farbe: {PlayerColor}) wählte Zug {From}->{To}, der das Schach nicht pariert.");
        private static readonly Action<ILogger, Guid, Player, string, string, Exception?> _logPawnPromotionPendingAfterCardAction =
            LoggerMessage.Define<Guid, Player, string, string>(LogLevel.Information, new EventId(23033, "LogPawnPromotionPendingAfterCard"), "[GameSession] Spiel {GameId}: Bauernumwandlung für Spieler {PlayerColor} auf Feld {PromotionSquare} nach Karteneffekt {CardTypeId} anstehend. Zug endet nicht.");


        // Umbenannte Methoden (ehemals RecordEvent...)
        private static readonly Action<ILogger, Guid, Guid, string, Exception?> _logExtraTurnEffectAppliedAction = // Name des Action-Delegaten ist identisch geblieben, EventID und Text angepasst
            LoggerMessage.Define<Guid, Guid, string>(LogLevel.Information, new EventId(23009, "LogExtraTurnEffectAppliedLog"), "[GameSession] Extrazug-Effekt angewendet für Spieler {PlayerId} in Spiel {GameId} nach Karte {CardId}.");
        private static readonly Action<ILogger, Guid, Guid, int, Exception?> _logPlayerMoveCountIncreasedAction = // Name des Action-Delegaten ist identisch geblieben, EventID und Text angepasst
              LoggerMessage.Define<Guid, Guid, int>(LogLevel.Debug, new EventId(23010, "LogPlayerMoveCountIncreasedLog"), "[GameSession] Zugzähler für Spieler {PlayerId} in Spiel {GameId} erhöht auf {MoveCount}.");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logPlayerCardDrawIndicatedAction = // Name des Action-Delegaten ist identisch geblieben, EventID und Text angepasst
            LoggerMessage.Define<Guid, Guid>(LogLevel.Information, new EventId(23011, "LogPlayerCardDrawIndicatedLog"), "[GameSession] Spieler {PlayerId} in Spiel {GameId} soll eine Karte ziehen.");

        // Weitere GameSession Logs
        private static readonly Action<ILogger, Guid, Guid, string, string, Exception?> _logExtraTurnFirstMoveCausesCheckAction = 
            LoggerMessage.Define<Guid, Guid, string, string>(LogLevel.Warning, new EventId(23032, "ExtraTurnFirstMoveCausesCheckLog"), "[GameSession] Spiel {GameId}: Spieler {PlayerId} versuchte mit erstem Zug des Extrazugs ({FromSquare}->{ToSquare}) den Gegner Schach zu setzen. Ungültig.");
        private static readonly Action<ILogger, Guid, Guid, string, Exception?> _logNotifyingOpponentOfCardPlayAction =
            LoggerMessage.Define<Guid, Guid, string>(LogLevel.Information, new EventId(23012, "NotifyingOpponentOfCardPlayLog"), "[GameSession] Benachrichtige alle in Spiel {GameId} über Kartenspiel von Spieler {PlayerId}, Karte {CardId}.");
        private static readonly Action<ILogger, Guid, PieceType, Player, Exception?> _logCapturedPieceAddedAction =
            LoggerMessage.Define<Guid, PieceType, Player>(LogLevel.Debug, new EventId(23013, "CapturedPieceAddedLog"), "[GameSession] {GameId}: Figur {PieceType} von Spieler {PlayerColor} zur Liste der geschlagenen Figuren hinzugefügt.");
        private static readonly Action<ILogger, Guid, string, string, PieceType?, Exception?> _logPawnPromotionMoveSelectionAction =
            LoggerMessage.Define<Guid, string, string, PieceType?>(LogLevel.Debug, new EventId(23014, "PawnPromotionMoveSelectionLog"), "[GameSession] MakeMove: Suche nach Bauernumwandlung für Spiel {GameId} von {From} nach {To} zu {PromotionType}.");
        private static readonly Action<ILogger, Guid, string, string, PieceType, Exception?> _logPawnPromotionMoveFoundAction =
              LoggerMessage.Define<Guid, string, string, PieceType>(LogLevel.Information, new EventId(23015, "PawnPromotionMoveFoundLog"), "[GameSession] MakeMove: Bauernumwandlung für Spiel {GameId} von {From} nach {To} zu {PromotionType} GEFUNDEN und ausgewählt.");
        private static readonly Action<ILogger, Guid, string, string, PieceType?, Exception?> _logPawnPromotionMoveNotFoundAction =
            LoggerMessage.Define<Guid, string, string, PieceType?>(LogLevel.Warning, new EventId(23016, "PawnPromotionMoveNotFoundLog"), "[GameSession] MakeMove: Bauernumwandlung für Spiel {GameId} von {From} nach {To} zu {PromotionType} NICHT gefunden unter legalen Zügen.");
        private static readonly Action<ILogger, Guid, string?, string?, string, Exception?> _logOnTurnChangedFromSessionAction =
           LoggerMessage.Define<Guid, string?, string?, string>(LogLevel.Debug, new EventId(23017, "OnTurnChangedFromSessionLog"), "[GameSession] Sende OnTurnChanged für Spiel {GameId}. LastMoveFrom: {LastMoveFrom}, LastMoveTo: {LastMoveTo}, CardEffectType: {CardEffectType}");
        private static readonly Action<ILogger, Guid, Guid, int, Exception?> _logPlayerDeckInitializedAction =
            LoggerMessage.Define<Guid, Guid, int>(LogLevel.Information, new EventId(23018, "PlayerDeckInitializedLog"), "[GameSession] Deck für Spieler {PlayerId} in Spiel {GameId} initialisiert und gemischt. {DrawPileCount} Karten im Stapel.");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logDrawAttemptUnknownPlayerAction =
            LoggerMessage.Define<Guid, Guid>(LogLevel.Warning, new EventId(23019, "DrawAttemptUnknownPlayerLog"), "[GameSession] Versuch, Karte für unbekannten Spieler {PlayerId} in Spiel {GameId} zu ziehen.");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logNoDrawPileForPlayerAction =
            LoggerMessage.Define<Guid, Guid>(LogLevel.Error, new EventId(23020, "NoDrawPileForPlayerLog"), "[GameSession] Kein Nachziehstapel für Spieler {PlayerId} in Spiel {GameId} gefunden!");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logPlayerDrawPileEmptyAction =
            LoggerMessage.Define<Guid, Guid>(LogLevel.Information, new EventId(23021, "PlayerDrawPileEmptyLog"), "[GameSession] Nachziehstapel für Spieler {PlayerId} in Spiel {GameId} ist leer.");
        private static readonly Action<ILogger, Guid, string, string, Guid, int, Exception?> _logPlayerDrewCardFromOwnDeckAction =
            LoggerMessage.Define<Guid, string, string, Guid, int>(LogLevel.Information, new EventId(23022, "PlayerDrewCardFromOwnDeckLog"), "[GameSession] Spieler {PlayerId} hat Karte '{CardName}' ({CardId}) aus seinem Deck in Spiel {GameId} gezogen. {RemainingInPlayerDrawPile} Karten in seinem Stapel verbleibend.");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logCannotFindPlayerDrawPileForCountAction =
            LoggerMessage.Define<Guid, Guid>(LogLevel.Warning, new EventId(23023, "CannotFindPlayerDrawPileForCountLog"), "[GameSession] Konnte Nachziehstapel für Spieler {PlayerId} in Spiel {GameId} nicht finden, um Anzahl zu ermitteln.");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logCurrentPlayerNotFoundForOpponentDetailsAction =
            LoggerMessage.Define<Guid, Guid>(LogLevel.Warning, new EventId(23024, "CurrentPlayerNotFoundForOpponentDetailsLog"), "[GameSession] GetOpponentDetails: Aktueller Spieler {CurrentPlayerId} nicht in Spiel {GameId} gefunden.");
        private static readonly Action<ILogger, Guid, Player, Guid, Exception?> _logNoOpponentFoundForPlayerAction =
            LoggerMessage.Define<Guid, Player, Guid>(LogLevel.Information, new EventId(23025, "NoOpponentFoundForPlayerLog"), "[GameSession] GetOpponentDetails: Kein Gegner für Spieler {CurrentPlayerId} (Farbe: {CurrentPlayerColor}) in Spiel {GameId} gefunden.");

        // Korrigierte Parameter für CA1725 in den Action-Definitionen, um Konsistenz mit Interface und Methoden zu gewährleisten:
        // Die ursprünglichen LoggerMessage.Define Aufrufe für 23026 und 23027 hatten gameId als string.
        // Wenn gameId durchgehend Guid sein soll, müsste dies hier auch geändert werden.
        // Für die Korrektur von CA1725 ist der Parametername in der öffentlichen Methode entscheidend.
        // Die Action-Delegaten können intern andere Namen haben, aber die öffentliche Schnittstelle muss stimmen.
        private static readonly Action<ILogger, Guid, Guid, string, string, Exception?> _logCardInstancePlayedAction =
            LoggerMessage.Define<Guid, Guid, string, string>(LogLevel.Debug, new EventId(23026, "CardInstancePlayedLog"), "[GameSession] Karte mit Instanz-ID {CardInstanceId} (Typ: {CardTypeId}) von Spieler {PlayerId} in Spiel {GameId} gespielt.");
        private static readonly Action<ILogger, Guid, Guid, string, Exception?> _logCardInstanceNotFoundInHandAction =
            LoggerMessage.Define<Guid, Guid, string>(LogLevel.Warning, new EventId(23027, "CardInstanceNotFoundInHandLog"), "[GameSession] Zu spielende Karteninstanz {CardInstanceId} nicht in Hand von Spieler {PlayerId} in Spiel {GameId} gefunden.");

        // --- ChessServer.Services.CardEffects Logs ---
        // (Unverändert von vorheriger Version)
        private static readonly Action<ILogger, Player, Guid, Guid, Exception?> _logAddTimeEffectAppliedAction =
            LoggerMessage.Define<Player, Guid, Guid>(LogLevel.Information, new EventId(24001, "AddTimeEffectAppliedLog"), "[AddTimeEffect] Zeitgutschrift (+2 Min) für Spieler {PlayerColor} ({PlayerId}) in Spiel {GameId} angewendet.");
        private static readonly Action<ILogger, Guid, Guid, Guid, Guid, Exception?> _logCardSwapEffectExecutedGuidAction =
            LoggerMessage.Define<Guid, Guid, Guid, Guid>(LogLevel.Information, new EventId(24002, "CardSwapEffectExecutedLog"), "[CardSwapEffect] Spieler {PlayerId} tauscht Karte mit Instanz-ID {SwappedOutPlayerCardInstanceId} gegen Gegnerkarte mit Instanz-ID {SwappedInOpponentCardInstanceId} in Spiel {GameId}.");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logCardSwapEffectOpponentNoCardsAction =
            LoggerMessage.Define<Guid, Guid>(LogLevel.Warning, new EventId(24003, "CardSwapEffectOpponentNoCardsLog"), "[CardSwapEffect] Spieler {PlayerId} versuchte Kartentausch in Spiel {GameId}, aber Gegner hat keine Handkarten.");
        private static readonly Action<ILogger, Guid, Guid, Guid, Exception?> _logCardSwapEffectPlayerCardInstanceNotFoundAction =
            LoggerMessage.Define<Guid, Guid, Guid>(LogLevel.Warning, new EventId(24004, "CardSwapEffectPlayerCardInstanceNotFoundLog"), "[CardSwapEffect] Spieler {PlayerId} versuchte Karte mit Instanz-ID {MissingCardInstanceId} zu tauschen (nicht in Hand) in Spiel {GameId}.");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logExtraZugEffectAppliedAction = // Für den CardEffect
            LoggerMessage.Define<Guid, Guid>(LogLevel.Information, new EventId(24005, "ExtraZugCardEffectAppliedLog"), "[ExtraZugEffect] Extrazug für Spieler {PlayerId} in Spiel {GameId} vermerkt.");
        private static readonly Action<ILogger, string?, string?, Guid, Guid, Exception?> _logPositionSwapEffectExecutedAction =
            LoggerMessage.Define<string?, string?, Guid, Guid>(LogLevel.Information, new EventId(24006, "PositionSwapEffectExecutedLog"), "[PositionSwapEffect] Positionstausch zwischen {FromSquare} und {ToSquare} für Spieler {PlayerId} in Spiel {GameId} ausgeführt.");
        private static readonly Action<ILogger, PieceType, string?, Player, Guid, Guid, Exception?> _logRebirthEffectExecutedAction =
            LoggerMessage.Define<PieceType, string?, Player, Guid, Guid>(LogLevel.Information, new EventId(24007, "RebirthEffectExecutedLog"), "[RebirthEffect] Figur {PieceType} auf Feld {TargetSquare} für Spieler {PlayerColor} ({PlayerId}) in Spiel {GameId} wiederbelebt.");
        private static readonly Action<ILogger, string, string?, string?, Guid, Exception?> _logRebirthEffectFailedStringAction =
            LoggerMessage.Define<string, string?, string?, Guid>(LogLevel.Warning, new EventId(24008, "RebirthEffectFailedStringLog"), "[RebirthEffect] Wiederbelebung von Figurentyp-String '{PieceTypeString}' auf {TargetSquare} in Spiel {GameId} fehlgeschlagen: {Reason}");
        private static readonly Action<ILogger, string, PieceType, string?, Guid, Exception?> _logRebirthEffectFailedEnumAction =
            LoggerMessage.Define<string, PieceType, string?, Guid>(LogLevel.Warning, new EventId(24009, "RebirthEffectFailedEnumLog"), "[RebirthEffect] Wiederbelebung von Figurentyp {PieceTypeEnum} auf {TargetSquare} in Spiel {GameId} fehlgeschlagen: {Reason}");
        private static readonly Action<ILogger, Player, Player, Guid, Guid, Exception?> _logSubtractTimeEffectAppliedAction =
            LoggerMessage.Define<Player, Player, Guid, Guid>(LogLevel.Information, new EventId(24010, "SubtractTimeEffectAppliedLog"), "[SubtractTimeEffect] Zeitdiebstahl (-2 Min) von Gegner {OpponentColor} durch Spieler {PlayerColor} ({PlayerId}) in Spiel {GameId} angewendet.");
        private static readonly Action<ILogger, string?, string?, Guid, Guid, Exception?> _logTeleportEffectExecutedAction =
            LoggerMessage.Define<string?, string?, Guid, Guid>(LogLevel.Information, new EventId(24011, "TeleportEffectExecutedLog"), "[TeleportEffect] Teleport von {FromSquare} nach {ToSquare} für Spieler {PlayerId} in Spiel {GameId} ausgeführt.");
        private static readonly Action<ILogger, Player, Player, Guid, Exception?> _logTimeSwapEffectAppliedAction =
            LoggerMessage.Define<Player, Player, Guid>(LogLevel.Information, new EventId(24012, "TimeSwapEffectAppliedLog"), "[TimeSwapEffect] Zeiten getauscht zwischen Spieler {Player1Color} und {Player2Color} in Spiel {GameId}.");
        private static readonly Action<ILogger, Guid, Guid, string, Exception?> _logSacrificeEffectExecutedAction =
            LoggerMessage.Define<Guid, Guid, string>(LogLevel.Information, new EventId(24013, "SacrificeEffectExecutedLog"), "[SacrificeEffect] Spiel {GameId}: Spieler {PlayerId} opfert Bauer auf {SacrificedPawnSquare}.");
        private static readonly Action<ILogger, Guid, Guid, string, Exception?> _logSacrificeEffectFailedPawnNotFoundAction =
            LoggerMessage.Define<Guid, Guid, string>(LogLevel.Warning, new EventId(24014, "SacrificeEffectFailedPawnNotFoundLog"), "[SacrificeEffect] Spiel {GameId}: Opfergabe für Spieler {PlayerId} fehlgeschlagen: Kein Bauer auf Feld {AttemptedSquare} gefunden.");
        private static readonly Action<ILogger, Guid, Guid, string, PieceType?, Exception?> _logSacrificeEffectFailedNotAPawnAction =
            LoggerMessage.Define<Guid, Guid, string, PieceType?>(LogLevel.Warning, new EventId(24015, "SacrificeEffectFailedNotAPawnLog"), "[SacrificeEffect] Spiel {GameId}: Opfergabe für Spieler {PlayerId} fehlgeschlagen: Figur auf {AttemptedSquare} ist kein Bauer ({ActualType}).");
        private static readonly Action<ILogger, Guid, Guid, string, Player, Exception?> _logSacrificeEffectFailedWrongColorAction =
            LoggerMessage.Define<Guid, Guid, string, Player>(LogLevel.Warning, new EventId(24016, "SacrificeEffectFailedWrongColorLog"), "[SacrificeEffect] Spiel {GameId}: Opfergabe für Spieler {PlayerId} fehlgeschlagen: Bauer auf {AttemptedSquare} gehört nicht diesem Spieler (Farbe: {PieceColor}).");
        private static readonly Action<ILogger, Guid, Guid, string, Exception?> _logSacrificeEffectFailedWouldCauseCheckAction =
            LoggerMessage.Define<Guid, Guid, string>(LogLevel.Warning, new EventId(24017, "SacrificeEffectFailedWouldCauseCheckLog"), "[SacrificeEffect] Spiel {GameId}: Opfergabe von Bauer auf {SacrificedPawnSquare} für Spieler {PlayerId} nicht möglich, da eigener König dadurch ins Schach geraten würde.");

        // --- ChessServer.Services.InMemoryGameManager.cs Logs ---
        // (Unverändert von vorheriger Version)
        private static readonly Action<ILogger, Guid, string, Guid, Player, int, Exception?> _logMgrGameCreatedAction =
            LoggerMessage.Define<Guid, string, Guid, Player, int>(LogLevel.Information, new EventId(25001, "MgrGameCreatedLog"), "Spiel {GameId} erstellt von Spieler {PlayerName} ({PlayerId}) mit Farbe {Color} und Startzeit {InitialMinutes} Minuten");
        private static readonly Action<ILogger, string, Guid, Player, Exception?> _logMgrPlayerJoinedGameTimerStartAction =
            LoggerMessage.Define<string, Guid, Player>(LogLevel.Information, new EventId(25002, "MgrPlayerJoinedGameTimerStartLog"), "Zweiter Spieler {PlayerName} ist Spiel {GameId} beigetreten. Starte Timer für {StartPlayer}.");
        private static readonly Action<ILogger, Guid, Exception?> _logMgrGameOverTimerStopAction =
            LoggerMessage.Define<Guid>(LogLevel.Information, new EventId(25003, "MgrGameOverTimerStopLog"), "Spiel {GameId} ist vorbei. Timer wird gestoppt.");
        private static readonly Action<ILogger, Guid, Guid, string, Exception?> _logMgrGameNotFoundForCardActivationAction =
            LoggerMessage.Define<Guid, Guid, string>(LogLevel.Warning, new EventId(25004, "MgrGameNotFoundForCardActivationLog"), "[InMemoryGameManager] Spiel {GameId} nicht gefunden für Kartenaktivierung von Spieler {PlayerId}, Karte {CardTypeId}.");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logMgrGameNotFoundForCapturedPiecesAction =
            LoggerMessage.Define<Guid, Guid>(LogLevel.Warning, new EventId(25005, "MgrGameNotFoundForCapturedPiecesLog"), "[InMemoryGameManager] Spiel {GameId} nicht gefunden beim Abrufen der geschlagenen Figuren für Spieler {PlayerId}.");
        private static readonly Action<ILogger, Guid, Exception?> _logMgrGameNotFoundForGetPlayerIdByColorAction =
            LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(25006, "MgrGameNotFoundForGetPlayerIdByColorLog"), "[InMemoryGameManager] Spiel {GameId} nicht gefunden in GetPlayerIdByColor.");
        private static readonly Action<ILogger, Guid, Guid, Exception?> _logMgrGameNotFoundForGetOpponentInfoAction =
            LoggerMessage.Define<Guid, Guid>(LogLevel.Warning, new EventId(25007, "MgrGameNotFoundForGetOpponentInfoLog"), "[InMemoryGameManager] Spiel {GameId} nicht gefunden für GetOpponentInfo von Spieler {CurrentPlayerId}.");
        private static readonly Action<ILogger, Guid, Guid, string, Exception?> _logMgrPlayerHubConnectionRegisteredAction =
            LoggerMessage.Define<Guid, Guid, string>(LogLevel.Information, new EventId(25008, "MgrPlayerHubConnectionRegisteredLog"), "[InMemoryGameManager] Hub-Verbindung für Spieler {PlayerId} zu Spiel {GameId} registriert (Connection: {ConnectionId}).");
        private static readonly Action<ILogger, string, Guid, Exception?> _logMgrPlayerHubConnectionUnregisteredAction =
            LoggerMessage.Define<string, Guid>(LogLevel.Information, new EventId(25009, "MgrPlayerHubConnectionUnregisteredLog"), "[InMemoryGameManager] Hub-Verbindung {ConnectionId} von Spiel {GameId} deregistriert.");
        private static readonly Action<ILogger, Guid, Exception?> _logMgrGameNotFoundForRegisterPlayerHubAction =
            LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(25010, "MgrGameNotFoundForRegisterPlayerHubLog"), "[InMemoryGameManager] Spiel {GameId} nicht gefunden für RegisterPlayerHubConnection.");


        public ChessLogger(ILogger<TCategoryName> msLogger)
        {
            _msLogger = msLogger ?? throw new ArgumentNullException(nameof(msLogger));
        }

        // Implementierungen
        public void LogStartingGenericCardSwapAnimation(Guid playerId, string cardGivenName, string cardReceivedName) => _logStartingGenericCardSwapAnimationAction(_msLogger, playerId, cardGivenName, cardReceivedName, null);
        // ... (viele weitere Implementierungen bleiben unverändert) ...
        public void LogExtraTurnFirstMoveCausesCheck(Guid gameId, Guid playerId, string fromSquare, string toSquare) => _logExtraTurnFirstMoveCausesCheckAction(_msLogger, gameId, playerId, fromSquare, toSquare, null); // NEU

        public void LogCardActivationAnimationFinishedClient() => _logCardActivationAnimationFinishedClientAction(_msLogger, null);
        public void LogSpecificCardSwapAnimationFinishedClient() => _logSpecificCardSwapAnimationFinishedClientAction(_msLogger, null);
        public void LogUpdatePlayerNamesMismatch(Player opponentColorRetrieved, Player opponentColorExpected) => _logUpdatePlayerNamesMismatchAction(_msLogger, opponentColorRetrieved, opponentColorExpected, null);
        public void LogUpdatePlayerNamesNotFound(string errorMessage) => _logUpdatePlayerNamesNotFoundAction(_msLogger, errorMessage, null);
        public void LogUpdatePlayerNamesError(Exception ex) => _logUpdatePlayerNamesErrorAction(_msLogger, ex);
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

        public void LogCardInstancePlayed(Guid cardInstanceId, Guid playerId, string cardTypeId, string gameId)
        {
            _logCardInstancePlayedAction(_msLogger, cardInstanceId, playerId, cardTypeId, gameId, null);
        }
        public void LogCardInstanceNotFoundInHand(Guid cardInstanceId, Guid playerId, string gameId)
        {
            _logCardInstanceNotFoundInHandAction(_msLogger, cardInstanceId, playerId, gameId, null);
        }
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
        public void LogSacrificeEffectExecuted(Guid gameId, Guid playerId, string sacrificedPawnSquare) => _logSacrificeEffectExecutedAction(_msLogger, gameId, playerId, sacrificedPawnSquare, null);
        public void LogSacrificeEffectFailedPawnNotFound(Guid gameId, Guid playerId, string attemptedSquare) => _logSacrificeEffectFailedPawnNotFoundAction(_msLogger, gameId, playerId, attemptedSquare, null);
        public void LogSacrificeEffectFailedNotAPawn(Guid gameId, Guid playerId, string attemptedSquare, PieceType? actualType) => _logSacrificeEffectFailedNotAPawnAction(_msLogger, gameId, playerId, attemptedSquare, actualType, null);
        public void LogSacrificeEffectFailedWrongColor(Guid gameId, Guid playerId, string attemptedSquare, Player pieceColor) => _logSacrificeEffectFailedWrongColorAction(_msLogger, gameId, playerId, attemptedSquare, pieceColor, null);
        public void LogSacrificeEffectFailedWouldCauseCheck(Guid gameId, Guid playerId, string sacrificedPawnSquare) => _logSacrificeEffectFailedWouldCauseCheckAction(_msLogger, gameId, playerId, sacrificedPawnSquare, null);
        public void LogPawnPromotionPendingAfterCard(Guid gameId, Player player, string promotionSquare, string cardTypeId) => _logPawnPromotionPendingAfterCardAction(_msLogger, gameId, player, promotionSquare, cardTypeId, null);

    }
}