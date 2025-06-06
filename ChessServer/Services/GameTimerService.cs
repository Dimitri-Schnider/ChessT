using ChessLogic;
using ChessNetwork.DTOs;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;

namespace ChessServer.Services
{
    // Verwaltet die Spielzeit (Bedenkzeit) für eine einzelne Schachpartie.
    public class GameTimerService : IDisposable
    {
        #region Fields

        private readonly Guid _gameId;
        private readonly ILogger<GameTimerService> _logger;
        private readonly object _lock = new object();

        private TimeSpan _whiteRemainingTime;           // Verbleibende Zeit für Weiss.
        private TimeSpan _blackRemainingTime;           // Verbleibende Zeit für Schwarz.
        private Timer? _timer;                          // Interner .NET Timer.
        private Player? _activePlayerForTimer;          // Spieler, dessen Uhr aktuell läuft.
        private DateTime _lastTickTime;                 // Zeitpunkt des letzten Timer-Ticks.
        private bool _isGameOver;                       // Flag, ob das Spiel beendet ist.
        private bool _isPausedInternal;                 // Interne Variable für den Pausenzustand.

        private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1);    // Intervall für Timer-Ticks.
        private static readonly TimeSpan MinimumTime = TimeSpan.FromMinutes(1);     // Minimale Zeit, die einem Spieler verbleiben kann.

        #endregion

        #region LoggerMessage Definitions

        private static readonly Action<ILogger, Guid, Player?, TimeSpan, TimeSpan, Exception?> _logTimerStarting =
            LoggerMessage.Define<Guid, Player?, TimeSpan, TimeSpan>(LogLevel.Information, new EventId(350, nameof(_logTimerStarting)), "[GameTimerService] Timer für Spiel {GameId}, Spieler {Player} wird gestartet. W: {WhiteTime}, B: {BlackTime}");

        private static readonly Action<ILogger, Guid, Player?, Exception?> _logTimerSwitching =
            LoggerMessage.Define<Guid, Player?>(LogLevel.Information, new EventId(351, nameof(_logTimerSwitching)), "[GameTimerService] Timer für Spiel {GameId} wird auf Spieler {Player} umgeschaltet.");

        private static readonly Action<ILogger, Player?, double, Guid, Exception?> _logTimerStoppedAndCalculated =
            LoggerMessage.Define<Player?, double, Guid>(LogLevel.Debug, new EventId(352, nameof(_logTimerStoppedAndCalculated)), "[GameTimerService] Timer gestoppt für Spieler {Player} in Spiel {GameId}. Vergangene Zeit: {ElapsedSeconds}s.");

        private static readonly Action<ILogger, Guid, Player?, TimeSpan, TimeSpan, Exception?> _logTimerTickTrace =
            LoggerMessage.Define<Guid, Player?, TimeSpan, TimeSpan>(LogLevel.Trace, new EventId(353, nameof(_logTimerTickTrace)), "[GameTimerService] Tick für Spieler {Player} in Spiel {GameId}. W: {WhiteTime}, B: {BlackTime}");

        private static readonly Action<ILogger, Player, Guid, Exception?> _logPlayerTimeExpired =
            LoggerMessage.Define<Player, Guid>(LogLevel.Information, new EventId(354, nameof(_logPlayerTimeExpired)), "[GameTimerService] Zeit für Spieler {Player} in Spiel {GameId} abgelaufen.");

        private static readonly Action<ILogger, Guid, Exception?> _logTimerDisposed =
            LoggerMessage.Define<Guid>(LogLevel.Trace, new EventId(355, nameof(_logTimerDisposed)), "[GameTimerService] Interner Timer gestoppt/entsorgt für Spiel {GameId}.");

        private static readonly Action<ILogger, Guid, Exception?> _logGameOverTimerStopped =
            LoggerMessage.Define<Guid>(LogLevel.Information, new EventId(356, nameof(_logGameOverTimerStopped)), "[GameTimerService] Spiel {GameId} als beendet markiert, Timer gestoppt.");

        private static readonly Action<ILogger, TimeSpan, Player, Guid, TimeSpan, TimeSpan, Exception?> _logTimeAdjustedTimer =
           LoggerMessage.Define<TimeSpan, Player, Guid, TimeSpan, TimeSpan>(LogLevel.Information, new EventId(357, nameof(_logTimeAdjustedTimer)), "[GameTimerService] {TimeAmount} für Spieler {Player} in Spiel {GameId} angepasst. W: {WhiteTime}, B: {BlackTime}");

