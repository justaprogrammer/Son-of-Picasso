using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using PicasaReboot.Core;

namespace PicasaReboot.Windows
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
