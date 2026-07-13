using ScanIPPro.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ScanIPPro.Services
{
    /// <summary>
    /// Основной сервис сканирования сети
    /// </summary>
    public class NetworkScanner
    {
        private readonly PortScanner _portScanner;
        private readonly MacResolver _macResolver;
        private readonly SemaphoreSlim _semaphore = new(20); // Уменьшил с 100 до 20 для стабильности

        public NetworkScanner()
        {
            _portScanner = new PortScanner();
            _macResolver = new MacResolver();
        }

        /// <summary>
        /// Сканирование списка IP-адресов с прогрессом
        /// </summary>
        public async Task<List<ScanResult>> ScanIpRangeAsync(List<string> ipList,
            IProgress<ScanResult> progress, CancellationToken token)
        {
            var results = new List<ScanResult>();
            var tasks = new List<Task>();

            foreach (var ip in ipList)
            {
                token.ThrowIfCancellationRequested();
                await _semaphore.WaitAsync(token);

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var result = await ScanSingleIpAsync(ip, token);
                        if (result != null)
                        {
                            lock (results)
                            {
                                results.Add(result);
                                progress?.Report(result);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка сканирования {ip}: {ex.Message}");
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }, token));
            }

            await Task.WhenAll(tasks);

            // Сортировка результатов
            return results
                .OrderBy(r => IPAddress.Parse(r.IPAddress).GetAddressBytes()
                    .Select(b => b.ToString("D3"))
                    .Aggregate((a, b) => a + b))
                .ToList();
        }

        /// <summary>
        /// Сканирование одного IP-адреса
        /// </summary>
        private async Task<ScanResult> ScanSingleIpAsync(string ip, CancellationToken token)
        {
            var result = new ScanResult { IPAddress = ip };

            try
            {
                // 1. ICMP Ping
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(IPAddress.Parse(ip), 2000);
                token.ThrowIfCancellationRequested();

                if (reply.Status == IPStatus.Success)
                {
                    result.Status = "Online";
                    result.RoundtripTime = reply.RoundtripTime;

                    // 2. Получение имени хоста
                    try
                    {
                        var hostEntry = await Dns.GetHostEntryAsync(ip);
                        result.HostName = hostEntry.HostName;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка DNS для {ip}: {ex.Message}");
                    }

                    // 3. Получение MAC-адреса
                    try
                    {
                        result.MACAddress = await _macResolver.GetMacAddressAsync(ip);
                        result.Vendor = _macResolver.GetVendorByOUI(result.MACAddress);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка MAC для {ip}: {ex.Message}");
                    }

                    // 4. Сканирование портов (только популярные, чтобы не замедлять)
                    try
                    {
                        result.OpenPorts = await _portScanner.ScanCommonPortsAsync(ip, token);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка портов для {ip}: {ex.Message}");
                    }
                }
            }
            catch (PingException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ping ошибка для {ip}: {ex.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Общая ошибка для {ip}: {ex.Message}");
            }

            return result;
        }

        // Метод для сканирования портов по запросу
        public async Task<List<PortScanResult>> ScanPortsAsync(
            string ip,
            string portRange,
            IProgress<PortScanResult> progress,
            CancellationToken token = default)
        {
            var (startPort, endPort) = ParsePortRange(portRange);
            return await _portScanner.ScanPortsWithProgressAsync(
                ip, startPort, endPort, progress, 300, token);
        }

        // Вспомогательный метод для парсинга диапазона портов
        private (int start, int end) ParsePortRange(string input)
        {
            if (string.IsNullOrEmpty(input))
                return (1, 1024);

            var parts = input.Split('-');
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out int start) &&
                int.TryParse(parts[1], out int end))
            {
                return (Math.Max(1, start), Math.Min(65535, end));
            }

            if (int.TryParse(input, out int single))
                return (single, single);

            return (1, 1024);
        }
    }
}
