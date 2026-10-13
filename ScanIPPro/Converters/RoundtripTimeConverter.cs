using Microsoft.UI.Xaml.Data;
using ScanIPPro.Services;
using System;

namespace ScanIPPro.Converters
{
    public class RoundtripTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is long time)
                return LocalizationService.GetString("Unit.Milliseconds", time);

            return "—";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}
