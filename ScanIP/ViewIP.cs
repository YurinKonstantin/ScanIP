using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ScanIP
{
    public class ViewIP : INotifyPropertyChanged
    {
        public ViewIP()
        {
            MyHost= Dns.GetHostName();
            MyIp4 = Dns.GetHostByName(MyHost).AddressList[Dns.GetHostByName(MyHost).AddressList.Length-1].ToString();
            MyIp6 = Dns.GetHostByName(MyHost).AddressList[0].ToString();

        }
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            // Raise the PropertyChanged event, passing the name of the property whose value has changed.
            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        public ObservableCollection<IP> ListIP = new ObservableCollection<IP>();
        string myIp4 ="127.1.1.1.";
        public string MyIp4
        {
            get
            {
                return myIp4;
            }
            set
            {
                myIp4 = value;
                OnPropertyChanged();
            }
        }
        string myIp6 = "127.1.1.1.";
        public string MyIp6
        {
            get
            {
                return myIp6;
            }
            set
            {
                myIp6 = value;
                OnPropertyChanged();
            }
        }
        string myHost= "127.1.1.1.";
        public string MyHost
        {
            get
            {
                return myHost;
            }
            set
            {
                myHost = value;
                OnPropertyChanged();
            }
        }
        string isScanPort = "127.1.1.1.";
        public string IsScanPort
        {
            get
            {
                return isScanPort;
            }
            set
            {
                isScanPort = value;

            }
        }
    }
}
