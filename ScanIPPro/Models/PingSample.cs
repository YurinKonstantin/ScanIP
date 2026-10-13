using System;

namespace ScanIPPro.Models
{
    public class PingSample
    {
        public DateTime Timestamp { get; set; }
        public long RoundtripTime { get; set; }
        public bool Success { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
