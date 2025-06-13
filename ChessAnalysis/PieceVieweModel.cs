using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ChessAnalysis
{
    public class PieceVieweModel : INotifyPropertyChanged
    {
        private int _file;

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
                    OnPropertyChanged(nameof(CanvasLeft));
                    OnPropertyChanged(nameof(CanvasTop));
                }
            }
        }
        public bool IsWhite { get; set; }

        public int File
        {
            get => _file;
            set
            {
                if (_file != value)
                {
                    _file = value;
                    OnPropertyChanged();                    // für File selbst
                    OnPropertyChanged(nameof(CanvasLeft));  // für Abhängigkeit
                }
            }
        }

        private string _imagePath = "";
        public string ImagePath
        {
            get => _imagePath;
            set
            {
                if (_imagePath != value)
                {
                    _imagePath = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ImageSource));
                }
            }
        }
        public BitmapImage ImageSource
        => new BitmapImage(new Uri(
               $"pack://application:,,,/ChessAnalysis;component/{ImagePath}",
               UriKind.Absolute));

        private int _rank;
        public int Rank
        {
            get => _rank;
            set
            {
                if (_rank != value)
                {
                    _rank = value;
                    OnPropertyChanged();                   // für Rank selbst
                    OnPropertyChanged(nameof(CanvasTop));  // für Abhängigkeit
                }
            }
        }

        // … rest wie gehabt …
        public double CanvasLeft => File * SquareSize;
        public double CanvasTop => (7 - Rank) * SquareSize;

        // INotifyPropertyChanged …
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}