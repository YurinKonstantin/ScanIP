using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ClassLibrary2
{
   static public class Class1
    {
        public static async Task<bool> LocalPing(string ip2)//Сканер адресов IP
        {

            try
            {
                Ping pingSender = new Ping();
                PingOptions options = new PingOptions();

                // Use the default Ttl value which is 128,
                // but change the fragmentation behavior.
                options.DontFragment = true;

                // Create a buffer of 32 bytes of data to be transmitted.


                PingReply reply = await pingSender.SendPingAsync(ip2, 100);

                if (reply.Status == IPStatus.Success)
                {
                    Debug.WriteLine("Address: {0}", reply.Address.ToString());
                    Debug.WriteLine("RoundTrip time: {0}", reply.RoundtripTime);
                    Debug.WriteLine("Time to live: {0}", reply.Options.Ttl);
                    Debug.WriteLine("Don't fragment: {0}", reply.Options.DontFragment);
                    Debug.WriteLine("Buffer size: {0}", reply.Buffer.Length);
                    return true;
                }
                return false;

            }
            catch (SocketException ee)
            {
                return false;
            }
            catch (Exception ex)
            {

                return false;
            }
        }
    }
}
