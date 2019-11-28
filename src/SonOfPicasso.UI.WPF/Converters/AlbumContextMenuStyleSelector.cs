using System.Windows;
using System.Windows.Controls;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.WPF.Converters
{
    class AlbumContextMenuStyleSelector : StyleSelector
    {
        public Style AlbumModelStyle { get; set; }
        public Style OtherMenuItemStyle { get; set; }
        public Style SeparatorMenuItemStyle { get; set; }

        public override Style SelectStyle(object item, DependencyObject container)
        {
            if (item is ImageContainerViewModel)
            {
                return AlbumModelStyle;
            }

            if (container is Separator)
            {
                return SeparatorMenuItemStyle;
            }

            return null;
        }
    }
}