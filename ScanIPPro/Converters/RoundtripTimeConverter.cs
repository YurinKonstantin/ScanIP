using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace ScanIPPro.Converters
{
    /// <summary>
    /// Конвертер для форматирования времени отклика
    /// </summary>
    public class RoundtripTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is long time)
            {
                return $"{time} мс";
            }
            return "—";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
