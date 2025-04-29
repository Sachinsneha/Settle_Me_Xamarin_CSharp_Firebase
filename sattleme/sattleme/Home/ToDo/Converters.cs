using System;
using System.Globalization;
using Xamarin.Forms;

namespace sattleme.Home.ToDo
{
    public class BoolToColorConverter : IValueConverter
    {
        public Color TrueColor { get; set; } = Color.LightGreen;
        public Color FalseColor { get; set; } = Color.LightGray;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool flag)
                return flag ? TrueColor : FalseColor;
            return FalseColor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToTextColorConverter : IValueConverter
    {
        public Color TrueColor { get; set; } = Color.Black;
        public Color FalseColor { get; set; } = Color.Gray;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool flag)
                return flag ? TrueColor : FalseColor;
            return FalseColor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
