using Avalonia;
using Avalonia.Markup.Xaml;

namespace SonOfPicasso.UI.Avalonia
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
   }
}