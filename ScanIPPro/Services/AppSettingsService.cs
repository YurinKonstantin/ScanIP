using ScanIPPro.Models;
using System;
using Windows.Storage;

namespace ScanIPPro.Services
{
    public static class AppSettingsService
    {
        private const string SettingsKey = "AppSettings";

        public static AppSettings Current { get; private set; } = new();

        public static event Action SettingsChanged;

        public static void Load()
        {
            if (ApplicationData.Current.LocalSettings.Values[SettingsKey] is string json
                && !string.IsNullOrEmpty(json))
            {
                try
                {
                    Current = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                catch
                {
                    Current = new AppSettings();
                }
            }

            if (ApplicationData.Current.LocalSettings.Values["AppLanguage"] is string legacyLang
                && !string.IsNullOrEmpty(legacyLang))
            {
                Current.Language = legacyLang;
            }

            if (ApplicationData.Current.LocalSettings.Values["Theme"] is string legacyTheme
                && !string.IsNullOrEmpty(legacyTheme))
            {
                Current.Theme = legacyTheme;
            }
        }

        public static void Save()
        {
            var json = System.Text.Json.JsonSerializer.Serialize(Current);
            ApplicationData.Current.LocalSettings.Values[SettingsKey] = json;
            ApplicationData.Current.LocalSettings.Values["AppLanguage"] = Current.Language;
            ApplicationData.Current.LocalSettings.Values["Theme"] = Current.Theme;
            SettingsChanged?.Invoke();
        }
    }
}
