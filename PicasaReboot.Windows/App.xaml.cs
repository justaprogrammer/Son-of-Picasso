using System.IO;
using System.Reflection;
using System.Windows;
using SonOfPicasso.Core;

namespace SonOfPicasso.Windows
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static App()
        {
            var location = Assembly.GetExecutingAssembly().Location;
            var directoryName = Path.GetDirectoryName(location);

            Guard.NotNull(directoryName, nameof(directoryName));

            var applicationLog = Path.Combine(directoryName, "application.log");
            if (File.Exists(applicationLog))
            {
                File.Delete(applicationLog);
            }
        }
    }
}
