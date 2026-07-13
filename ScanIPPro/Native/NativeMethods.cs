using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ScanIPPro.Native
{
    /// <summary>
    /// P/Invoke методы для работы с системными API
    /// </summary>
    public static class NativeMethods
    {
        /// <summary>
        /// Отправляет ARP-запрос для получения MAC-адреса по IP
        /// </summary>
        /// <param name="destIp">IP-адрес назначения (в сетевом порядке байт)</param>
        /// <param name="srcIp">IP-адрес источника (обычно 0)</param>
        /// <param name="pMacAddr">Буфер для MAC-адреса (6 байт)</param>
        /// <param name="pMacAddrLen">Размер буфера (вход/выход)</param>
        /// <returns>0 в случае успеха, иначе код ошибки</returns>
        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        public static extern int SendARP(uint destIp, uint srcIp, byte[] pMacAddr, ref int pMacAddrLen);

        /// <summary>
        /// Получение информации о сетевом интерфейсе
        /// </summary>
        [DllImport("iphlpapi.dll", CharSet = CharSet.Auto)]
        public static extern int GetBestInterface(uint destAddr, out int bestIfIndex);

        /// <summary>
        /// Преобразование IP-адреса из строки в uint (сетевой порядок)
        /// </summary>
        public static uint IpToUint(string ipAddress)
        {
            var parts = ipAddress.Split('.');
            if (parts.Length != 4)
                throw new ArgumentException("Неверный формат IP-адреса");

            return (uint)(byte.Parse(parts[0]) << 24 |
                         byte.Parse(parts[1]) << 16 |
                         byte.Parse(parts[2]) << 8 |
                         byte.Parse(parts[3]));
        }

        /// <summary>
        /// Преобразование MAC-адреса из байтов в строку
        /// </summary>
        public static string MacBytesToString(byte[] macBytes, int length)
        {
            if (macBytes == null || length < 6)
                return null;

            return string.Join(":", macBytes.Take(6).Select(b => b.ToString("X2")));
        }
    }
}
