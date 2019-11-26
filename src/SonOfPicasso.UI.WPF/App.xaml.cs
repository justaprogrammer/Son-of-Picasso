using System.Windows;
using Akavache;
using Autofac;
using Microsoft.EntityFrameworkCore;
using SonOfPicasso.Data.Repository;
using SonOfPicasso.UI.ViewModels;
using SonOfPicasso.UI.WPF.Windows;

namespace SonOfPicasso.UI.WPF
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string ApplicationName = "SonOfPicasso";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var container = AppConfiguration.Configure(GetType().Assembly, App.ApplicationName);

            var dataContext = container.Resolve<DataContext>();
            dataContext.Database.Migrate();

            var mainWindow = container.Resolve<MainWindow>();

            mainWindow.ViewModel = container.Resolve<ApplicationViewModel>();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            BlobCache.Shutdown().Wait();

            base.OnExit(e);
        }
    }
}