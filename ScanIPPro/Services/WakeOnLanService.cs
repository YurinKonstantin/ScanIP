using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ScanIPPro.Services
{
    public static class WakeOnLanService
    {
        public static async Task<bool> SendAsync(string macAddress)
        {
            var mac = ParseMac(macAddress);
            if (mac == null)
                return false;

            var packet = new byte[102];
            for (int i = 0; i < 6; i++)
                packet[i] = 0xFF;

            for (int i = 1; i <= 16; i++)
                Buffer.BlockCopy(mac, 0, packet, i * 6, 6);

            try
            {
                using var client = new UdpClient();
                client.EnableBroadcast = true;
                await client.SendAsync(packet, packet.Length, new IPEndPoint(IPAddress.Broadcast, 9));
                await client.SendAsync(packet, packet.Length, new IPEndPoint(IPAddress.Broadcast, 7));
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static byte[] ParseMac(string macAddress)
        {
            if (string.IsNullOrWhiteSpace(macAddress) || macAddress == "—")
                return null;

            var parts = macAddress
                .Replace("-", ":")
                .Split(':', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 6)
                return null;

            try
            {
                return parts.Select(p => Convert.ToByte(p, 16)).ToArray();
            }
            catch
            {
                return null;
            }
        }
    }
}
