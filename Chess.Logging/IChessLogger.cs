using System;
using System.Collections.Generic;
using ChessLogic;
using ChessNetwork.DTOs;

namespace Chess.Logging
{
    public interface IChessLogger
    {
        // --- ChessClient.Pages.Chess.razor.cs Logs ---
        void LogStartingGenericCardSwapAnimation(Guid playerId, string cardGivenName, string cardReceivedName);
        void LogCardActivationAnimationFinishedClient();
        void LogSpecificCardSwapAnimationFinishedClient();
        void LogUpdatePlayerNamesMismatch(Player opponentColorRetrieved, Player opponentColorExpected);
        void LogUpdatePlayerNamesNotFound(string errorMessage);
        void LogUpdatePlayerNamesError(Exception ex);
        void LogHandlePlayCardActivationAnimation(string cardTypeId, Guid playerId, Player playerColor);
        void LogHandleClientAnimationFinishedTriggered(string? lastCardId, bool isPendingSwapNull);
        void LogHandleReceiveCardSwapDetails(string givenCardName, string receivedCardName, bool isGenericAnimating);
        void LogActuallyStartingSpecificSwapAnim(string givenCardName, string receivedCardName);
        void LogGenericCardAnimationStartedForCard(string cardName);
        void LogClientCriticalServicesNullOnInit(string serviceName);
        void LogClientSignalRConnectionWarning(string errorMessage);

        // --- ChessServer.Controllers.GamesController.cs Logs ---
        void LogMoveProcessingError(Guid gameId, string fromSquare, string toSquare, Exception? ex);
        void LogGameCreated(Guid gameId, string playerName, int initialMinutes);
        void LogPlayerJoinedGame(string playerName, Guid gameId);
        void LogGameNotFoundOnJoinAttempt(Guid gameId);
        void LogInvalidOperationOnJoinGame(Guid gameId, string errorMessage);
        void LogApplyMoveInfo(Guid gameId, Guid playerId, bool isValid);
        void LogSignalRUpdateInfo(Guid gameId, Player nextPlayerTurn, GameStatusDto statusForNextPlayer, string? lastMoveFrom, string? lastMoveTo, string? cardEffectSquareCount);
        void LogOnTurnChangedSentToHub(Guid gameId);
        void LogOnTimeUpdateSentAfterMove(Guid gameId);
        void LogGameNotFoundOnMove(Guid gameId, Guid playerId);
        void LogInvalidOperationOnMove(Guid gameId, Guid playerId);
        void LogGameNotFoundOnTimeRequest(Guid gameId);
        void LogErrorGettingTime(Guid gameId, Exception? ex);
        void LogGameHistoryNullFromManager(Guid gameId);
        void LogGameHistoryKeyNotFound(Guid gameId);
        void LogGameHistoryGenericError(Guid gameId, Exception? ex);
        void LogCardActivationAttempt(Guid gameId, Guid playerId, string cardTypeId);
        void LogCardActivationFailedController(Guid gameId, Guid playerId, string cardTypeId, string reason);
        void LogCardActivationSuccessController(Guid gameId, Guid playerId, string cardTypeId);
        void LogSignalingPlayerToDrawCard(Guid gameId, Guid playerIdToDraw);
        void LogGettingCapturedPieces(Guid gameId, Guid playerId);
        void LogGameNotFoundCapturedPieces(Guid gameId, Guid playerId);
        void LogErrorGettingCapturedPieces(Guid gameId, Guid playerId, Exception? ex);
        void LogControllerGameNotFound(Guid gameId, string actionName);
        void LogControllerMoveSentCardToHand(string cardName, string connectionId, Guid playerId);
        void LogControllerConnectionIdNotFoundNoMoreCards(string actionSource, Guid playerId);
        void LogControllerConnectionIdNotFoundGeneric(string actionSource, Guid playerId);
        void LogControllerActivateCardSentCardToHand(string cardName, string connectionId, Guid playerId);
        void LogExtraTurnFirstMoveCausesCheck(Guid gameId, Guid playerId, string fromSquare, string toSquare); 
        void LogControllerCouldNotDeterminePlayerIdForStatus(Guid gameId, Player playerColor);
        void LogControllerErrorGettingOpponentInfo(Guid gameId, Guid playerId, Exception? ex);
        void LogControllerErrorGettingLegalMoves(Guid gameId, Guid playerId, string fromSquare, Exception? ex);
        void LogControllerConnectionIdForPlayerNotFound(Guid playerId);