        private static readonly Action<ILogger, Player, Player, Guid, TimeSpan, TimeSpan, Exception?> _logTimeSwappedTimer =
            LoggerMessage.Define<Player, Player, Guid, TimeSpan, TimeSpan>(LogLevel.Information, new EventId(358, nameof(_logTimeSwappedTimer)), "[GameTimerService] Zeiten zwischen Spieler {Player1} und {Player2} in Spiel {GameId} getauscht. W: {WhiteTime}, B: {BlackTime}");

        private static readonly Action<ILogger, Player, Guid, Exception?> _logTimeExpiredAfterManipulation =
            LoggerMessage.Define<Player, Guid>(LogLevel.Information, new EventId(359, nameof(_logTimeExpiredAfterManipulation)), "[GameTimerService] Zeit für Spieler {Player} in Spiel {GameId} durch Manipulation auf 0 oder weniger gefallen und als abgelaufen markiert.");

        private static readonly Action<ILogger, Guid, Player?, Exception?> _logTimerPaused =
            LoggerMessage.Define<Guid, Player?>(LogLevel.Debug, new EventId(360, nameof(_logTimerPaused)), "[GameTimerService] Timer für Spiel {GameId}, Spieler {ActivePlayer} pausiert.");

        private static readonly Action<ILogger, Guid, Player?, Exception?> _logTimerResumed =
            LoggerMessage.Define<Guid, Player?>(LogLevel.Debug, new EventId(361, nameof(_logTimerResumed)), "[GameTimerService] Timer für Spiel {GameId}, Spieler {ActivePlayer} fortgesetzt.");

        #endregion

        #region Events & Properties

        public event Action<TimeUpdateDto>? OnTimeUpdated;
        public event Action<Player>? OnTimeExpired;

        public bool IsPaused
        {
            get
            {
                lock (_lock)
                {
                    return _isPausedInternal;
                }
            }
        }

        #endregion

        public GameTimerService(Guid gameId, TimeSpan initialTimePerPlayer, ILogger<GameTimerService> logger)
        {
            _gameId = gameId;
            _whiteRemainingTime = initialTimePerPlayer;
            _blackRemainingTime = initialTimePerPlayer;
            _logger = logger;
            _isGameOver = false;
            _isPausedInternal = false;
        }

        #region Public Control Methods

        // Startet oder wechselt den Timer auf den angegebenen Spieler.
        public void StartPlayerTimer(Player player, bool isGameOver)
        {
            lock (_lock)
            {
                _isGameOver = isGameOver;
                _isPausedInternal = false;
                if (_isGameOver)
                {
                    StopTimerInternal();
                    return;
                }

                _activePlayerForTimer = player;
                _lastTickTime = DateTime.UtcNow;

                StopTimerInternal();
                _timer = new Timer(TimerTick, null, TickInterval, TickInterval);
                _logTimerStarting(_logger, _gameId, _activePlayerForTimer, _whiteRemainingTime, _blackRemainingTime, null);
            }
            TriggerTimeUpdate();
        }

        // Pausiert den laufenden Timer.
        public void PauseTimer()
        {
            lock (_lock)
            {
                if (!_isGameOver && !_isPausedInternal && _timer != null)
                {
                    _isPausedInternal = true;
                    TimeSpan elapsedSinceLastTick = DateTime.UtcNow - _lastTickTime;
                    if (_activePlayerForTimer == Player.White)
                    {
                        _whiteRemainingTime -= elapsedSinceLastTick;
                        if (_whiteRemainingTime < TimeSpan.Zero) _whiteRemainingTime = TimeSpan.Zero;
                    }
                    else if (_activePlayerForTimer == Player.Black)
                    {
                        _blackRemainingTime -= elapsedSinceLastTick;
                        if (_blackRemainingTime < TimeSpan.Zero) _blackRemainingTime = TimeSpan.Zero;
                    }
                    _lastTickTime = DateTime.UtcNow;

                    _timer.Change(Timeout.Infinite, Timeout.Infinite);
                    _logTimerPaused(_logger, _gameId, _activePlayerForTimer, null);
                    TriggerTimeUpdate();
                }
            }
        }

        // Setzt einen pausierten Timer fort.
        public void ResumeTimer()
        {
            lock (_lock)
            {
                if (!_isGameOver && _isPausedInternal && _activePlayerForTimer.HasValue)
                {
                    _isPausedInternal = false;
                    _lastTickTime = DateTime.UtcNow;
                    if (_timer == null)
                    {
                        _timer = new Timer(TimerTick, null, TickInterval, TickInterval);
                    }
                    else
                    {
                        _timer.Change(TickInterval, TickInterval);
                    }
                    _logTimerResumed(_logger, _gameId, _activePlayerForTimer, null);
                    TriggerTimeUpdate();
                }
            }
        }

