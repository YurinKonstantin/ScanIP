using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
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
    public sealed partial class PageSetting : Page
    {
        public PageSetting()
        {
            this.InitializeComponent();
            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            String localValue = localSettings.Values["langsetting"] as string;
            int x = 0;
            if (localSettings == null || localValue == String.Empty)
            {
                localValue = "Auto";
                Option1RadioButton.IsChecked=true;
            }
            if (localValue == "Русский")
            {
                Windows.ApplicationModel.Resources.Core.ResourceContext.SetGlobalQualifierValue("Language", "ru-RU");
                Option3RadioButton.IsChecked = true;
                x = 2;
            }
            if (localValue == "English")
            {
                Windows.ApplicationModel.Resources.Core.ResourceContext.SetGlobalQualifierValue("Language", "en-US");
                Option2RadioButton.IsChecked = true;
                x = 1;
            }
            if (localValue == "Deutsch")
            {
                Windows.ApplicationModel.Resources.Core.ResourceContext.SetGlobalQualifierValue("Language", "de-DE");
                Option4RadioButton.IsChecked = true;
                x = 3;
            }
            if (localValue == "Japanese")
            {
                Windows.ApplicationModel.Resources.Core.ResourceContext.SetGlobalQualifierValue("Language", "ja-JP");
                Option5RadioButton.IsChecked = true;
                x = 4;
            }
            if (localValue == "French")
            {
                Windows.ApplicationModel.Resources.Core.ResourceContext.SetGlobalQualifierValue("Language", "fr-FR");
                Option6RadioButton.IsChecked = true;
                x = 5;
            }


        }

        private void Option1RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = sender as RadioButton;
            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (rb != null && rb.Tag!=null)
            {
               
                switch (rb.Tag.ToString())
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
                    case "Русский":
                        localSettings.Values["Langsetting"] = "Русский";
                        Windows.ApplicationModel.Resources.Core.ResourceContext.SetGlobalQualifierValue("Language", "ru-RU");
                        this.InitializeComponent();
                        break;
                    case "Deutsch":
                        localSettings.Values["Langsetting"] = "Deutsch";
                        Windows.ApplicationModel.Resources.Core.ResourceContext.SetGlobalQualifierValue("Language", "de-DE");
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
        }
    }
}
