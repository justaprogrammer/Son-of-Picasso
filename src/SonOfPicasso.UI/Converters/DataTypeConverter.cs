using System;
using System.Globalization;
using System.Windows.Data;

namespace SonOfPicasso.UI.Converters
{
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
