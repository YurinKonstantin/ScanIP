using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x419

namespace ScanIP
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
          


        }

        CancellationTokenSource cancelTokenSource;
        ViewIP viewIP = new ViewIP();
       async void myIP()
        {
            string ss = String.Empty;
            for(int i=0; i< Dns.GetHostByName(System.Net.Dns.GetHostName()).AddressList.Length; i++)
            {
                ss += System.Net.Dns.GetHostByName(System.Net.Dns.GetHostName()).AddressList[i].ToString() + "\n";
            }
            MessageDialog dd = new MessageDialog(System.Net.Dns.GetHostName() +"\t"+ ss);
          await  dd.ShowAsync();
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            string ss = St.Text;
            string se = En.Text;
            int portS = Convert.ToInt32(Sp.Text);
            int portE = Convert.ToInt32(Ep.Text);
            viewIP.ListIP.Clear();
         
            // адрес сервера
            await Sos.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Sos.Text = "Поиск доступных IP:";
            });
            cancelTokenSource = new CancellationTokenSource();
            CancellationToken token = cancelTokenSource.Token;
            bool g= await Task.Run(()=> TCPIPScan(ss, se, portS, portE, token));
            
            await Sos.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Sos.Text = "Поиск доступных IP: " +"Завершено";
            });
            MessageDialog dd = new MessageDialog("Сканирование сети выбранных значений завершено!", "Завершение сканирования сети");
            await dd.ShowAsync();
        }
        public async Task<bool> TCPIPScan(string ipStart, string ipEnd, int pS, int pE, CancellationToken token)
        {
            string[] ipS = ipStart.Split(".");
            string[] ipE = ipEnd.Split(".");
            int portS = pS;
           int portE = pE;
            for (int j = Convert.ToInt32(ipS[0]); j <= Convert.ToInt32(ipE[0]); j++)
            {
                for (int k = Convert.ToInt32(ipS[1]); k <= Convert.ToInt32(ipE[1]); k++)
                {
                    for (int h = Convert.ToInt32(ipS[2]); h <= Convert.ToInt32(ipE[2]); h++)
                    {
                        for (int i = Convert.ToInt32(ipS[ipS.Length - 1]); i <= Convert.ToInt32(ipE[ipE.Length - 1]); i++)
                        {
                            if (token.IsCancellationRequested)
                            {
                              
                                return false;
                            }
                            await Sos.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                Sos.Text = "Поиск доступных IP: "+ j + "." + k + "." + h + "." + i.ToString();
                            });
                            if (await LocalPing(j + "." + k + "." + h + "." + i.ToString()))
                            {
                                Debug.WriteLine("YES");
                                string ip4 = j + "." + k + "." + h + "." + i.ToString();
                                string ip6 = "Не опредлено";
                                string host = "Не опредлено";
                                try
                                {
                                    host = Dns.GetHostEntry(j + "." + k + "." + h + "." + i.ToString()).HostName;
                                }
                                catch (Exception)
                                {

                                }
                               
                                try
                                {

                                    foreach (var fd in Dns.GetHostEntry(j + "." + k + "." + h + "." + i.ToString()).AddressList)
                                    {
                                        try
                                        {


                                            if (fd.IsIPv6LinkLocal)
                                            {
                                                ip6 = fd.ToString();
                                            }

                                        }
                                        catch (Exception ex)
                                        {

                                        }

                                    }

                                }
                                catch (Exception)
                                {

                                }
                                IP iP = new IP() { IPname4 = ip4, IPname6 = ip6, MyHost=host };
                                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
            () =>
            {
                viewIP.ListIP.Add(iP);

            });
                                Task.Run(() => scanPort(portS, portE, iP, iP.IPname4, token));


                            }
                        }
                    }

                }
            }
            return true;
        }
        public bool TCPIPScan(string ipStart, string ipEnd, int pS, int pE, CancellationToken token, bool tt)
        {
            List<IP> newList = new List<IP>();
            string[] ipS = ipStart.Split(".");
            string[] ipE = ipEnd.Split(".");
            int portS = pS;
            int portE = pE;
            for (int j = Convert.ToInt32(ipS[0]); j <= Convert.ToInt32(ipE[0]); j++)
            {
                for (int k = Convert.ToInt32(ipS[1]); k <= Convert.ToInt32(ipE[1]); k++)
                {
                    for (int h = Convert.ToInt32(ipS[2]); h <= Convert.ToInt32(ipE[2]); h++)
                    {
                        for (int i = Convert.ToInt32(ipS[ipS.Length - 1]); i <= Convert.ToInt32(ipE[ipE.Length - 1]); i++)
                        {
                           
                            string ip4 = j + "." + k + "." + h + "." + i.ToString();
                            string ip6 = "Не опредлено";
                            string host = "Не опредлено";
                            newList.Add(new IP() { IPname4 = ip4, IPname6 = ip6, MyHost = host });
                          
                        }
                    }

                }
            }
            var ff = from ip in newList.AsParallel<IP>() where ip.LocalPingIP == true select ip;
            Sos.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Sos.Text = "Поиск доступных IP: " + ff.Count().ToString();
            });
            foreach (var ip in ff)
            {


             //   await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High,
   // () =>
   // {
        viewIP.ListIP.Add(ip);

  //  });
            }
            return true;
        }
        private async void IpListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var gg = (IP)((ListView)sender).SelectedItem;

            if (gg != null)
            {


                PortListView.ItemsSource = gg.Ports;
                
            }
           
        }
        public async void scanPort(int StartPort, int EndPort, IP h, string IPname4, CancellationToken token)
        {
            for (int CurrPort = StartPort; CurrPort <= EndPort; CurrPort++)
            {

                if (token.IsCancellationRequested)
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
() =>
{
h.IsScanPort = "";
});
                    return;
                }
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
() =>
{
    h.IsScanPort = "Идет поиск открытых портов. Порт: "+ CurrPort.ToString();
});
                Debug.WriteLine(CurrPort);
                //Инициализируем новый экземпляр класса 
                TcpClient TcpScan = new System.Net.Sockets.TcpClient();
                try
                {
                    TcpScan.ReceiveTimeout = 10;
                    TcpScan.SendTimeout = 10;
                    //Выполняем подключение клиента к указанному порту заданного узла.
                    await TcpScan.ConnectAsync(IPname4, CurrPort);
                    //Если подключение выполнено успешно то выводим в listBox1
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
() =>
{
    h.Ports.Add(new Port() { namePort = CurrPort.ToString(), isOpen = "Открыт" });
});
                }
                catch
                {
  
                    //Если возникло исключение то порт закрыт

                }
                //Переводим курсор на последнюю строку списка

            }
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
() =>
{
h.IsScanPort = "Открытые порты";
});
        }
        private async Task<bool> LocalPing(string ip2)//Сканер адресов IP
        {
         
            try
            {
                Debug.WriteLine(ip2);
                IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(ip2), 0);
                Dns.GetHostEntry(ip2);
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.SendTimeout = 50;
                socket.ReceiveTimeout = 50;
              socket.NoDelay = true;
                //socket.Bind(ipPoint);
                Dns.GetHostEntry(ip2);
                return true;
               
            }
            catch(SocketException ee)
            {
                return false;
            }
            catch (Exception ex)
            {

                return false;
            }
        }

        private void AppBarButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

        private void AppBarButton_Click_1(object sender, RoutedEventArgs e)
        {
            cancelTokenSource.Cancel();
        }
    }
}
