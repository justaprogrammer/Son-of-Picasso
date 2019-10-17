using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.Converters
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

    public class DataTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            if (value != null)
            {
                return value.GetType();
            }

            return new object();
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
