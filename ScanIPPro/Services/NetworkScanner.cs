using ScanIPPro.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace ScanIPPro.Services
{
    public class ScanOptions
    {
        public bool OnlineOnly { get; set; } = true;
        public int PingTimeoutMs { get; set; } = 2000;
        public int Concurrency { get; set; } = 20;
    }

    /// <summary>
    /// Основной сервис сканирования сети
    /// </summary>
    public class NetworkScanner
    {
        private readonly PortScanner _portScanner;
        private readonly MacResolver _macResolver;

        public NetworkScanner()
        {
            _portScanner = new PortScanner();
            _macResolver = new MacResolver();
        }

        public async Task<List<ScanResult>> ScanIpRangeAsync(
            List<string> ipList,
            IProgress<ScanResult> progress,
            IProgress<(int completed, int total)> scanProgress,
            ScanOptions options,
            CancellationToken token)
        {
            var results = new List<ScanResult>();
            var tasks = new List<Task>();
            var semaphore = new SemaphoreSlim(Math.Max(1, options?.Concurrency ?? 20));
            var total = ipList.Count;
            var completed = 0;

            foreach (var ip in ipList)
            {
                token.ThrowIfCancellationRequested();
                await semaphore.WaitAsync(token);

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var result = await ScanSingleIpAsync(ip, options, token);
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
                        System.Diagnostics.Debug.WriteLine($"Scan error {ip}: {ex.Message}");
                    }
                    finally
                    {
                        var done = Interlocked.Increment(ref completed);
                        scanProgress?.Report((done, total));
                        semaphore.Release();
                    }
                }, token));
            }

            await Task.WhenAll(tasks);

            return results
                .OrderBy(r => IPAddress.Parse(r.IPAddress).GetAddressBytes()
                    .Select(b => b.ToString("D3"))
                    .Aggregate((a, b) => a + b))
                .ToList();
        }

        private async Task<ScanResult> ScanSingleIpAsync(string ip, ScanOptions options, CancellationToken token)
        {
            options ??= new ScanOptions();
            var result = new ScanResult { IPAddress = ip };

            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(IPAddress.Parse(ip), options.PingTimeoutMs);
                token.ThrowIfCancellationRequested();

                if (reply.Status != IPStatus.Success)
                {
                    result.Status = ScanResult.StatusOffline;
                    return options.OnlineOnly ? null : result;
                }

                result.Status = ScanResult.StatusOnline;
                result.RoundtripTime = reply.RoundtripTime;

                try
                {
                    var hostEntry = await Dns.GetHostEntryAsync(ip);
                    result.HostName = hostEntry.HostName;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"DNS error {ip}: {ex.Message}");
                }

                try
                {
                    result.MACAddress = await _macResolver.GetMacAddressAsync(ip);
                    result.Vendor = _macResolver.GetVendorByOUI(result.MACAddress);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"MAC error {ip}: {ex.Message}");
                }

                try
                {
                    result.OpenPorts = await _portScanner.ScanCommonPortsAsync(ip, token);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Port error {ip}: {ex.Message}");
                }
            }
            catch (PingException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ping error {ip}: {ex.Message}");
                result.Status = ScanResult.StatusOffline;
                return options.OnlineOnly ? null : result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"General error {ip}: {ex.Message}");
                result.Status = ScanResult.StatusOffline;
                return options.OnlineOnly ? null : result;
            }

            return result;
        }

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

        private static (int start, int end) ParsePortRange(string input)
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
