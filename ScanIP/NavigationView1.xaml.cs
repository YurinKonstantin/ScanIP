using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public sealed partial class NavigationView1 : Page
    {
        public NavigationView1()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
        }
        private void NavView_ItemInvoked(object sender, NavigationViewItemInvokedEventArgs args)
        {
            try
            {


                if (args.IsSettingsInvoked)
                {
                     ContentFrame.Navigate(typeof(PageSetting));
                }
                else
                {
                    // find NavigationViewItem with Content that equals InvokedItem

                    var item = ((NavigationView)sender).MenuItems.OfType<NavigationViewItem>().First(x => (string)x.Content == (string)args.InvokedItem);
                    NavView_Navigate(item as NavigationViewItem);
                }
            }
            catch(Exception ex)
            {

            }
        }
     
        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            // you can also add items in code behind
           // NavView.MenuItems.Add(new NavigationViewItemSeparator());
           // NavView.MenuItems.Add(new NavigationViewItem()
           // { Content = "My content", Icon = new SymbolIcon(Symbol.Folder), Tag = "content" });

            // set the initial SelectedItem 
            foreach (NavigationViewItemBase item in NavView.MenuItems)
            {
                if (item is NavigationViewItem && item.Tag.ToString() == "IPScan")
                {
                    NavView.SelectedItem = item;
                    ContentFrame.Navigate(typeof(PageIPScanner));
                   // NavView.Header = "IP Scanner";


                    break;
                }
            }

          //  ContentFrame.Navigated += On_Navigated;

            // add keyboard accelerators for backwards navigation
          //  KeyboardAccelerator GoBack = new KeyboardAccelerator();
           // GoBack.Key = VirtualKey.GoBack;
          //  GoBack.Invoked += BackInvoked;
          //  KeyboardAccelerator AltLeft = new KeyboardAccelerator();
          //  AltLeft.Key = VirtualKey.Left;
          //  AltLeft.Invoked += BackInvoked;
           // this.KeyboardAccelerators.Add(GoBack);
           // this.KeyboardAccelerators.Add(AltLeft);
            // ALT routes here
         //   AltLeft.Modifiers = VirtualKeyModifiers.Menu;
           // NavView.IsPaneOpen = false;


        }
        private async void NavView_Navigate(NavigationViewItem item)
        {
           
            switch (item.Tag)
            {
                
                case "IPScan":
                    ContentFrame.Navigate(typeof(PageIPScanner));
                   // NavView.Header = "IP Scanner";
                    break;

                case "DNSScan":
                    ContentFrame.Navigate(typeof(PageDNSScanner));
                   // NavView.Header = "DNS Scanner";
                    break;
              
            }
        }

        private void ContentFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {

        }
    }
}
