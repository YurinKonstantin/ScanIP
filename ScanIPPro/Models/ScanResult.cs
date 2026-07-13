using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace ScanIPPro.Models
{
    /// <summary>
    /// Модель результата сканирования одного IP-адреса
    /// </summary>
    public class ScanResult : INotifyPropertyChanged
    {
        private string _status = "Offline";
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
            get => _hostName ?? "Не определено";
            set { _hostName = value; OnPropertyChanged(); }
        }

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
            set { _status = value; OnPropertyChanged(); }
        }

        public bool IsOnline => Status == "Online";

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
