using ScanIPPro.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Windows.ApplicationModel.Resources;
using Windows.Globalization;
using Windows.Storage;

namespace ScanIPPro.Services
{
    public static class LocalizationService
    {
        private static ResourceLoader _resourceLoader;
        private static string _currentLanguage = "en-US";

        public static event Action LanguageChanged;

        public static IReadOnlyList<string> SupportedLanguages { get; } = new[]
        {
            "en-US", "ru-RU", "zh-CN", "zh-TW", "es-ES", "pt-BR", "de-DE", "fr-FR", "ja-JP", "ko-KR"
        };

        public static string CurrentLanguage
        {
            get => _currentLanguage;
            private set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    LanguageChanged?.Invoke();
                }
            }
        }

        static LocalizationService()
        {
            var savedLang = ApplicationData.Current.LocalSettings.Values["AppLanguage"] as string;
            var culture = NormalizeCulture(string.IsNullOrEmpty(savedLang)
                ? CultureInfo.CurrentUICulture.Name
                : savedLang);
            ApplyLanguage(culture, persist: false);
        }

        public static void SetLanguage(string cultureName)
        {
            ApplyLanguage(NormalizeCulture(cultureName), persist: true);
        }

        public static string GetString(string key)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            try
            {
                _resourceLoader ??= CreateLoader();

                // In WinUI 3 / MRT Core, dots in .resw names become path separators.
                // Resource name "Scan.Start" is looked up as "Scan/Start".
                var resourceId = key.Replace('.', '/');
                var value = _resourceLoader.GetString(resourceId);

                if (string.IsNullOrEmpty(value))
                {
                    // Fallback: try the original key in case the PRI kept dots.
                    value = _resourceLoader.GetString(key);
                }

                return string.IsNullOrEmpty(value) ? key : value;
            }
            catch
            {
                return key;
            }
        }

        public static string GetString(string key, params object[] args)
        {
            var format = GetString(key);
            try
            {
                return string.Format(CultureInfo.CurrentCulture, format, args);
            }
            catch
            {
                return format;
            }
        }

        public static string GetStatusDisplay(string status) => status switch
        {
            "Online" => GetString("Status.Online"),
            "Offline" => GetString("Status.Offline"),
            _ => status ?? string.Empty
        };

        public static string GetPortStatusDisplay(PortStatus status) => status switch
        {
            PortStatus.Open => GetString("PortStatus.Open"),
            PortStatus.Closed => GetString("PortStatus.Closed"),
            PortStatus.Filtered => GetString("PortStatus.Filtered"),
            _ => GetString("PortStatus.Unknown")
        };

        public static string GetLanguageDisplayName(string cultureName)
        {
            try
            {
                return new CultureInfo(cultureName).NativeName;
            }
            catch
            {
                return cultureName;
            }
        }

        private static ResourceLoader CreateLoader()
        {
            // Default map for Strings/*/Resources.resw
            return new ResourceLoader();
        }

        private static void ApplyLanguage(string cultureName, bool persist)
        {
            if (!SupportedLanguages.Contains(cultureName))
                cultureName = "en-US";

            CultureInfo.CurrentCulture = new CultureInfo(cultureName);
            CultureInfo.CurrentUICulture = new CultureInfo(cultureName);
            ApplicationLanguages.PrimaryLanguageOverride = cultureName;

            // Recreate loader after language override so lookups use the new language.
            _resourceLoader = CreateLoader();
            CurrentLanguage = cultureName;

            if (persist)
            {
                ApplicationData.Current.LocalSettings.Values["AppLanguage"] = cultureName;
                if (AppSettingsService.Current != null)
                {
                    AppSettingsService.Current.Language = cultureName;
                    AppSettingsService.Save();
                }
            }
        }

        private static string NormalizeCulture(string cultureName)
        {
            if (string.IsNullOrWhiteSpace(cultureName))
                return "en-US";

            if (SupportedLanguages.Contains(cultureName))
                return cultureName;

            var neutral = cultureName.Split('-')[0].ToLowerInvariant();
            return neutral switch
            {
                "en" => "en-US",
                "ru" => "ru-RU",
                "zh" when cultureName.Contains("TW", StringComparison.OrdinalIgnoreCase)
                    || cultureName.Contains("Hant", StringComparison.OrdinalIgnoreCase) => "zh-TW",
                "zh" => "zh-CN",
                "es" => "es-ES",
                "pt" => "pt-BR",
                "de" => "de-DE",
                "fr" => "fr-FR",
                "ja" => "ja-JP",
                "ko" => "ko-KR",
                _ => SupportedLanguages.FirstOrDefault(l =>
                         l.StartsWith(neutral, StringComparison.OrdinalIgnoreCase)) ?? "en-US"
            };
        }
    }
}
