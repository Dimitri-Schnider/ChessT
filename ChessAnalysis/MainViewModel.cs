using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;
using System.IO;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ChessAnalysis;
using System.Diagnostics;
using System.Windows.Threading;


namespace ChessAnalysis
{
    
    public class MainViewModel : INotifyPropertyChanged
    {
        private List<CardViewModel> _allPlayedCards = new();
        public ObservableCollection<CardViewModel> LeftPlayedCards { get; }
        = new ObservableCollection<CardViewModel>();

        public ObservableCollection<CardViewModel> RightPlayedCards { get; }
            = new ObservableCollection<CardViewModel>();
        
  
        public IReadOnlyList<double> PlaybackSpeeds { get; }
      = new List<double> { 0.5, 1.0, 2.0, 5.0 };

        // 2) Index der aktuellen Geschwindigkeit
        private int _speedIndex = 1; // startet bei 1.0s (PlaybackSpeeds[1])

        // 3) Property, die der Button bindet
        public double SelectedPlaybackSpeed
        {
            get => PlaybackSpeeds[_speedIndex];
            private set
            {
                // wird nicht direkt gesetzt, sondern über CycleSpeedCommand
                OnPropertyChanged(nameof(SelectedPlaybackSpeed));
                _autoPlayTimer.Interval = TimeSpan.FromSeconds(SelectedPlaybackSpeed);
            }
        }
        public ICommand CycleSpeedCommand { get; }

