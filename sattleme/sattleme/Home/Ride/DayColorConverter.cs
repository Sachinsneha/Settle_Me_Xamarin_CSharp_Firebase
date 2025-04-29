using System;
using System.Globalization;
using Xamarin.Forms;

namespace sattleme.Home.Ride
{
    public class DayColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
           
            if (value is System.Collections.IList days && parameter is string day)
            {
                foreach (var d in days)
                {
                    if (string.Equals(d.ToString(), day, StringComparison.OrdinalIgnoreCase))
                        return Color.Green;  
                }
            }
            return Color.Green;  
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
