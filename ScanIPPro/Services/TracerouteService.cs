using ScanIPPro.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace ScanIPPro.Services
{
    public class TracerouteService
    {
        public async Task<List<TracerouteHop>> TraceAsync(string host, int maxHops, CancellationToken token)
        {
            var hops = new List<TracerouteHop>();
            var target = host.Trim();

            for (int ttl = 1; ttl <= maxHops; ttl++)
            {
                token.ThrowIfCancellationRequested();

                var hop = new TracerouteHop { Hop = ttl };
                try
                {
                    using var ping = new Ping();
                    var options = new PingOptions(ttl, true);
                    var reply = await ping.SendPingAsync(target, 4000, Array.Empty<byte>(), options);

                    hop.Address = reply.Address?.ToString() ?? "*";
                    hop.RoundtripTime = reply.RoundtripTime;
                    hop.Status = reply.Status.ToString();

                    if (reply.Status == IPStatus.Success)
                    {
                        try
                        {
                            var entry = await Dns.GetHostEntryAsync(reply.Address);
                            hop.HostName = entry.HostName;
                        }
                        catch
                        {
                            hop.HostName = "—";
                        }

                        hops.Add(hop);
                        break;
                    }

                    if (reply.Status == IPStatus.TtlExpired || reply.Status == IPStatus.TimedOut)
                    {
                        hop.Address = reply.Address?.ToString() ?? "*";
                        hops.Add(hop);
                        continue;
                    }

                    hops.Add(hop);
                }
                catch (Exception ex)
                {
                    hop.Status = ex.Message;
                    hops.Add(hop);
                }
            }

            return hops;
        }
    }
}
