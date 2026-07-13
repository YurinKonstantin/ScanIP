using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace ScanIPPro.Helpers
{
    /// <summary>
    /// Парсер IP-адресов и диапазонов
    /// </summary>
    public static class IpRangeParser
    {
        /// <summary>
        /// Парсит строку с IP-адресом или диапазоном
        /// </summary>
        /// <param name="input">Примеры: 192.168.1.1, 192.168.1.1-254</param>
        /// <returns>Список IP-адресов или null при ошибке</returns>
        public static List<string> Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            input = input.Trim();

            // Одиночный IP
            if (IPAddress.TryParse(input, out var singleIp))
            {
                return new List<string> { singleIp.ToString() };
            }

            // Диапазон: 192.168.1.1-254
            var rangeParts = input.Split('-');
            if (rangeParts.Length == 2 && IPAddress.TryParse(rangeParts[0], out var baseIp))
            {
                var baseBytes = baseIp.GetAddressBytes();

                if (int.TryParse(rangeParts[1], out int lastOctet) &&
                    lastOctet >= 1 && lastOctet <= 254)
                {
                    var result = new List<string>();
                    for (int i = 1; i <= lastOctet; i++)
                    {
                        var bytes = (byte[])baseBytes.Clone();
                        bytes[3] = (byte)i;
                        result.Add(new IPAddress(bytes).ToString());
                    }
                    return result;
                }
            }

            // Диапазон с маской: 192.168.1.0/24 (CIDR)
            if (input.Contains('/'))
            {
                return ParseCidr(input);
            }

            return null;
        }

        /// <summary>
        /// Парсинг CIDR-нотации (например, 192.168.1.0/24)
        /// </summary>
        private static List<string> ParseCidr(string input)
        {
            try
            {
                var parts = input.Split('/');
                if (parts.Length != 2)
                    return null;

                var baseIp = IPAddress.Parse(parts[0]);
                var maskBits = int.Parse(parts[1]);

                if (maskBits < 0 || maskBits > 32)
                    return null;

                var baseBytes = baseIp.GetAddressBytes();
                uint ip = (uint)(baseBytes[0] << 24 | baseBytes[1] << 16 |
                                 baseBytes[2] << 8 | baseBytes[3]);

                uint mask = maskBits == 0 ? 0 : ~(uint.MaxValue >> maskBits);
                uint startIp = ip & mask;
                uint endIp = startIp | ~mask;

                var result = new List<string>();
                for (uint i = startIp + 1; i < endIp; i++)
                {
                    var bytes = BitConverter.GetBytes(i);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(bytes);

                    result.Add(new IPAddress(bytes).ToString());
                }

                return result;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Проверка, является ли строка допустимым IP-адресом
        /// </summary>
        public static bool IsValidIp(string input)
        {
            return IPAddress.TryParse(input, out _);
        }

        /// <summary>
        /// Проверка, является ли строка допустимым диапазоном
        /// </summary>
        public static bool IsValidRange(string input)
        {
            return Parse(input) != null;
        }
    }
}
