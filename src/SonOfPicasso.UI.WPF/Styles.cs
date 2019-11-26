using System.Collections.Concurrent;
using System.Windows;

namespace SonOfPicasso.UI.WPF
{
    public static class Styles
    {
        private static readonly ConcurrentDictionary<string, Style> LoadedStyles =
            new ConcurrentDictionary<string, Style>();

        public static Style TextBoxError => GetStyle("TextBoxError");

        private static Style GetStyle(string resourceKey) => LoadedStyles.GetOrAdd(resourceKey, s => (Style) Application.Current.FindResource(resourceKey));
    }
}