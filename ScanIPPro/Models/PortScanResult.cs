using ScanIPPro.Services;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ScanIPPro.Models
{
    public enum PortStatus
    {
        Open,
        Closed,
        Filtered
    }

    public class PortScanResult : INotifyPropertyChanged
    {
        private int _port;
        private string _protocol;
        private PortStatus _status;
        private string _service;
        private string _banner;

        public int Port
        {
            get => _port;
            set { _port = value; OnPropertyChanged(); }
        }

        public string Protocol
        {
            get => _protocol;
            set { _protocol = value; OnPropertyChanged(); }
        }

        public PortStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusText));
            }
        }

        public string Service
        {
            get => _service ?? LocalizationService.GetString("PortStatus.Unknown");
            set { _service = value; OnPropertyChanged(); }
        }

        public string Banner
        {
            get => _banner ?? "—";
            set { _banner = value; OnPropertyChanged(); }
        }

        public string StatusText => LocalizationService.GetPortStatusDisplay(Status);

        public void RefreshLocalizedProperties()
        {
            OnPropertyChanged(nameof(Service));
            OnPropertyChanged(nameof(StatusText));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
