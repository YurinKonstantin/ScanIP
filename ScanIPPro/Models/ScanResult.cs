using ScanIPPro.Services;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ScanIPPro.Models
{
    public class ScanResult : INotifyPropertyChanged
    {
        public const string StatusOnline = "Online";
        public const string StatusOffline = "Offline";

        private string _status = StatusOffline;
        private string _ipAddress;
        private string _hostName;
        private string _macAddress;
        private string _vendor;
        private string _openPorts;
        private long _roundtripTime;

        public string IPAddress
        {
            get => _ipAddress;
            set { _ipAddress = value; OnPropertyChanged(); }
        }

        public string HostName
        {
            get => string.IsNullOrEmpty(_hostName)
                ? LocalizationService.GetString("HostName.Unknown")
                : _hostName;
            set { _hostName = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayHostName)); }
        }

        public string DisplayHostName => HostName;

        public string MACAddress
        {
            get => _macAddress ?? "—";
            set { _macAddress = value; OnPropertyChanged(); }
        }

        public string Vendor
        {
            get => _vendor ?? "—";
            set { _vendor = value; OnPropertyChanged(); }
        }

        public string OpenPorts
        {
            get => _openPorts ?? "—";
            set { _openPorts = value; OnPropertyChanged(); }
        }

        public long RoundtripTime
        {
            get => _roundtripTime;
            set { _roundtripTime = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsOnline));
                OnPropertyChanged(nameof(DisplayStatus));
            }
        }

        public string DisplayStatus => LocalizationService.GetStatusDisplay(_status);

        public bool IsOnline => Status == StatusOnline;

        public event PropertyChangedEventHandler PropertyChanged;

        public void RefreshLocalizedProperties()
        {
            OnPropertyChanged(nameof(HostName));
            OnPropertyChanged(nameof(DisplayHostName));
            OnPropertyChanged(nameof(DisplayStatus));
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
