using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace OrderManagerPlus.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                switch (status)
                {
                    case "Не виконано":
                        return new SolidColorBrush(Color.FromArgb(191, 255, 0, 0));
                    case "Частково виконано":
                        return new SolidColorBrush(Color.FromArgb(191, 255, 255, 0));
                    case "Виконано/не оплачено":
                        return new SolidColorBrush(Color.FromArgb(191, 0, 0, 255));
                    case "Виконано і оплачено":
                        return new SolidColorBrush(Color.FromArgb(191, 0, 128, 0));
                }
            }

            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}