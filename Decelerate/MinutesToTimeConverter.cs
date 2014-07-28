using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Decelerate
{
    public class MinutesToTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            object result = DependencyProperty.UnsetValue;

            if (value is double)
            {
                var timeSpan = TimeSpan.FromMinutes(Math.Round((double)value));

                result = timeSpan.Hours + "h " + timeSpan.Minutes + "m";
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}