using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.Avalonia.Windows
{
    public class MainWindow : ReactiveWindow<ApplicationViewModel>
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}