using System.Linq;
using System.Windows;
using PicasaReboot.Core;
using PicasaReboot.Core.Logging;
using PicasaReboot.Windows.ViewModels;
using Serilog;

namespace PicasaReboot.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static ILogger Log { get; } = LogManager.ForContext<MainWindow>();

        public MainWindow()
        {
            Log.Debug("Initializing");

            InitializeComponent();

            var applicationViewModel = new DirectoryViewModel(new ImageService());
            DataContext = applicationViewModel;

            Log.Debug("Initialized");

            applicationViewModel.Name = @"C:\Users\Spade\Desktop\New folder";
        }
    }
}
