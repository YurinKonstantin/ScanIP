using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace ScanIPPro.Services
{
    public static class NetworkInfoService
    {
        public static string GetSuggestedScanRange()
        {
            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.OperationalStatus != OperationalStatus.Up)
                    continue;

                if (networkInterface.NetworkInterfaceType is not (NetworkInterfaceType.Ethernet
                    or NetworkInterfaceType.Wireless80211 or NetworkInterfaceType.GigabitEthernet))
                    continue;

                var properties = networkInterface.GetIPProperties();
                var address = properties.UnicastAddresses
                    .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork
                                         && !IPAddress.IsLoopback(a.Address));

                if (address?.IPv4Mask == null)
                    continue;

                var ipBytes = address.Address.GetAddressBytes();
                var maskBytes = address.IPv4Mask.GetAddressBytes();
                var networkBytes = new byte[4];
                var broadcastBytes = new byte[4];

                for (int i = 0; i < 4; i++)
                {
                    networkBytes[i] = (byte)(ipBytes[i] & maskBytes[i]);
                    broadcastBytes[i] = (byte)(networkBytes[i] | ~maskBytes[i]);
                }

                if (broadcastBytes[3] <= 1)
                    continue;

                return $"{networkBytes[0]}.{networkBytes[1]}.{networkBytes[2]}.1-{broadcastBytes[3] - 1}";
            }

            return "192.168.1.1-254";
        }
    }
}
