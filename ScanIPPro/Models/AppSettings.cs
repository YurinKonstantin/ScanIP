namespace ScanIPPro.Models
{
    public class AppSettings
    {
        public string Language { get; set; } = "en-US";
        public string Theme { get; set; } = "System";
        public int PingTimeoutMs { get; set; } = 2000;
        public int Concurrency { get; set; } = 20;
        public bool OnlineOnly { get; set; } = true;
    }
}
