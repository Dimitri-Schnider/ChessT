using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System.Windows.Media;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ChessAnalysis
{
    public class CardViewModel : INotifyPropertyChanged
    {
        public string PlayerName { get; set; } = "";
        public int ActivationMoveNumber { get; set; }
        public int PlayerColor { get; set; }
        public string CardName { get; set; } = "";
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
        {
            get
            {
                var uri = new Uri(
                    $"pack://application:,,,/ChessAnalysis;component/{ImagePath}",
                    UriKind.Absolute);

                // <<< Hier Debug-Ausgabe einfügen >>>
                Debug.WriteLine("[DBG] Loading Card Image URI: " + uri);

                return new BitmapImage(uri);
            }
        }

        // INotifyPropertyChanged impl.
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
        
    

    //public BitmapImage ImageSource
    //  => new BitmapImage(new Uri(
    //    $"pack://application:,,,/ChessAnalysis;Items/Cards/{ImagePath}",
    //  UriKind.Absolute));



