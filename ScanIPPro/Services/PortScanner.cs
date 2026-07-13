using ScanIPPro.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ScanIPPro.Services
{
    /// <summary>
    /// Сервис сканирования портов
    /// </summary>
    public class PortScanner
    {
        private readonly int[] _commonPorts = new[]
        {
            21, 22, 23, 25, 53, 80, 110, 135, 139, 143,
            443, 445, 993, 995, 1723, 3306, 3389, 5432, 8080, 8443
        };

        // Оригинальный метод для сканирования популярных портов (без изменений)
        public async Task<string> ScanCommonPortsAsync(string ip, CancellationToken token)
        {
            var openPorts = new List<int>();
            var tasks = new List<Task>();

            foreach (var port in _commonPorts)
            {
                token.ThrowIfCancellationRequested();
                var currentPort = port;

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        using var client = new TcpClient();
                        var connectTask = client.ConnectAsync(ip, currentPort);

                        if (await Task.WhenAny(connectTask, Task.Delay(300, token)) == connectTask)
                        {
                            if (client.Connected)
                            {
                                lock (openPorts)
                                    openPorts.Add(currentPort);
                            }
                        }
                    }
                    catch (SocketException) { /* Игнорируем */ }
                    catch (ObjectDisposedException) { /* Игнорируем */ }
                    catch { /* Игнорируем */ }
                }, token));
            }

            await Task.WhenAll(tasks);

            if (!openPorts.Any())
                return "Нет открытых";

            return string.Join(", ", openPorts
                .OrderBy(p => p)
                .Select(p => $"{p}({GetServiceName(p)})"));
        }

        // Метод для сканирования диапазона портов с прогрессом
        public async Task<List<PortScanResult>> ScanPortsWithProgressAsync(
            string ip,
            int startPort,
            int endPort,
            IProgress<PortScanResult> progress,
            int timeout = 300,
            CancellationToken token = default)
        {
            var results = new List<PortScanResult>();
            var tasks = new List<Task>();
            var semaphore = new SemaphoreSlim(50); // Ограничиваем количество параллельных подключений

            for (int port = startPort; port <= endPort; port++)
            {
                token.ThrowIfCancellationRequested();
                await semaphore.WaitAsync(token);
                var currentPort = port;

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var result = await ScanPortDetailsAsync(ip, currentPort, timeout, token);
                        if (result != null)
                        {
                            lock (results)
                            {
                                results.Add(result);
                                progress?.Report(result);
                            }
                        }
                    }
                    catch (OperationCanceledException) { /* Отмена */ }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка сканирования порта {currentPort}: {ex.Message}");
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, token));
            }

            await Task.WhenAll(tasks);
            return results.OrderBy(r => r.Port).ToList();
        }

        // Метод для детального сканирования одного порта
        private async Task<PortScanResult> ScanPortDetailsAsync(
            string ip,
            int port,
            int timeout,
            CancellationToken token)
        {
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(ip, port);

                if (await Task.WhenAny(connectTask, Task.Delay(timeout, token)) == connectTask)
                {
                    if (client.Connected)
                    {
                        var result = new PortScanResult
                        {
                            Port = port,
                            Protocol = "TCP",
                            Status = PortStatus.Open,
                            Service = GetServiceName(port)
                        };

                        // Попытка получить баннер
                        result.Banner = await GetBannerAsync(client, timeout);
                        return result;
                    }
                }
            }
            catch (SocketException) { /* Игнорируем */ }
            catch (ObjectDisposedException) { /* Игнорируем */ }
            catch (OperationCanceledException) { /* Отмена */ }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка детального сканирования порта {port}: {ex.Message}");
            }
            return null;
        }

        // Метод для получения баннера
        private async Task<string> GetBannerAsync(TcpClient client, int timeout)
        {
            try
            {
                if (client.Connected)
                {
                    using var stream = client.GetStream();
                    if (stream.CanRead)
                    {
                        var buffer = new byte[256];
                        var readTask = stream.ReadAsync(buffer, 0, buffer.Length);

                        if (await Task.WhenAny(readTask, Task.Delay(timeout)) == readTask)
                        {
                            var bytesRead = await readTask;
                            if (bytesRead > 0)
                            {
                                return Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка получения баннера: {ex.Message}");
            }
            return null;
        }

        // Метод для определения службы по порту
        private string GetServiceName(int port)
        {
            return port switch
            {
                21 => "FTP",
                22 => "SSH",
                23 => "Telnet",
                25 => "SMTP",
                53 => "DNS",
                69 => "TFTP",
                80 => "HTTP",
                110 => "POP3",
                111 => "RPC",
                135 => "MS RPC",
                137 => "NetBIOS-NS",
                138 => "NetBIOS-DGM",
                139 => "NetBIOS-SSN",
                143 => "IMAP",
                161 => "SNMP",
                162 => "SNMPTRAP",
                389 => "LDAP",
                443 => "HTTPS",
                445 => "SMB",
                465 => "SMTPS",
                514 => "Syslog",
                515 => "LPD",
                543 => "Kerberos",
                587 => "SMTP",
                636 => "LDAPS",
                873 => "Rsync",
                990 => "FTPS",
                993 => "IMAPS",
                995 => "POP3S",
                1080 => "SOCKS",
                1433 => "MSSQL",
                1434 => "MSSQL-UDP",
                1521 => "Oracle",
                1723 => "PPTP",
                1812 => "RADIUS",
                1813 => "RADIUS-ACCT",
                2049 => "NFS",
                2082 => "cPanel",
                2083 => "cPanel-SSL",
                2086 => "WHM",
                2087 => "WHM-SSL",
                2095 => "Webmail",
                2096 => "Webmail-SSL",
                2181 => "ZooKeeper",
                2375 => "Docker",
                2376 => "Docker-SSL",
                2377 => "Docker-Swarm",
                2483 => "Oracle",
                2484 => "Oracle-SSL",
                3000 => "Grafana/Node.js",
                3306 => "MySQL",
                3389 => "RDP",
                3690 => "SVN",
                4369 => "Erlang",
                5000 => "Flask/UPnP",
                5001 => "UPnP-SSL",
                5007 => "WAF",
                5432 => "PostgreSQL",
                5672 => "AMQP",
                5671 => "AMQP-SSL",
                5900 => "VNC",
                5901 => "VNC-1",
                5902 => "VNC-2",
                5903 => "VNC-3",
                6379 => "Redis",
                6443 => "Kubernetes-API",
                6667 => "IRC",
                7000 => "Cassandra",
                7001 => "WebLogic",
                8080 => "HTTP-Alt",
                8081 => "HTTP-Proxy",
                8086 => "InfluxDB",
                8087 => "HTTP-Alt",
                8088 => "HTTP-Alt",
                8090 => "HTTP-Alt",
                8096 => "Jellyfin",
                8140 => "Puppet",
                8443 => "HTTPS-Alt",
                8883 => "MQTT-SSL",
                8888 => "Jupyter",
                9000 => "PHP-FPM",
                9001 => "Supervisor",
                9042 => "Cassandra",
                9090 => "Prometheus",
                9091 => "Prometheus",
                9092 => "Kafka",
                9100 => "NodeExporter",
                9200 => "Elasticsearch",
                9300 => "Elasticsearch",
                9418 => "Git",
                9999 => "Zabbix",
                11211 => "Memcached",
                11215 => "Memcached",
                15672 => "RabbitMQ",
                16080 => "MaNGOS",
                16225 => "MongoDB",
                27017 => "MongoDB",
                27018 => "MongoDB",
                27019 => "MongoDB",
                28015 => "RethinkDB",
                29015 => "RethinkDB",
                50000 => "SAP",
                50001 => "SAP",
                50010 => "SAP",
                50050 => "SAP",
                _ => "Неизвестно"
            };
        }

        // Сканирование указанного диапазона портов
        public async Task<List<int>> ScanPortRangeAsync(string ip, int startPort, int endPort,
            int timeout = 300, CancellationToken token = default)
        {
            var openPorts = new List<int>();
            var tasks = new List<Task>();
            var semaphore = new SemaphoreSlim(50);

            for (int port = startPort; port <= endPort; port++)
            {
                token.ThrowIfCancellationRequested();
                await semaphore.WaitAsync(token);
                var currentPort = port;

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        using var client = new TcpClient();
                        var connectTask = client.ConnectAsync(ip, currentPort);

                        if (await Task.WhenAny(connectTask, Task.Delay(timeout, token)) == connectTask)
                        {
                            if (client.Connected)
                            {
                                lock (openPorts)
                                    openPorts.Add(currentPort);
                            }
                        }
                    }
                    catch (SocketException) { /* Игнорируем */ }
                    catch (ObjectDisposedException) { /* Игнорируем */ }
                    finally
                    {
                        semaphore.Release();
                    }
                }, token));
            }

            await Task.WhenAll(tasks);
            return openPorts.OrderBy(p => p).ToList();
        }
    }
}
