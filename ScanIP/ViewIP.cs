using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ScanIP
{
    public class ViewIP : INotifyPropertyChanged
    {
        public ViewIP()
        {
            try
            {


                MyHost = Dns.GetHostName();
              //  foreach (var dd in Dns.GetHostByName(MyHost).AddressList)
                {
                   // string tip = "IP4";
                   // if (dd.AddressFamily.ToString().Contains("6"))
                    {
                    //    tip = "IP6";
                    }
                  
                   // ListMyIP.Add(new ClassMyIP() { MyIp = dd.ToString(), MytipInt = tip });
                }
               // NetInfo();
            }
            catch(Exception ex)
            {

            }
         

        }
        public void NetInfo()
        {
            try
            {
                NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
                // перебираем все сетевые интерфейсы
                foreach (NetworkInterface nic in adapters)
                {
                    if (nic.OperationalStatus==OperationalStatus.Up)
                    { 
                       
                        
                       
                        
                        ClassMyIP classMyIP = new ClassMyIP();
                      
                    string strInterfaceName = nic.Name; // наименование интерфейса
                    classMyIP.MyName = strInterfaceName;
                        if (nic.OperationalStatus == OperationalStatus.Up)
                        {
                            classMyIP.Activ = true;
                        }
                        else
                        {
                            classMyIP.Activ = false;
                        }
                            string strPhysicalAddress = nic.GetPhysicalAddress().ToString(); //МАС - адрес

                    string strAddr = nic.Name + "\n" + strPhysicalAddress + "\n";
                    int x = 0;
                  //  Debug.WriteLine(nic.);
                    // перебираем IP адреса
                    IPInterfaceProperties properties = nic.GetIPProperties();
                    foreach (UnicastIPAddressInformation unicast in properties.UnicastAddresses)
                    {
                        strAddr += unicast.Address.ToString() + " / " + unicast.Address.AddressFamily.ToString() + "\n";
                        if (unicast.Address.AddressFamily.ToString() == "InterNetworkV6")
                        {
                            classMyIP.MyIp6 = unicast.Address.ToString();
                            Debug.WriteLine(unicast.Address.ToString() + " ffff " + unicast.Address.AddressFamily.ToString());
                        }
                        if (unicast.Address.AddressFamily.ToString() == "InterNetwork" && x == 0)
                        {
                            classMyIP.MyIp4 = unicast.Address.ToString();
                            x++;
                        }
                    }

                    // перебираем днс-сервера
                    foreach (IPAddress dnsAddress in properties.DnsAddresses)
                    {
                        strAddr += dnsAddress.ToString() + "\n";
                        //classMyIP.MyIp6 = dnsAddress.ToString();
                    }

                    // перебираем шлюзы
                    foreach (GatewayIPAddressInformation gatewayIpAddressInformation in properties.GatewayAddresses)
                    {
                        strAddr += gatewayIpAddressInformation.Address.ToString() + "\n";
                    }
                    if (classMyIP.MyIp4!="127.0.0.1")
                        {
                            ListMyIP.Add(classMyIP);
                        }
                   
                    //MessageBox.Show(strAddr);
                    Debug.WriteLine(strAddr);
                }
                }

            }
            catch (Exception ex)
            {
              //  MessageBox.Show("Error");
            }
        }
        public ObservableCollection<ClassMyIP> ListMyIP = new ObservableCollection<ClassMyIP>();
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            // Raise the PropertyChanged event, passing the name of the property whose value has changed.
            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
       public List<IP> ListIP = new List<IP>();
    
        public ObservableCollection<string> ListDNSIP = new ObservableCollection<string>();
        public ObservableCollection<string> ListDNSAl = new ObservableCollection<string>();
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