        // Hält den Timer an, berechnet die seit dem letzten Tick vergangene Zeit und gibt sie zurück.
        public TimeSpan StopAndCalculateElapsedTime()
        {
            lock (_lock)
            {
                if (_activePlayerForTimer == null || _isGameOver || _isPausedInternal || _timer == null)
                {
                    return TimeSpan.Zero;
                }

                TimeSpan elapsed = DateTime.UtcNow - _lastTickTime;
                StopTimerInternal();

                if (_activePlayerForTimer == Player.White)
                {
                    _whiteRemainingTime -= elapsed;
                    if (_whiteRemainingTime < TimeSpan.Zero) _whiteRemainingTime = TimeSpan.Zero;
                }
                else
                {
                    _blackRemainingTime -= elapsed;
                    if (_blackRemainingTime < TimeSpan.Zero) _blackRemainingTime = TimeSpan.Zero;
                }
                _logTimerStoppedAndCalculated(_logger, _activePlayerForTimer, elapsed.TotalSeconds, _gameId, null);
                _activePlayerForTimer = null;
                TriggerTimeUpdate();
                return elapsed;
            }
        }

        // Markiert das Spiel als beendet und stoppt den Timer.
        public void SetGameOver()
        {
            lock (_lock)
            {
                _isGameOver = true;
                _isPausedInternal = false;
                StopTimerInternal();
                _logGameOverTimerStopped(_logger, _gameId, null);
                TriggerTimeUpdate();
            }
        }

        #endregion

        #region Public Time Manipulation Methods

        // Fügt einem Spieler Zeit hinzu.
        public bool AddTime(Player player, TimeSpan timeToAdd)
        {
            lock (_lock)
            {
                if (_isGameOver) return false;
                if (player == Player.White)
                {
                    _whiteRemainingTime += timeToAdd;
                }
                else
                {
                    _blackRemainingTime += timeToAdd;
                }
                _logTimeAdjustedTimer(_logger, timeToAdd, player, _gameId, _whiteRemainingTime, _blackRemainingTime, null);
                TriggerTimeUpdate();
                return true;
            }
        }

        // Zieht einem Spieler Zeit ab.
        public bool SubtractTime(Player player, TimeSpan timeToSubtract)
        {
            lock (_lock)
            {
                if (_isGameOver) return false;
                if (player == Player.White)
                {
                    _whiteRemainingTime -= timeToSubtract;
                    if (_whiteRemainingTime < MinimumTime && _whiteRemainingTime > TimeSpan.Zero)
                    {
                        _whiteRemainingTime = MinimumTime;
                    }
                    else if (_whiteRemainingTime <= TimeSpan.Zero)
                    {
                        _whiteRemainingTime = TimeSpan.Zero;
                    }
                }
                else
                {
                    _blackRemainingTime -= timeToSubtract;
                    if (_blackRemainingTime < MinimumTime && _blackRemainingTime > TimeSpan.Zero)
                    {
                        _blackRemainingTime = MinimumTime;
                    }
                    else if (_blackRemainingTime <= TimeSpan.Zero)
                    {
                        _blackRemainingTime = TimeSpan.Zero;
                    }
                }
                _logTimeAdjustedTimer(_logger, -timeToSubtract, player, _gameId, _whiteRemainingTime, _blackRemainingTime, null);
                TriggerTimeUpdate();
                CheckForImmediateTimeoutAfterManipulation();
                return true;
            }
        }

        // Tauscht die Bedenkzeiten zwischen zwei Spielern.
        public bool SwapTimes(Player player1, Player player2)
        {
            lock (_lock)
            {
                if (_isGameOver || player1 == player2) return false;

                TimeSpan player1CurrentTime = (player1 == Player.White) ? _whiteRemainingTime : _blackRemainingTime;
                TimeSpan player2CurrentTime = (player2 == Player.White) ? _whiteRemainingTime : _blackRemainingTime;
                TimeSpan newPlayer1Time = player2CurrentTime;
                TimeSpan newPlayer2Time = player1CurrentTime;

                if (newPlayer1Time < MinimumTime && newPlayer1Time > TimeSpan.Zero) newPlayer1Time = MinimumTime;
                if (newPlayer2Time < MinimumTime && newPlayer2Time > TimeSpan.Zero) newPlayer2Time = MinimumTime;
                if (newPlayer1Time < TimeSpan.Zero) newPlayer1Time = TimeSpan.Zero;
                if (newPlayer2Time < TimeSpan.Zero) newPlayer2Time = TimeSpan.Zero;

                if (player1 == Player.White) _whiteRemainingTime = newPlayer1Time; else _blackRemainingTime = newPlayer1Time;
                if (player2 == Player.White) _whiteRemainingTime = newPlayer2Time; else _blackRemainingTime = newPlayer2Time;

                _logTimeSwappedTimer(_logger, player1, player2, _gameId, _whiteRemainingTime, _blackRemainingTime, null);
                TriggerTimeUpdate();
                CheckForImmediateTimeoutAfterManipulation();
                return true;
            }
        }

