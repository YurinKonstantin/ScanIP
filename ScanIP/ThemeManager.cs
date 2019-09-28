using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;

namespace ScanIP
{
    public sealed class ThemeManager : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
                    => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        public const string DarkThemePath = "ms-appx:///Themes/Theme.Dark.xaml";
        public const string LightThemePath = "ms-appx:///Themes/Theme.Light.xaml";
        private ResourceDictionary _currentThemeDictionary;
        public string CurrentTheme { get; private set; }

        public Brush BackgroundBrush => _currentThemeDictionary[nameof(BackgroundBrush)] as Brush;
        public Brush ChromeBrush => _currentThemeDictionary[nameof(ChromeBrush)] as Brush;
        public Brush ForegroundBrush => _currentThemeDictionary[nameof(ForegroundBrush)] as Brush;
        private void RaisePropertyChanged()
        {
            OnPropertyChanged(nameof(BackgroundBrush));
            OnPropertyChanged(nameof(ChromeBrush));
            OnPropertyChanged(nameof(ForegroundBrush));
            OnPropertyChanged(nameof(CurrentTheme));
        }
        public void LoadTheme(string path)
        {
            _currentThemeDictionary = new ResourceDictionary();
            App.LoadComponent(_currentThemeDictionary, new Uri(path));
            CurrentTheme = Path.GetFileNameWithoutExtension(path);

            RaisePropertyChanged();
        }

        public async Task LoadThemeFromFile(StorageFile file)
        {
            string xaml = await FileIO.ReadTextAsync(file);
            _currentThemeDictionary = XamlReader.Load(xaml) as ResourceDictionary;
            CurrentTheme = Path.GetFileNameWithoutExtension(file.Path);

            RaisePropertyChanged();
        }
        public ThemeManager()
        {
            LoadTheme(DarkThemePath);
        }
    }
}
