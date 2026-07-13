using ScanIPPro.Native;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ScanIPPro.Services
{
    /// <summary>
    /// Сервис для получения MAC-адресов и определения вендоров
    /// </summary>
    public class MacResolver
    {
        private readonly Dictionary<string, string> _ouiDatabase = new()
        {
            // Производители сетевого оборудования
            { "00:1A:2B", "Intel" },
            { "00:1B:21", "Apple" },
            { "00:1E:C2", "Samsung" },
            { "00:0C:29", "VMware" },
            { "00:50:56", "VMware" },
            { "00:1C:42", "Cisco" },
            { "00:1D:60", "Nokia" },
            { "00:1E:65", "Dell" },
            { "00:1A:6B", "HP" },
            { "B8:27:EB", "Raspberry Pi" },
            { "00:21:5A", "Microsoft" },
            { "00:15:5D", "Hyper-V" },
            { "00:16:3E", "Xen" },
            { "00:1C:BF", "Sony" },
            { "00:1D:D8", "LG" },
            { "00:0F:B0", "Huawei" },
            { "00:1A:11", "ZTE" },
            { "00:26:86", "TP-Link" },
            { "00:18:4D", "D-Link" },
            { "00:1C:F0", "Netgear" },
            { "00:1D:AA", "Asus" },
            { "00:1E:8C", "Acer" },
            { "00:1F:33", "Atheros" },
            { "00:24:21", "Broadcom" },
            { "00:25:9C", "Realtek" }
        };

        /// <summary>
        /// Получение MAC-адреса по IP через ARP
        /// </summary>
        public async Task<string> GetMacAddressAsync(string ip)
        {
            try
            {
                // Проверяем, что IP в локальной сети
                if (!IsLocalIp(ip))
                    return null;
            
            return await Task.Run(() =>
            {
                try
                {
                    var ipAddr = IPAddress.Parse(ip);
                    byte[] macAddr = new byte[6];
                    int macLen = macAddr.Length;

                    uint destIp = NativeMethods.IpToUint(ip);
                    int result = NativeMethods.SendARP(destIp, 0, macAddr, ref macLen);

                    if (result == 0 && macLen == 6)
                        return NativeMethods.MacBytesToString(macAddr, macLen);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка получения MAC для {ip}: {ex.Message}");
                }
                return null;
            });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Общая ошибка MAC для {ip}: {ex.Message}");
                return null;
            }
        }

        private bool IsLocalIp(string ip)
        {
            try
            {
                var address = IPAddress.Parse(ip);
                var bytes = address.GetAddressBytes();

                // Проверка на локальные диапазоны
                if (bytes[0] == 10) return true; // 10.0.0.0/8
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true; // 172.16.0.0/12
                if (bytes[0] == 192 && bytes[1] == 168) return true; // 192.168.0.0/16
                if (bytes[0] == 127) return true; // localhost

                return false;
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// Определение производителя по OUI (первые 3 байта MAC)
        /// </summary>
        public string GetVendorByOUI(string macAddress)
        {
            if (string.IsNullOrEmpty(macAddress) || macAddress.Length < 8)
                return "Неизвестный";

            var oui = macAddress.Substring(0, 8).ToUpper();
            return _ouiDatabase.TryGetValue(oui, out var vendor) ? vendor : "Неизвестный";
        }

        /// <summary>
        /// Добавление/обновление записи о производителе
        /// </summary>
        public void AddVendor(string oui, string vendorName)
        {
            if (!string.IsNullOrEmpty(oui) && !string.IsNullOrEmpty(vendorName))
                _ouiDatabase[oui.ToUpper()] = vendorName;
        }
    }
}
