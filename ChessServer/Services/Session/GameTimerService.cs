using Chess.Logging;
using ChessLogic;
using ChessNetwork.DTOs;
using System;
using System.Threading;

namespace ChessServer.Services.Session
{
    // Verwaltet die Spielzeit (Bedenkzeit) für eine einzelne Schachpartie.
    // Diese Klasse ist verantwortlich für das Starten, Stoppen, Pausieren und Anpassen der Uhren
    // und löst Events bei Zeit-Updates oder Zeitüberschreitungen aus.
    public class GameTimerService : IDisposable
    {
        #region Felder

        private readonly Guid _gameId;                                              // Die ID des zugehörigen Spiels.
        private readonly IChessLogger _logger;                                      // Dienst für das Logging.
        private readonly object _lock = new object();                               // Sperrobjekt zur Gewährleistung der Thread-Sicherheit.

        private TimeSpan _whiteRemainingTime;                                       // Verbleibende Zeit für Weiss.
        private TimeSpan _blackRemainingTime;                                       // Verbleibende Zeit für Schwarz.
        private Timer? _timer;                                                      // Interner .NET Timer, der die Zeit herunterzählt.
        private Player? _activePlayerForTimer;                                      // Spieler, dessen Uhr aktuell läuft.
        private DateTime _lastTickTime;                                             // Zeitpunkt des letzten Timer-Ticks zur präzisen Zeitberechnung.
        private bool _isGameOver;                                                   // Flag, ob das Spiel beendet ist.
        private bool _isPausedInternal;                                             // Interne Variable für den Pausenzustand.
        private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1);    // Intervall für Timer-Ticks.
        private static readonly TimeSpan MinimumTime = TimeSpan.FromMinutes(1);     // Minimale Zeit, die einem Spieler nach einer Manipulation verbleiben kann.

        #endregion

        #region Events & Eigenschaften

        // Event, das ausgelöst wird, um die Clients über eine Zeitänderung zu informieren.
        public event Action<TimeUpdateDto>? OnTimeUpdated;
        // Event, das ausgelöst wird, wenn die Zeit eines Spielers abläuft.
        public event Action<Player>? OnTimeExpired;

        // Gibt an, ob der Timer aktuell pausiert ist.
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

        // Konstruktor: Initialisiert den Timer-Dienst mit der Startzeit und dem zentralen Logger.
        public GameTimerService(Guid gameId, TimeSpan initialTimePerPlayer, IChessLogger logger)
        {
            _gameId = gameId;
            _whiteRemainingTime = initialTimePerPlayer;
            _blackRemainingTime = initialTimePerPlayer;
            _logger = logger;
            _isGameOver = false;
            _isPausedInternal = false;
        }

        #region Öffentliche Steuerungs-Methoden

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
                _logger.LogTimerStarting(_gameId, _activePlayerForTimer, _whiteRemainingTime, _blackRemainingTime);
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
                    // Berechnet die seit dem letzten Tick vergangene Zeit und zieht sie ab, um präzise zu bleiben.
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
                    // Stoppt den Timer, indem das Intervall auf unendlich gesetzt wird.
                    _timer.Change(Timeout.Infinite, Timeout.Infinite);
                    _logger.LogTimerPaused(_gameId, _activePlayerForTimer);
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
                    _logger.LogTimerResumed(_gameId, _activePlayerForTimer);
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

                // Zieht die letzte vergangene Zeitspanne ab.
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
                _logger.LogTimerStoppedAndCalculated(_activePlayerForTimer, elapsed.TotalSeconds, _gameId);
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
                _logger.LogGameOverTimerStopped(_gameId);
                TriggerTimeUpdate();
            }
        }

        #endregion

        #region Öffentliche Methoden zur Zeitmanipulation

        // Fügt einem Spieler Zeit hinzu.
        public virtual bool AddTime(Player player, TimeSpan timeToAdd)
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
                _logger.LogTimeAdjustedTimer(timeToAdd, player, _gameId, _whiteRemainingTime, _blackRemainingTime);
                TriggerTimeUpdate();
                return true;
            }
        }

        // Zieht einem Spieler Zeit ab, wobei eine minimale Restzeit sichergestellt wird.
        public virtual bool SubtractTime(Player player, TimeSpan timeToSubtract)
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
                _logger.LogTimeAdjustedTimer(-timeToSubtract, player, _gameId, _whiteRemainingTime, _blackRemainingTime);
                TriggerTimeUpdate();
                CheckForImmediateTimeoutAfterManipulation();
                return true;
            }
        }

        // Tauscht die Bedenkzeiten zwischen zwei Spielern, wobei eine minimale Restzeit sichergestellt wird.
        public bool SwapTimes(Player player1, Player player2)
        {
            lock (_lock)
            {
                if (_isGameOver || player1 == player2) return false;
                TimeSpan player1CurrentTime = player1 == Player.White ? _whiteRemainingTime : _blackRemainingTime;
                TimeSpan player2CurrentTime = player2 == Player.White ? _whiteRemainingTime : _blackRemainingTime;
                TimeSpan newPlayer1Time = player2CurrentTime;
                TimeSpan newPlayer2Time = player1CurrentTime;

                if (newPlayer1Time < MinimumTime && newPlayer1Time > TimeSpan.Zero) newPlayer1Time = MinimumTime;
                if (newPlayer2Time < MinimumTime && newPlayer2Time > TimeSpan.Zero) newPlayer2Time = MinimumTime;
                if (newPlayer1Time < TimeSpan.Zero) newPlayer1Time = TimeSpan.Zero;
                if (newPlayer2Time < TimeSpan.Zero) newPlayer2Time = TimeSpan.Zero;

                if (player1 == Player.White) _whiteRemainingTime = newPlayer1Time; else _blackRemainingTime = newPlayer1Time;
                if (player2 == Player.White) _whiteRemainingTime = newPlayer2Time; else _blackRemainingTime = newPlayer2Time;

                _logger.LogTimeSwappedTimer(player1, player2, _gameId, _whiteRemainingTime, _blackRemainingTime);
                TriggerTimeUpdate();
                CheckForImmediateTimeoutAfterManipulation();
                return true;
            }
        }

        #endregion

        #region Öffentliche Abfrage-Methoden

        // Prüft, ob der Timer für einen bestimmten Spieler läuft.
        public bool IsRunningForPlayer(Player player)
        {
            lock (_lock)
            {
                return !_isGameOver && !_isPausedInternal && _activePlayerForTimer == player && _timer != null;
            }
        }

        // Gibt die aktuelle Zeit für einen Spieler zurück.
        public virtual TimeSpan GetCurrentTimeForPlayer(Player player)
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
                    PlayerWhoseTurnItIs: _isGameOver || _isPausedInternal ? null : _activePlayerForTimer
                );
            }
        }

        #endregion

        #region Private Hilfsmethoden

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
                // Zieht die vergangene Zeit vom aktiven Spieler ab.
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

                _logger.LogTimerTickTrace(_gameId, _activePlayerForTimer, _whiteRemainingTime, _blackRemainingTime);
                TriggerTimeUpdate();

                // Wenn die Zeit abgelaufen ist, wird das Spiel beendet.
                if (timeExpiredThisTick && expiredPlayer.HasValue)
                {
                    _isGameOver = true;
                    StopTimerInternal();
                    _logger.LogPlayerTimeExpired(expiredPlayer.Value, _gameId);
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
                _logger.LogTimeExpiredAfterManipulation(playerThatMightHaveExpired.Value, _gameId);
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