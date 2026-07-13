using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

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
            set { _status = value; OnPropertyChanged(); }
        }

        public string Service
        {
            get => _service ?? "Неизвестно";
            set { _service = value; OnPropertyChanged(); }
        }

        public string Banner
        {
            get => _banner ?? "—";
            set { _banner = value; OnPropertyChanged(); }
        }

        public string StatusText => Status switch
        {
            PortStatus.Open => "Открыт",
            PortStatus.Closed => "Закрыт",
            PortStatus.Filtered => "Фильтруется",
            _ => "Неизвестно"
        };

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
