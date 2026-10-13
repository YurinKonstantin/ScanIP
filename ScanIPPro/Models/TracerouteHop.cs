namespace ScanIPPro.Models
{
    public class TracerouteHop
    {
        public int Hop { get; set; }
        public string Address { get; set; } = "—";
        public string HostName { get; set; } = "—";
        public long RoundtripTime { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
