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

            var imageFileSystemService = new ImageFileSystemService();

            var files = imageFileSystemService.ListFiles(@"C:\Users\StanleyGoldman\Dropbox\Camera Uploads");
            var images = files.Take(10).Select(s =>
            {
                var bitmapImage = imageFileSystemService.LoadImage(s);
                return new ImageView(s, bitmapImage);
            }).ToList();

            var applicationViewModel = new ApplicationViewModel
            {
                Images = images
            };

            DataContext = applicationViewModel;
        }
    }
}
