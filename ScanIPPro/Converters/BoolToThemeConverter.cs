using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace ScanIPPro.Converters
{
    public class BoolToThemeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isDark)
            {
                return isDark ? ElementTheme.Dark : ElementTheme.Light;
            }
            return ElementTheme.Default;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is ElementTheme theme)
            {
                return theme == ElementTheme.Dark;
            }
            return false;
        }
    }
}
