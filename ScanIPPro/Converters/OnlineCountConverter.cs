using Microsoft.UI.Xaml.Data;
using ScanIPPro.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScanIPPro.Converters
{
    /// <summary>
    /// Конвертер для подсчета онлайн устройств
    /// </summary>
    public class OnlineCountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is IEnumerable collection)
            {
                var onlineCount = collection
                    .OfType<ScanResult>()
                    .Count(r => r.IsOnline);

                return onlineCount;
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
