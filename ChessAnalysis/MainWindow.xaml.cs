using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ChessAnalysis
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();


        }


        private void BoardCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            var vm = (MainViewModel)DataContext!;
            // 1) wie wird das Board-Image wirklich skaliert?
            double imgW = ChessBoardImage.ActualWidth;
            double imgH = ChessBoardImage.ActualHeight;

            // 2) Feld-Größe = imgW/8 (oder imgH/8, je nachdem quadratisch) 
            vm.SquareSize = imgW / 8.0;

            // 3) Offset ermitteln
            double offsetX = (BoardCanvas.ActualWidth - imgW) / 2.0;
            double offsetY = (BoardCanvas.ActualHeight - imgH) / 2.0;

            vm.BoardOffsetX = offsetX;
            vm.BoardOffsetY = offsetY;

            vm.SetupInitialBoard();
        }

    }
}
