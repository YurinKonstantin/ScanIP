using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ScanIP
{
   public class IP : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            // Raise the PropertyChanged event, passing the name of the property whose value has changed.
            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        public  string IPname4 { get; set; }
       public string IPname6 { get; set; }
        public ObservableCollection<Port> Ports = new ObservableCollection<Port>();
        string myHost = "127.1.1.1.";
        public string MyHost
        {
            get
            {
                return myHost;
            }
            set
            {
                myHost = value;
              
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
                OnPropertyChanged();

            }
        }
        public bool LocalPingIP
        {
            get
            {
                return LocalPing();
            }
  
        }
        public bool LocalPing()//Сканер адресов IP
        {

            try
            {
                IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(IPname4), 0);
                Dns.GetHostEntry(IPname4);
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.SendTimeout = 50;
                socket.ReceiveTimeout = 50;
                socket.NoDelay = true;
                //socket.Bind(ipPoint);
                Dns.GetHostEntry(IPname4);
                Debug.WriteLine(IPname4);
                return true;

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