        #endregion

        #region Public Query Methods

        // Prüft, ob der Timer für einen bestimmten Spieler läuft.
        public bool IsRunningForPlayer(Player player)
        {
            lock (_lock)
            {
                return !_isGameOver && !_isPausedInternal && _activePlayerForTimer == player && _timer != null;
            }
        }

        // Gibt die aktuelle Zeit für einen Spieler zurück.
        public TimeSpan GetCurrentTimeForPlayer(Player player)
        {
            lock (_lock)
            {
                return player == Player.White ? _whiteRemainingTime : _blackRemainingTime;
            }
        }

        // Gibt ein DTO mit den aktuellen Zeiten und dem aktiven Spieler zurück.
        public TimeUpdateDto GetCurrentTimeUpdateDto()
        {
            lock (_lock)
            {
                return new TimeUpdateDto(
                    WhiteTime: _whiteRemainingTime,
                    BlackTime: _blackRemainingTime,
                    PlayerWhoseTurnItIs: (_isGameOver || _isPausedInternal) ? null : _activePlayerForTimer
                );
            }
        }

        #endregion

        #region Private Helper Methods

        // Die Methode, die vom Timer in regelmässigen Abständen aufgerufen wird.
        private void TimerTick(object? state)
        {
            lock (_lock)
            {
                if (_isGameOver || _isPausedInternal || _activePlayerForTimer == null || _timer == null)
                {
                    if (_isPausedInternal && _timer != null) _timer.Change(Timeout.Infinite, Timeout.Infinite);
                    else if (_isGameOver) StopTimerInternal();
                    return;
                }

                TimeSpan elapsed = DateTime.UtcNow - _lastTickTime;
                _lastTickTime = DateTime.UtcNow;

                bool timeExpiredThisTick = false;
                Player? expiredPlayer = null;
                if (_activePlayerForTimer == Player.White)
                {
                    _whiteRemainingTime -= elapsed;
                    if (_whiteRemainingTime <= TimeSpan.Zero)
                    {
                        _whiteRemainingTime = TimeSpan.Zero;
                        timeExpiredThisTick = true;
                        expiredPlayer = Player.White;
                    }
                }
                else
                {
                    _blackRemainingTime -= elapsed;
                    if (_blackRemainingTime <= TimeSpan.Zero)
                    {
                        _blackRemainingTime = TimeSpan.Zero;
                        timeExpiredThisTick = true;
                        expiredPlayer = Player.Black;
                    }
                }

                _logTimerTickTrace(_logger, _gameId, _activePlayerForTimer, _whiteRemainingTime, _blackRemainingTime, null);
                TriggerTimeUpdate();

                if (timeExpiredThisTick && expiredPlayer.HasValue)
                {
                    _isGameOver = true;
                    StopTimerInternal();
                    _logPlayerTimeExpired(_logger, expiredPlayer.Value, _gameId, null);
                    OnTimeExpired?.Invoke(expiredPlayer.Value);
                }
            }
        }

        // Stoppt den internen Timer sicher.
        private void StopTimerInternal()
        {
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
            _timer?.Dispose();
            _timer = null;
        }

        // Löst das OnTimeUpdated-Event aus.
        private void TriggerTimeUpdate()
        {
            OnTimeUpdated?.Invoke(GetCurrentTimeUpdateDto());
        }

        // Prüft nach einer Zeitmanipulation, ob ein Spieler sofort die Zeit überschritten hat.
        private void CheckForImmediateTimeoutAfterManipulation()
        {
            if (_isGameOver) return;
            Player? playerThatMightHaveExpired = null;

            if (_whiteRemainingTime <= TimeSpan.Zero)
            {
                playerThatMightHaveExpired = Player.White;
                _whiteRemainingTime = TimeSpan.Zero;
            }
            else if (_blackRemainingTime <= TimeSpan.Zero)
            {
                playerThatMightHaveExpired = Player.Black;
                _blackRemainingTime = TimeSpan.Zero;
            }

            if (playerThatMightHaveExpired.HasValue)
            {
                _isGameOver = true;
                StopTimerInternal();
                _logTimeExpiredAfterManipulation(_logger, playerThatMightHaveExpired.Value, _gameId, null);
                OnTimeExpired?.Invoke(playerThatMightHaveExpired.Value);
                TriggerTimeUpdate();
            }
        }

        #endregion

        // Gibt die vom Timer verwendeten Ressourcen frei.
        public void Dispose()
        {
            lock (_lock)
            {
                StopTimerInternal();
            }
            GC.SuppressFinalize(this);
        }
    }
}