        private readonly DispatcherTimer _autoPlayTimer;
        private bool _isPlaying;
        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                if (_isPlaying != value)
                {
                    _isPlaying = value;
                    OnPropertyChanged(nameof(IsPlaying));
                }
            }
        }

        // RemainingTimeWhite
        private TimeSpan _remainingWhite = TimeSpan.Zero;         
        public TimeSpan RemainingTimeWhite
        {
            get => _remainingWhite;
            set
            {
                if (_remainingWhite != value)
                {
                    _remainingWhite = value;
                    OnPropertyChanged();
                }
            }
        }
       //  RemainingTimeBlack
        private TimeSpan _remainingBlack = TimeSpan.Zero;
        public TimeSpan RemainingTimeBlack
        {
            get => _remainingBlack;
            set
            {
                if (_remainingBlack != value)
                {
                    _remainingBlack = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _playerWhiteNameing = "";
        public string PlayerWhiteNameing
        {
            get => _playerWhiteNameing;
            set
            {
                if (_playerWhiteNameing != value)
                {
                    _playerWhiteNameing = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _playerBlackNaming = "";
        public string PlayerBlackNameing
        {
            get => _playerBlackNaming;
            set
            {
                if (_playerBlackNaming != value)
                {
                    _playerBlackNaming = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _currentMove;
        private double _squareSize;
        public double SquareSize
        {
            get => _squareSize;
            set
            {
                if (_squareSize != value)
                {
                    _squareSize = value;
                    OnPropertyChanged();
                    // jedem Piece mitgeben, wie groß ein Feld ist
                    foreach (var p in Pieces)
                        p.SquareSize = value;
                }
            }
        }
        public double BoardOffsetX { get; set; }
        public double BoardOffsetY { get; set; }
        public ObservableCollection<string> Items { get; set; } = new();
        public bool CanGoPrev => _currentMove > 0 ;
        public bool CanGoNext => _currentMove < Items.Count;
        public ICommand LoadJsonCommand { get; }
        public ICommand NextMoveCommand { get; }
        public ICommand PrevMoveCommand { get; }
        public ICommand PlayToggleCommand { get; }
        public ICommand SetSpeedCommand { get; }
        public ObservableCollection<PieceVieweModel> Pieces { get; }
           = new ObservableCollection<PieceVieweModel>();

        public MainViewModel()
        {
            // 1) fixe virtuelle Board-Größe 512x512
            // 2) Feldgröße berechnen: 512 / 8 = 64px
            SquareSize = 512.0 / 8.0;
            // 3) jetzt erst das Board füllen
            SetupInitialBoard();

            LoadJsonCommand = new RelayCommand( async _ => await LoadJsonAsync());
            NextMoveCommand = new RelayCommand(_ => OnNext(), _ => CanGoNext);
            PrevMoveCommand = new RelayCommand(_ => OnPrev(), _ => CanGoPrev);
            // PlayToggleCommand = new RelayCommand(_ => TogglePlay());
            // ResetCommand = new RelayCommand(_ => ResetGame());
            //SaveCommand = new RelayCommand(_ => SaveGame());
            _autoPlayTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(SelectedPlaybackSpeed)
            };
            _autoPlayTimer.Tick += (s, e) =>
            {
                if (CanGoNext) OnNext();
                else ToggleAutoPlay();
            };

            PlayToggleCommand = new RelayCommand(_ => ToggleAutoPlay());
            // CycleSpeedCommand initialisieren
            CycleSpeedCommand = new RelayCommand(_ => {
                // nächster Index, wrap-around
                _speedIndex = (_speedIndex + 1) % PlaybackSpeeds.Count;
                // PropertyChanged feuern
                OnPropertyChanged(nameof(SelectedPlaybackSpeed));
                // Timer-Intervall anpassen
                _autoPlayTimer.Interval = TimeSpan.FromSeconds(SelectedPlaybackSpeed);
            });



        }


        private void ToggleAutoPlay()
        {
            if (_autoPlayTimer.IsEnabled)
            {
                _autoPlayTimer.Stop();
                IsPlaying = false;
            }
            else if (CanGoNext)
            {
                _autoPlayTimer.Start();
                IsPlaying = true;
            }
            UpdateCanExecute();
        }

        //Beim Start des Programmes alle figuren an die richtige stellen posiotienren
        public void SetupInitialBoard()
        {
            Pieces.Clear();
            // 1) Weiße Bauern (Rank=1)
            for (int file = 0; file < 8; file++)
            {
                Pieces.Add(new PieceVieweModel
                {
                    File = file,
                    Rank = 1,
                    SquareSize = SquareSize,
                    // Achtung: weißer Bauer heißt PawnW.png
                    ImagePath = "Items/PawnW.png",
                    IsWhite = true
                });
            }

             // 2) Weiße Rückreihe (Rank=0)
             string[] whiteBack = {
                 "RookW","KnightW","BishopW",
                  "QueenW","KingW","BishopW",
                 "KnightW","RookW"
    };
            for (int file = 0; file < 8; file++)
            {
                Pieces.Add(new PieceVieweModel
                {
                    File = file,
                    Rank = 0,
                    SquareSize = SquareSize,
                    ImagePath = $"Items/{whiteBack[file]}.png",
                    IsWhite = true
                });
            }

            // 3) Schwarze Bauern (Rank=6)
            for (int file = 0; file < 8; file++)
            {
                Pieces.Add(new PieceVieweModel
                {
                    File = file,
                    Rank = 6,
                    SquareSize = SquareSize,
                    // schwarzer Bauer heißt PawnB.png
                    ImagePath = "Items/PawnB.png",
                    IsWhite = false
                   
                });
            }

            // 4) Schwarze Rückreihe (Rank=7)
            string[] blackBack = {
        "RookB","KnightB","BishopB",
        "QueenB","KingB","BishopB",
        "KnightB","RookB"
    };
            for (int file = 0; file < 8; file++)
            {
                Pieces.Add(new PieceVieweModel
                {
                    File = file,
                    Rank = 7,
                    SquareSize = SquareSize,
                    ImagePath = $"Items/{blackBack[file]}.png",
                    IsWhite = false,
                });
                foreach (var p in Pieces)
                {
                    Debug.WriteLine($"Piece at {p.File},{p.Rank}: URI = {p.ImageSource.UriSource}");
                }
            }
        }
        private void RefreshState()
        {
            // 1) Brett und Karten‐Leisten auf Anfang zurücksetzen
            SetupInitialBoard();
            LeftPlayedCards.Clear();
            RightPlayedCards.Clear();

            // 2) Bis currentMove alle Moves und Karten‐Aktivierungen anwenden
            for (int i = 0; i < _currentMove; i++)
            {
                // a) Move‐JSON parsen
                using var doc = JsonDocument.Parse(Items[i]);
                var move = doc.RootElement;

                // b) Figur verschieben 
                MovePiece(move);

                // c) Karten aktivieren
                foreach (var card in _allPlayedCards
                         .Where(c => c.ActivationMoveNumber == i + 1))
                {
                    if (card.PlayerColor == 1) LeftPlayedCards.Add(card);
                    else RightPlayedCards.Add(card);
                }
               // Debug.WriteLine($"[DBG] Nach Zug {i + 1}: Pieces.Count = {Pieces.Count}");
              //  foreach (var p in Pieces)
               // {
               //     Debug.WriteLine(
              //        $"    {p.ImagePath} @ ({p.File},{p.Rank}) IsWhite={p.IsWhite}");
              //  }



                //Zeit Updaten
                UpdateRemainingTimes(move);
            }
        }
        private void UpdateCanExecute()
        {
            (NextMoveCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (PrevMoveCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
        private void OnNext()
        {
            if (_currentMove < Items.Count)
                _currentMove++;
            RefreshState();
            UpdateCanExecute();
        }

        private void OnPrev()
        {
            if (_currentMove > 0)
                _currentMove--;
            RefreshState();
            UpdateCanExecute();
            
        }
        
        
        private async Task LoadJsonAsync()
        {
            // Beim laden einer neuen datei das Board wieder Initialieseieren
            SetupInitialBoard();

            var dlg = new OpenFileDialog { Filter = "JSON files (*.json)|*.json" };
            if (dlg.ShowDialog() != true) return;

            string path = dlg.FileName;

            try
            {
                // 1) Datei asynchron einlesen
                string json = await File.ReadAllTextAsync(path);

                // 2) Parsing in Hintergrund-Thread
                using var doc = await Task.Run(() => JsonDocument.Parse(json));
                JsonElement root = doc.RootElement;


                //Karten array auslesen
                _allPlayedCards.Clear();
                LeftPlayedCards.Clear();
                RightPlayedCards.Clear();
                if (root.TryGetProperty("PlayedCards", out var pcArray)
                    && pcArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var cardElem in pcArray.EnumerateArray())
                    {
                       

                        // Erzeuge dein CardViewModel (ggf. ImagePath pro CardId)
                        var cvm = new CardViewModel
                        {
                            ActivationMoveNumber = cardElem.GetProperty("MoveNumberWhenActivated").GetInt32(),
                            PlayerColor = cardElem.GetProperty("PlayerColor").GetInt32(),
                            PlayerName = cardElem.GetProperty("PlayerName").GetString()!,
                            CardName = cardElem.GetProperty("CardName").GetString()!,
                            ImagePath = $"Items/Cards/{cardElem.GetProperty("CardId").GetString()}.png"
                        };
                        _allPlayedCards.Add(cvm);
                    }
                }
               // Debug.WriteLine($"[DBG] LeftPlayedCards.Count  = {LeftPlayedCards.Count}");
               // Debug.WriteLine($"[DBG] RightPlayedCards.Count = {RightPlayedCards.Count}");

                // >>>> Spieler-Namen & InitialTime >>>>
                if (root.TryGetProperty("PlayerWhiteName", out var pwNameElem))
                    PlayerWhiteNameing = pwNameElem.GetString() ?? PlayerWhiteNameing;

                if (root.TryGetProperty("PlayerBlackName", out var pbNameElem))
                    PlayerBlackNameing = pbNameElem.GetString() ?? PlayerBlackNameing;

                if (root.TryGetProperty("InitialTimeMinutes", out var initMinElem)
                    && initMinElem.TryGetInt32(out var minutes))
                {


                    // 3) Array finden (Moves oder Wurzelarray)
                    JsonElement movesArray;
                    if (root.ValueKind == JsonValueKind.Object
                        && root.TryGetProperty("Moves", out var m)
                        && m.ValueKind == JsonValueKind.Array)
                        movesArray = m;
                    else if (root.ValueKind == JsonValueKind.Array)
                        movesArray = root;
                    else
                    {
                        Items.Clear();
                        Items.Add("Kein gültiges Moves-Array gefunden.");
                        return;
                    }

                    if (movesArray.GetArrayLength() > 0)
                    {
                        // das erste Move-Element holen und RemainingTime initial anzeigen
                        var firstMove = movesArray[0];
                        UpdateRemainingTimes(firstMove);
                    }


                    //  UI-Liste updaten (im UI-Thread)


                    {
                        Items.Clear();
                        foreach (var move in movesArray.EnumerateArray())
                            Items.Add(move.GetRawText());
                    }
                    ;
                    Debug.WriteLine("Items geladen");
                    Debug.WriteLine(PlayerBlackNameing);
                    Debug.WriteLine(PlayerWhiteNameing);
                    _currentMove = 0;
                    (NextMoveCommand as RelayCommand)!.RaiseCanExecuteChanged();
                }
            }
            catch (Exception ex)
            {

                {
                    Items.Clear();
                    Items.Add($"Fehler: {ex.Message}");
                }
            }
        }

        private void UpdateRemainingTimes(JsonElement move)
        {
            // White
            if (move.TryGetProperty(nameof(RemainingTimeWhite), out var rtw))
            {
                // Wenn's ein String ist, als TimeSpan parsen
                if (rtw.ValueKind == JsonValueKind.String
                    && TimeSpan.TryParse(rtw.GetString(), out var tsWhite))
                {
                    RemainingTimeWhite = tsWhite;
                }
                // ansonsten, falls es doch mal eine Zahl wäre, als Millisekunden
                else if (rtw.ValueKind == JsonValueKind.Number
                         && rtw.TryGetInt32(out var msWhite))
                {
                    RemainingTimeWhite = TimeSpan.FromMilliseconds(msWhite);
                }
            }

            // Black
            if (move.TryGetProperty(nameof(RemainingTimeBlack), out var rtb))
            {
                if (rtb.ValueKind == JsonValueKind.String
                    && TimeSpan.TryParse(rtb.GetString(), out var tsBlack))
                {
                    RemainingTimeBlack = tsBlack;
                }
                else if (rtb.ValueKind == JsonValueKind.Number
                         && rtb.TryGetInt32(out var msBlack))
                {
                    RemainingTimeBlack = TimeSpan.FromMilliseconds(msBlack);
                }
            }
           
            Debug.WriteLine(RemainingTimeBlack);
            Debug.WriteLine(RemainingTimeWhite);
        }
        private void MovePiece(JsonElement move)
        {
            string from = move.GetProperty("From").GetString()!;
            string to = move.GetProperty("To").GetString()!;

            // ——— Special 1: Nur Karte gespielt, keine Figur bewegen ———
            if (from == "card" && to == "play")
                return;

            // ——— Special 2: Wiedergeburt (Revival) aus dem Friedhof ———
            if (from == "graveyard")
            {
                // Zielkoordinate parsen
                int tx = to[0] - 'a';
                int ty = to[1] - '1';

                // Farbe und Bilddatei aus JSON ziehen
                bool isWhite = move.GetProperty("PlayerColor").GetInt32() == 1;
                // "PieceMoved" liefert z.B. "Black Queen" oder "White Knight"
                string pieceLabel = move.GetProperty("PieceMoved").GetString()!;
                // Bildname zusammenbasteln (z.B. "QueenB.png" oder "KnightW.png")
                string suffix = isWhite ? "W" : "B";
                string baseName = pieceLabel.Split(' ')[1];  // z.B. "Queen"
                string imageFile = $"Items/{baseName}{suffix}.png";

                // neues Piece erzeugen
                Pieces.Add(new PieceVieweModel
                {
                    File = tx,
                    Rank = ty,
                    SquareSize = SquareSize,
                    ImagePath = imageFile,
                    IsWhite = isWhite
                });
                return;
            }

            // ——— Special 3: Positions-Tausch (MoveType 7) ———
            if (move.GetProperty("ActualMoveType").GetInt32() == 7)
            {
                int fx = from[0] - 'a', fy = from[1] - '1';
                int tx = to[0] - 'a', ty = to[1] - '1';

                var p1 = Pieces.FirstOrDefault(p => p.File == fx && p.Rank == fy);
                var p2 = Pieces.FirstOrDefault(p => p.File == tx && p.Rank == ty);
                if (p1 != null && p2 != null)
                {
                    // einfach Koordinaten tauschen
                    (p1.File, p2.File) = (p2.File, p1.File);
                    (p1.Rank, p2.Rank) = (p2.Rank, p1.Rank);
                }
                return;
            }

            // ——— Normaler Zug mit Capture ———
            int fxx = from[0] - 'a', fyy = from[1] - '1';
            int txx = to[0] - 'a', tyy = to[1] - '1';

            var attacker = Pieces.FirstOrDefault(p => p.File == fxx && p.Rank == fyy);
            if (attacker == null) return;

            // entferne gegnerischen Stein auf dem Zielfeld (falls da einer steht)
            var victim = Pieces.FirstOrDefault(p =>
                p.File == txx &&
                p.Rank == tyy &&
                p.IsWhite != attacker.IsWhite);
            if (victim != null)
                Pieces.Remove(victim);

            // verschiebe den Angreifer
            attacker.File = txx;
            attacker.Rank = tyy;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));



    }

}

