using System.Linq;
using System.Windows;
using PicasaReboot.Core;
using PicasaReboot.Windows.ViewModels;

namespace PicasaReboot.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var applicationViewModel = new ApplicationViewModel(new ImageService());
            DataContext = applicationViewModel;

            applicationViewModel.Initialize(@"C:\Users\StanleyGoldman\Dropbox\Camera Uploads");
        }
    }
}
