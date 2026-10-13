using Microsoft.UI.Xaml;
using ScanIPPro.Services;

namespace ScanIPPro
{
    public partial class App : Application
    {
        public static Window MainWindow { get; private set; }

        public App()
        {
            AppSettingsService.Load();
            FavoritesService.Load();
            LocalizationService.SetLanguage(AppSettingsService.Current.Language);
            InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            MainWindow = new MainWindow();
            MainWindow.Activate();
        }
    }
}
