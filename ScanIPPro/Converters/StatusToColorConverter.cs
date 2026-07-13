using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Text;

namespace ScanIPPro.Converters
{
    /// <summary>
    /// Конвертер статуса устройства в цвет
    /// </summary>
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value?.ToString() == "Online")
            {
                // Зеленый для онлайн
                return new SolidColorBrush(Colors.Green);
            }
            else if (value?.ToString() == "Offline")
            {
                // Серый для офлайн
                return new SolidColorBrush(Colors.Gray);
            }

            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Конвертер статуса в иконку
    /// </summary>
    public class StatusToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value?.ToString() == "Online" ? "●" : "○";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
