using System.Windows;
using SonOfPicasso.Core;
using SonOfPicasso.Core.Logging;
using SonOfPicasso.Windows.ViewModels;

namespace SonOfPicasso.Windows
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

            var folder = @"C:\Users\Spade\Desktop\New folder";
            var applicationViewModel = new DirectoryViewModel(new ImageService(), folder);
            DataContext = applicationViewModel;

            Log.Debug("Initialized");

        }
    }
}
