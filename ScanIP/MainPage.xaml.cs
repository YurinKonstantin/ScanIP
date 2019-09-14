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
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage;
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
        private int reviewBarrier = 5;
        int NowreviewBarrier = 0;
        bool showOt = false;
        private string reviewKey = "userReviewedApp";
        private string launchesKey = "userReviewAppLaunches";
        private string reviewText = "Надеемся, вам нравится наше приложение XXXX.";
        private string reviewInviteText = "Пожалуйста, оцените наше приложение";
        private void updateReviewStatus()
        {

            try
            {


                ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
                Windows.Storage.ApplicationDataCompositeValue composite = (ApplicationDataCompositeValue)roamingSettings.Values["RoamingFontInfo"];
                if (composite != null)
                {
                    showOt = Convert.ToBoolean(composite["Reviewed"]);
                    Debug.WriteLine(showOt.ToString());
                    NowreviewBarrier = (int)composite["reviewBarrier"];
                    Debug.WriteLine(NowreviewBarrier.ToString());

                    if (Convert.ToBoolean(showOt))
                    {
                        NowreviewBarrier++;
                           composite["Reviewed"] = "true";
                        composite["reviewBarrier"] = NowreviewBarrier;
                        roamingSettings.Values["RoamingFontInfo"] = composite;
                    }
                }

                else
                {


                    // Save a composite setting that will be roamed between devices
                     composite = new Windows.Storage.ApplicationDataCompositeValue();
                    Debug.WriteLine("creat");
                    composite["Reviewed"] = "true";
                    composite["reviewBarrier"] = 0;
                    roamingSettings.Values["RoamingFontInfo"] = composite;

                }


            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            // load a composite setting that roams between devices
           
          
        }


        private async void checkReviews()
        {
           

            if (showOt && (NowreviewBarrier == reviewBarrier))
            {
                var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                Debug.WriteLine("Show");
                ContentDialog deleteFileDialog = new ContentDialog
                {
                    Title = resourceLoader.GetString("ShowTitle"),
                    Content = resourceLoader.GetString("ShowContent"),
                    PrimaryButtonText = resourceLoader.GetString("ShowPrimaryText"),
                    SecondaryButtonText= resourceLoader.GetString("ShowSecondaryText"),
                    CloseButtonText = resourceLoader.GetString("ShowCloseText")

                };

                ContentDialogResult result = await deleteFileDialog.ShowAsync();

                // Delete the file if the user clicked the primary button.
                /// Otherwise, do nothing.
                if (result == ContentDialogResult.Primary)
                {
                    bool result1 = await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-windows-store://review/?ProductId=9N0RCC49K629"));
                    if(result1)
                    {
                        ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
                        Windows.Storage.ApplicationDataCompositeValue composite = (ApplicationDataCompositeValue)roamingSettings.Values["RoamingFontInfo"];
                        composite["Reviewed"] = "false";
                        composite["reviewBarrier"] = 0;
                        roamingSettings.Values["RoamingFontInfo"] = composite;
                        Debug.WriteLine("Yess false");
                    }

                }
                if (result == ContentDialogResult.Secondary)
                {
                    ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
                    Windows.Storage.ApplicationDataCompositeValue composite = (ApplicationDataCompositeValue)roamingSettings.Values["RoamingFontInfo"];
                    composite["Reviewed"] = "true";
                    composite["reviewBarrier"] = 0;
                    roamingSettings.Values["RoamingFontInfo"] = composite;
                    Debug.WriteLine("Yess true");
                }
                else
                {
                    ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
                    Windows.Storage.ApplicationDataCompositeValue composite = (ApplicationDataCompositeValue)roamingSettings.Values["RoamingFontInfo"];
                    composite["Reviewed"] = "false";
                    composite["reviewBarrier"] = 0;
                    roamingSettings.Values["RoamingFontInfo"] = composite;
                    Debug.WriteLine("nooo false");
                }



            }
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
        Task task;
        List<Task> list = new List<Task>();
        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            // Detect if Internet can be reached
          

            string ss = St.Text;
            string se = En.Text;
           
           
            int portS = Convert.ToInt32(Sp.Text);
            int portE = Convert.ToInt32(Ep.Text);
            viewIP.ListIP.Clear();
         
            // адрес сервера
            await Sos.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // Sos.Text = "Поиск доступных IP:";
                Sos.Text = resourceLoader.GetString("SostPoisk");
            });
            cancelTokenSource = new CancellationTokenSource();
            CancellationToken token = cancelTokenSource.Token;
            if(turbo.IsOn)
            {
                await Task.Run(() => Scan(ss, se, portS, portE, token));
              await  waitTask();
            }
            else
            {
                bool g = await Task.Run(() => TCPIPScan(ss, se, portS, portE, token));
            }
            
           
            
            await Sos.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // Sos.Text = "Поиск доступных IP: " +"Завершено";
                Sos.Text = resourceLoader.GetString("SostEnd");
            });
            
            MessageDialog dd = new MessageDialog(resourceLoader.GetString("MesText"), resourceLoader.GetString("MesHead") );
            await dd.ShowAsync();
            updateReviewStatus();
            checkReviews();
        }
        public async Task waitTask()
        {
            while(true)
            {
                lock (list)
                {
                    Task.WaitAll(list.ToArray());
                    break;
                }
            }
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
                                var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                                Sos.Text = resourceLoader.GetString("SostTec") + j + "." + k + "." + h + "." + i.ToString();
                                
                            });
                            if (await LocalPing(j.ToString() + "." + k.ToString() + "." + h.ToString() + "." + i.ToString()))
                            {
                               
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
        public async Task<bool> Scan(string ipStart, string ipEnd, int pS, int pE, CancellationToken token)
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
                            string s =  j.ToString() + "." + k.ToString() + "." + h.ToString() + "." + i.ToString();

                            if (token.IsCancellationRequested)
                            {
                                return false;
                            }
                            Task t = Task.Run(() => TCPIPScan(s, portS, portE, token, true));

                           await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
() =>
{
    list.Add(t);
});
                            await Sos.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                                Sos.Text = resourceLoader.GetString("SostTec") + s;

                            });


                        }
                    }

                }
            }
            return true;

        }
        public async void TCPIPScan(string ipStart, int portS, int portE, CancellationToken token, bool tt)
        {
            try
            {

                Debug.WriteLine(ipStart);
                if (await LocalPing(ipStart))
                {

                    string ip4 = ipStart;
                    string ip6 = "Не опредлено";
                    string host = "Не опредлено";
                    try
                    {
                        host = Dns.GetHostEntry(ipStart).HostName;
                    }
                    catch (Exception)
                    {

                    }

                    try
                    {

                        foreach (var fd in Dns.GetHostEntry(ipStart).AddressList)
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
                    IP iP = new IP() { IPname4 = ip4, IPname6 = ip6, MyHost = host };
                    Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
    () =>
    {
        viewIP.ListIP.Add(iP);

    });
                    Task.Run(() => scanPort(portS, portE, iP, iP.IPname4, token));


                }
            }
            catch(Exception ex)
            {

            }
        }
        private async void IpListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var gg = (IP)((ListView)sender).SelectedItem;

            if (gg != null)
            {


                PortListView.ItemsSource = gg.Ports;
                
            }
           
        }
        ResourceLoader resourceLoader1 = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
        public async void scanPort(int StartPort, int EndPort, IP h, string IPname4, CancellationToken token)
        {

          //  var ff = from x in Enumerable.Range(0, 10).AsParallel() where new TcpClient().ConnectAsync(IPname4, x).ToString() != "1" select new Port() { namePort = Convert.ToString(x), isOpen = resourceLoader1.GetString("Open") };
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
    var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();

    h.IsScanPort = resourceLoader.GetString("TecPort") + CurrPort.ToString();
});
               
                //Инициализируем новый экземпляр класса 
                IPAddress iPAddress = IPAddress.Parse(IPname4);
                IPEndPoint ep = new IPEndPoint(iPAddress, CurrPort);
                Socket soc = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IAsyncResult asyncResult = soc.BeginConnect(ep, new AsyncCallback(ConnectCallback), soc);
                if(!asyncResult.AsyncWaitHandle.WaitOne(30,false))
                {
                    soc.Close();
                }
                else
                {
                    soc.Close();
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
() =>
{
var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
h.Ports.Add(new Port() { namePort = CurrPort.ToString(), isOpen = resourceLoader.GetString("Open") });
});

                }


            }
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
() =>
{
    var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
    h.IsScanPort = resourceLoader.GetString("OpensPorts");
});
        }
        public static ManualResetEvent connectDone = new ManualResetEvent(false);
        public static ManualResetEvent connectDoneIP = new ManualResetEvent(false);
        private static void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;
                client.EndConnect(ar);
                connectDone.Set();
            }
            catch (Exception e)
            {

            }
        }
        private static void ConnectCallbackIP(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;
              
                client.EndConnect(ar);
                connectDoneIP.Set();
            }
            catch (Exception e)
            {

            }
        }
    
        private async Task<bool> LocalPing(string ip2)//Сканер адресов IP
        {
         
            try
            {
            
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