        // --- ChessServer.Hubs.ChessHub.cs Logs ---
        void LogHubClientConnected(string connectionId);
        void LogHubClientDisconnected(string connectionId, string? errorMessage, Exception? ex);
        void LogHubClientJoiningGameGroup(string connectionId, string gameIdString);
        void LogHubClientAddedToGameGroup(string connectionId, string gameIdString);
        void LogHubPlayerJoinedNotificationSent(string joiningPlayerName, string gameIdString, int playerCount);
        void LogHubClientLeavingGameGroup(string connectionId, string gameIdString);
        void LogHubClientRemovedFromGameGroup(string connectionId, string gameIdString);
        void LogHubGameNotFoundForPlayerCount(Guid gameIdGuid, string connectionId);
        void LogHubJoinGameInvalidGameIdFormat(string gameIdString, string connectionId);
        void LogHubPlayerRegisteredToHub(Guid playerId, string connectionId, Guid gameId);
        void LogHubPlayerDeregisteredFromHub(string connectionId);
        void LogHubSendingInitialHand(Guid playerId, string connectionId, Guid gameId, int handSize, int drawPileCount);
        void LogHubFailedToSendInitialHandSessionNotFound(Guid gameId, Guid playerId);
        void LogHubPlayerActuallyJoinedGame(string playerName, string gameIdString);
        void LogHubPlayerMappingRemovedOnDisconnect(Guid playerId);
        void LogHubConnectionRemovedFromGameOnDisconnect(string connectionId, Guid gameId);
        void LogHubErrorSendingInitialHand(Guid playerId, Guid gameId, Exception? ex);

        // --- ChessServer.Services.GameSession.cs Logs ---
        void LogSessionErrorGetNameByColor(Guid gameId, Exception? ex);
        void LogSessionSendTimeUpdate(Guid gameId, TimeSpan whiteTime, TimeSpan blackTime, Player? activePlayer);
        void LogSessionErrorIsPlayerTurn(Guid gameId, Exception? ex);
        void LogSessionColorNotDetermined(Guid gameId, Guid playerId, int playerCount);
        void LogGameEndedByTimeoutInSession(Guid gameId, Player expiredPlayer);
        void LogSessionCardActivationAttempt(Guid gameId, Guid playerId, string cardId);
        void LogSessionCardActivationFailed(Guid gameId, Guid playerId, string cardId, string reason);
        void LogSessionCardActivationSuccess(Guid gameId, Guid playerId, string cardId);
        void LogExtraTurnEffectApplied(Guid gameId, Guid playerId, string cardId); 
        void LogPlayerMoveCountIncreased(Guid gameId, Guid playerId, int moveCount); 
        void LogPlayerCardDrawIndicated(Guid gameId, Guid playerId);
        void LogNotifyingOpponentOfCardPlay(Guid gameId, Guid playerId, string cardId);
        void LogCapturedPieceAdded(Guid gameId, PieceType pieceType, Player playerColor);
        void LogPawnPromotionMoveSelection(Guid gameId, string from, string toSquare, PieceType? promotionType);
        void LogPawnPromotionMoveFound(Guid gameId, string from, string toSquare, PieceType promotionType);
        void LogPawnPromotionMoveNotFound(Guid gameId, string from, string toSquare, PieceType? promotionType);
        void LogOnTurnChangedFromSession(Guid gameId, string? lastMoveFrom, string? lastMoveTo, string cardEffectType);
        void LogPlayerDeckInitialized(Guid playerId, Guid gameId, int drawPileCount);
        void LogDrawAttemptUnknownPlayer(Guid playerId, Guid gameId);
        void LogNoDrawPileForPlayer(Guid playerId, Guid gameId);
        void LogPlayerDrawPileEmpty(Guid playerId, Guid gameId);
        void LogPlayerDrewCardFromOwnDeck(Guid playerId, string cardName, string cardId, Guid gameId, int remainingInPlayerDrawPile);
        void LogCannotFindPlayerDrawPileForCount(Guid playerId, Guid gameId);
        void LogCurrentPlayerNotFoundForOpponentDetails(Guid currentPlayerId, Guid gameId);
        void LogNoOpponentFoundForPlayer(Guid currentPlayerId, Player currentPlayerColor, Guid gameId);
        void LogCardInstancePlayed(Guid cardInstanceId, Guid playerId, string cardTypeId, string gameId); 
        void LogCardInstanceNotFoundInHand(Guid cardInstanceId, Guid playerId, string gameId);
        void LogPlayerAttemptingCardWhileInCheck(Guid gameId, Guid playerId, Player playerColor, string cardTypeId);
        void LogPlayerStillInCheckAfterCardTurnNotEnded(Guid gameId, Guid playerId, string cardTypeId);
        void LogPlayerInCheckTriedInvalidMove(Guid gameId, Guid playerId, Player playerColor, string fromSquare, string toSquare);
        void LogPlayerTriedMoveThatDidNotResolveCheck(Guid gameId, Guid playerId, Player playerColor, string fromSquare, string toSquare);
        void LogPawnPromotionPendingAfterCard(Guid gameId, Player player, string promotionSquare, string cardTypeId);


