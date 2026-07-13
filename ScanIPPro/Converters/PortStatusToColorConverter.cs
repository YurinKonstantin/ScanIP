using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using ScanIPPro.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ScanIPPro.Converters
{
    public class PortStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is PortStatus status)
            {
                return status switch
                {
                    PortStatus.Open => new SolidColorBrush(Colors.Green),
                    PortStatus.Closed => new SolidColorBrush(Colors.Gray),
                    PortStatus.Filtered => new SolidColorBrush(Colors.Orange),
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
