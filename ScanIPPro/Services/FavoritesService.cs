using ScanIPPro.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Windows.Storage;

namespace ScanIPPro.Services
{
    public static class FavoritesService
    {
        private const string Key = "DeviceFavorites";
        private static List<DeviceFavorite> _favorites = new();

        public static IReadOnlyList<DeviceFavorite> Favorites => _favorites;

        public static event Action FavoritesChanged;

        public static void Load()
        {
            if (ApplicationData.Current.LocalSettings.Values[Key] is string json
                && !string.IsNullOrEmpty(json))
            {
                try
                {
                    _favorites = JsonSerializer.Deserialize<List<DeviceFavorite>>(json) ?? new();
                }
                catch
                {
                    _favorites = new();
                }
            }
        }

        public static void Save()
        {
            ApplicationData.Current.LocalSettings.Values[Key] =
                JsonSerializer.Serialize(_favorites);
            FavoritesChanged?.Invoke();
        }

        public static bool IsFavorite(string ip) =>
            _favorites.Any(f => f.IPAddress == ip);

        public static void Toggle(ScanResult device, string comment = "")
        {
            var existing = _favorites.FirstOrDefault(f => f.IPAddress == device.IPAddress);
            if (existing != null)
            {
                _favorites.Remove(existing);
            }
            else
            {
                _favorites.Add(new DeviceFavorite
                {
                    IPAddress = device.IPAddress,
                    HostName = device.HostName,
                    MACAddress = device.MACAddress,
                    Comment = comment
                });
            }

            Save();
        }
    }
}
