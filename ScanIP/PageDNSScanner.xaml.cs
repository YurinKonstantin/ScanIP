using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    public sealed partial class PageDNSScanner : Page
    {
        public PageDNSScanner()
        {
            this.InitializeComponent();
        }
        ViewIP viewIP = new ViewIP();
        private async void AppBarButton_Click_2(object sender, RoutedEventArgs e)
        {
            try
            {
                string hostname = textDNS.Text;
                viewIP.ListDNSAl.Clear();
                viewIP.ListDNSIP.Clear();
                IPHostEntry entry = await Dns.GetHostEntryAsync(hostname);
                string ip = String.Empty;
                foreach (IPAddress a in entry.AddressList)
                {

                    viewIP.ListDNSIP.Add(a.ToString());

                }
                foreach (string a in entry.Aliases)
                {
                    viewIP.ListDNSAl.Add(a.ToString());
                }
            }
            catch (Exception ex)
            {

            }
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            MessageDialog dd = new MessageDialog(resourceLoader.GetString("MesText"), resourceLoader.GetString("MesHead"));
            await dd.ShowAsync();


        }
    }
}