        // --- ChessServer.Services.CardEffects Logs ---
        void LogAddTimeEffectApplied(Player playerColor, Guid playerId, Guid gameId);
        void LogCardSwapEffectExecuted(Guid swappedOutPlayerCardInstanceId, Guid swappedInOpponentCardInstanceId, Guid playerId, Guid gameId);
        void LogCardSwapEffectOpponentNoCards(Guid playerId, Guid gameId);
        void LogCardSwapEffectPlayerCardInstanceNotFound(Guid missingCardInstanceId, Guid playerId, Guid gameId);
        void LogExtraZugEffectApplied(Guid playerId, Guid gameId);
        void LogPositionSwapEffectExecuted(string? fromSquare, string? toSquare, Guid playerId, Guid gameId);
        void LogRebirthEffectExecuted(PieceType pieceType, string? targetSquare, Player playerColor, Guid playerId, Guid gameId);
        void LogRebirthEffectFailedString(string reason, string? pieceTypeString, string? targetSquare, Guid gameId);
        void LogRebirthEffectFailedEnum(string reason, PieceType pieceTypeEnum, string? targetSquare, Guid gameId);
        void LogSubtractTimeEffectApplied(Player opponentColor, Player playerColor, Guid playerId, Guid gameId);
        void LogTeleportEffectExecuted(string? fromSquare, string? toSquare, Guid playerId, Guid gameId);
        void LogTimeSwapEffectApplied(Player player1Color, Player player2Color, Guid gameId);
        void LogSacrificeEffectExecuted(Guid gameId, Guid playerId, string sacrificedPawnSquare);
        void LogSacrificeEffectFailedPawnNotFound(Guid gameId, Guid playerId, string attemptedSquare);
        void LogSacrificeEffectFailedNotAPawn(Guid gameId, Guid playerId, string attemptedSquare, PieceType? actualType);
        void LogSacrificeEffectFailedWrongColor(Guid gameId, Guid playerId, string attemptedSquare, Player pieceColor);
        void LogSacrificeEffectFailedWouldCauseCheck(Guid gameId, Guid playerId, string sacrificedPawnSquare);


        // --- ChessServer.Services.InMemoryGameManager.cs Logs ---
        void LogMgrGameCreated(Guid gameId, string playerName, Guid playerId, Player color, int initialMinutes);
        void LogMgrPlayerJoinedGameTimerStart(string playerName, Guid gameId, Player startPlayer);
        void LogMgrGameOverTimerStop(Guid gameId);
        void LogMgrGameNotFoundForCardActivation(Guid gameId, Guid playerId, string cardTypeId);
        void LogMgrGameNotFoundForCapturedPieces(Guid gameId, Guid playerId);
        void LogMgrGameNotFoundForGetPlayerIdByColor(Guid gameId);
        void LogMgrGameNotFoundForGetOpponentInfo(Guid gameId, Guid currentPlayerId);
        void LogMgrPlayerHubConnectionRegistered(Guid playerId, Guid gameId, string connectionId);
        void LogMgrPlayerHubConnectionUnregistered(string connectionId, Guid gameId);
        void LogMgrGameNotFoundForRegisterPlayerHub(Guid gameId);


        // NEU: Für PvC Spiele
        void LogPvCGameCreated(Guid gameId, string playerName, Player playerColor, string computerDifficulty);
        void LogComputerFetchingMove(Guid gameId, string fen, int depth);
        void LogComputerReceivedMove(Guid gameId, string move, string fen, int depth);
        void LogComputerMoveError(Guid gameId, string fen, int depth, string errorMessage);
        void LogComputerMakingMove(Guid gameId, string from, string toSquare);
        void LogComputerStartingInitialMove(Guid gameId, Player computerColor, Player currentPlayer); // NEU HINZUGEFÜGT

        void LogGetPlayerIdByColorFailed(Guid gameId, Player color, Guid? whiteId, Guid? blackId); // NEU


        // NEU: Für Verzögerung vor Computerzug nach Kartenaktivierung durch Mensch
        void LogComputerTurnDelayAfterCard(Guid gameId, string cardTypeId, double delaySeconds);
        void LogComputerTurnDelayCardSwap(Guid gameId, double delaySeconds);
        void LogComputerTimerPausedForAnimation(Guid gameId, Player computerColor);
        void LogComputerTimerResumedAfterAnimation(Guid gameId, Player computerColor);
        void LogComputerSkippingTurnAfterAnimationDelay(Guid gameId, string cardTypeId);


        // Für ChessBoard Infos
        void LogIsChessboardEnabledStatus(ChessboardEnabledStatusLogArgs args); // Parameter geändert

        void LogHandleHubTurnChangedClientInfo(Player nextPlayer, GameStatusDto statusForNextPlayer, string? lastMoveFromServerFrom, string? lastMoveFromServerTo, int cardEffectsCount);
        void LogAwaitingTurnConfirmationStatus(bool flagStatus, string context);

        // Duplicated Card

        void LogClientAttemptedToAddDuplicateCardInstance(Guid instanceId, string cardName);

    }
}