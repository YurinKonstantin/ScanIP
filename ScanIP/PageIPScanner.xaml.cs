using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
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

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace ScanIP
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class PageIPScanner : Page
    {
        public PageIPScanner()
        {
            this.InitializeComponent();
            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            String localValue = localSettings.Values["langsetting"] as string;
            int x = 0;
            if (localSettings == null || localValue == String.Empty)
            {
                localValue = "Auto";
            }
            if (localValue == "Русский")
            {
                Windows.ApplicationModel.Resources.Core.ResourceContext.SetGlobalQualifierValue("Language", "ru-RU");
                x = 2;
            }
            if (localValue == "English")
            {
                Windows.ApplicationModel.Resources.Core.ResourceContext.SetGlobalQualifierValue("Language", "en-DE");
                x = 1;
            }
            if (localValue == "Deutsch")
            {
                Windows.ApplicationModel.Resources.Core.ResourceContext.SetGlobalQualifierValue("Language", "de-DE");
                x = 3;
            }
            if (localValue == "Japanese")
            {
                Windows.ApplicationModel.Resources.Core.ResourceContext.SetGlobalQualifierValue("Language", "ja-JP");
                x = 4;
            }
            if (localValue == "French")
            {
                Windows.ApplicationModel.Resources.Core.ResourceContext.SetGlobalQualifierValue("Language", "fr-FR");
                x = 5;
            }

            this.InitializeComponent();
    
            viewIP.NetInfo();
            myIP();

            ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            Windows.Storage.ApplicationDataCompositeValue composite = (ApplicationDataCompositeValue)roamingSettings.Values["SettingFontInfo"];
            if (composite != null)
            {
                St.Text = composite["StartIP"] as string;
                En.Text = composite["EndIP"] as string;
                Sp.Text = composite["StartPort"] as string;
                Ep.Text = composite["EndPort"] as string;
            }
            IpListView.ItemsSource = viewIP.ListIP;
           
        }
        protected async override void OnNavigatedFrom(NavigationEventArgs e)
        {
            IpListView.ItemsSource = viewIP.ListIP.OrderBy(ip => Convert.ToInt32(ip.IPname4.Split('.')[3]));
        }
        List<string> lang = new List<string>();
        private int reviewBarrier = 5;
        int NowreviewBarrier = 0;
        bool showOt = false;

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
            catch (Exception ex)
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
                    SecondaryButtonText = resourceLoader.GetString("ShowSecondaryText"),
                    CloseButtonText = resourceLoader.GetString("ShowCloseText")

                };

                ContentDialogResult result = await deleteFileDialog.ShowAsync();

                // Delete the file if the user clicked the primary button.
                /// Otherwise, do nothing.
                if (result == ContentDialogResult.Primary)
                {
                    bool result1 = await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-windows-store://review/?ProductId=9N0RCC49K629"));
                    if (result1)
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
            try
            {


                string MyHost = Dns.GetHostName();
                foreach (var dd in Dns.GetHostByName(MyHost).AddressList)
                {
                    string tip = "IP4";
                    if (dd.AddressFamily.ToString().Contains("6"))
                    {
                        tip = "IP6";
                    }
                    if (tip == "IP4")
                    {
                        // St.Text = dd.ToString().Split(".")[0] + "." + dd.ToString().Split(".")[1] + "." + dd.ToString().Split(".")[2] + "." + "1";
                        //  En.Text = dd.ToString().Split(".")[0] + "." + dd.ToString().Split(".")[1] + "." + dd.ToString().Split(".")[2] + "." + "255";
                    }

                }
                var f = from d in viewIP.ListMyIP where d.Activ == true select d.MyIp4;
                if (f.Count() == 1)
                {
                    St.Text = f.ElementAt(1).ToString().Split(".")[0] + "." + f.ElementAt(1).ToString().Split(".")[1] + "." + f.ElementAt(1).ToString().Split(".")[2] + "." + "1";
                    En.Text = f.ElementAt(1).ToString().Split(".")[0] + "." + f.ElementAt(1).ToString().Split(".")[1] + "." + f.ElementAt(1).ToString().Split(".")[2] + "." + "255";
                }
                if (f.Count() > 1)
                {
                    St.Text = f.ElementAt(1).ToString().Split(".")[0] + "." + f.ElementAt(1).ToString().Split(".")[1] + "." + f.ElementAt(1).ToString().Split(".")[2] + "." + "1";
                    En.Text = f.ElementAt(1).ToString().Split(".")[0] + "." + f.ElementAt(1).ToString().Split(".")[1] + "." + f.ElementAt(1).ToString().Split(".")[2] + "." + "255";
                }

                App.ThemeManager.LoadTheme(ThemeManager.DarkThemePath);
            }
            catch (Exception ex)
            {

            }
        }
        Task task;
        List<Task> list = new List<Task>();
        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            // Detect if Internet can be reached


            string ss = St.Text;
            string se = En.Text;

            try
            {


                int portS = Convert.ToInt32(Sp.Text);
                int portE = Convert.ToInt32(Ep.Text);
                if (portS < 1)
                {
                    portS = 1;
                }
                viewIP.ListIP.Clear();
                IpListView.ItemsSource = viewIP.ListIP;

                // адрес сервера
                await Sos.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                // Sos.Text = "Поиск доступных IP:";
                Sos.Text = resourceLoader.GetString("SostPoisk");
                });
                cancelTokenSource = new CancellationTokenSource();
                CancellationToken token = cancelTokenSource.Token;

                await Task.Run(() => Scan(ss, se, portS, portE, token));
                await waitTask();




                await Sos.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                // Sos.Text = "Поиск доступных IP: " +"Завершено";
                Sos.Text = resourceLoader.GetString("SostEnd");
                });

                MessageDialog dd = new MessageDialog(resourceLoader.GetString("MesText"), resourceLoader.GetString("MesHead"));
                await dd.ShowAsync();
                updateReviewStatus();
                checkReviews();
            }
            catch(Exception ex)
            {
                await new MessageDialog(ex.Message).ShowAsync();
            }
        }
        public async Task waitTask()
        {
            while (true)
            {
                lock (list)
                {
                    Task.WaitAll(list.ToArray());
                    break;
                }
            }
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
                            string s = j.ToString() + "." + k.ToString() + "." + h.ToString() + "." + i.ToString();

                            if (token.IsCancellationRequested)
                            {
                                return false;
                            }
                            Task t = Task.Run(() => TCPIPScan(s, portS, portE, token, true));

                            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High,
 () =>
 {
     list.Add(t);
    // IpListView.ItemsSource = viewIP.ListIP.OrderBy(ip=>ip.IPname4);
});
                            await Sos.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
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


                if (await LocalPing(ipStart))
                {

                    string ip4 = ipStart;

                    string ip6 = String.Empty;
                    string host = String.Empty;
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
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High,
    () =>
    {
        viewIP.ListIP.Add(iP);
        IpListView.ItemsSource = viewIP.ListIP.OrderBy(ip => Convert.ToInt32(ip.IPname4.Split('.')[3]));
        int x = 0;
        foreach (var f in IpListView.Items)
        {
            if (select.IPname4 == ((IP)f).IPname4)
            {
                IpListView.SelectedIndex = x;
            }
            x++;
        }
        //PortListView.ItemsSource = select.Ports;


    });
                    Task.Run(() => scanPort(portS, portE, iP, iP.IPname4, token));


                }
            }
            catch (Exception ex)
            {

            }
        }
        public IP select = new IP();
        private async void IpListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var gg = (IP)((ListView)sender).SelectedItem;

            if (gg != null)
            {
                PortListView.ItemsSource = gg.Ports;
                select = gg;
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
                if (!asyncResult.AsyncWaitHandle.WaitOne(30, false))
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


        private async Task<bool> LocalPing(string ip2)//Сканер адресов IP
        {

            try
            {


                Dns.GetHostEntry(ip2);


                return true;

            }
            catch (SocketException ee)
            {
                try
                {


                    TcpClient client = new TcpClient();
                    client.ReceiveTimeout = 100;
                    client.SendTimeout = 100;
                    //client.NoDelay = true;
                    await client.ConnectAsync(ip2, 1);
                    client.Close();
                    return true;
                }
                catch (SocketException e)
                {
                    if (e.ErrorCode == 10061)
                    {
                        return true;
                    }
                    return false;
                }
                catch (Exception ew)
                {
                    return false;
                }


            }
            catch (Exception ex)
            {
                try
                {


                    TcpClient client = new TcpClient();
                    client.ReceiveTimeout = 100;
                    client.SendTimeout = 100;
                    //client.NoDelay = true;
                    await client.ConnectAsync(ip2, 80);
                    client.Close();
                    return true;
                }
                catch (SocketException e)
                {
                    if (e.ErrorCode == 10061)
                    {
                        return true;
                    }
                    return false;
                }
                catch (Exception ew)
                {
                    return false;
                }
                return false;
            }
        }


        private void AppBarButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            splitView.IsPaneOpen = !splitView.IsPaneOpen;
        }

        private void AppBarButton_Click_1(object sender, RoutedEventArgs e)
        {
            cancelTokenSource.Cancel();
        }

      
        private void ColorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Add "using Windows.UI;" for Color and Colors.
            string colorName = e.AddedItems[0].ToString();
            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            switch (colorName)
            {
                case "Auto":

                    localSettings.Values["Langsetting"] = "Auto";
                    this.InitializeComponent();
                    break;
                case "English":
                    localSettings.Values["Langsetting"] = "English";
                    Windows.ApplicationModel.Resources.Core.ResourceContext.SetGlobalQualifierValue("Language", "en-US");
                    this.InitializeComponent();
                    break;
                case "Deutsch":
                    localSettings.Values["Langsetting"] = "Deutsch";
                    Windows.ApplicationModel.Resources.Core.ResourceContext.SetGlobalQualifierValue("Language", "de-DE");
                    this.InitializeComponent();
                    break;
                case "Русский":
                    localSettings.Values["Langsetting"] = "Русский";
                    Windows.ApplicationModel.Resources.Core.ResourceContext.SetGlobalQualifierValue("Language", "ru-RU");
                    this.InitializeComponent();
                    break;
                case "Japanese":
                    localSettings.Values["Langsetting"] = "Japanese";
                    Windows.ApplicationModel.Resources.Core.ResourceContext.SetGlobalQualifierValue("Language", "ja-JP");
                    this.InitializeComponent();
                    break;
                case "French":
                    localSettings.Values["Langsetting"] = "French";
                    Windows.ApplicationModel.Resources.Core.ResourceContext.SetGlobalQualifierValue("Language", "fr-FR");
                    this.InitializeComponent();
                    break;

            }

        }

        private void AppBarButton_Tapped_1(object sender, TappedRoutedEventArgs e)
        {

            ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            Windows.Storage.ApplicationDataCompositeValue composite = new Windows.Storage.ApplicationDataCompositeValue();
            composite["StartIP"] = St.Text;
            composite["EndIP"] = En.Text;
            composite["StartPort"] = Sp.Text;
            composite["EndPort"] = Ep.Text;
            roamingSettings.Values["SettingFontInfo"] = composite;
        }
    }
}
