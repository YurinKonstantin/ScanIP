using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using ScanIPPro.Helpers;
using ScanIPPro.Models;
using ScanIPPro.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using WinRT.Interop;

namespace ScanIPPro
{
    public sealed partial class MainWindow : Window
    {
        private enum FilterMode { All, Online, Offline }

        private readonly NetworkScanner _scanner;
        private readonly ContinuousPingService _pingService = new();
        private readonly TracerouteService _tracerouteService = new();
        private CancellationTokenSource _cts;
        private CancellationTokenSource _portScanCts;
        private CancellationTokenSource _tracerouteCts;
        private int _portScanVersion;
        private bool _isScanning;
        private bool _isLoaded;
        private bool _isApplyingFilter;
        private string _searchText = string.Empty;
        private FilterMode _filterMode = FilterMode.All;
        private string _currentStatusKey;
        private object[] _statusArgs = Array.Empty<object>();

        public ObservableCollection<ScanResult> ScanResults { get; } = new();
        private readonly ObservableCollection<ScanResult> _displayedResults = new();
        private readonly ObservableCollection<PortScanResult> _portResults = new();
        private readonly ObservableCollection<PingSample> _pingSamples = new();
        private readonly ObservableCollection<TracerouteHop> _tracerouteHops = new();

        public MainWindow()
        {
            InitializeComponent();
            _scanner = new NetworkScanner();
            Activated += OnWindowActivated;
            ScanResults.CollectionChanged += OnScanResultsChanged;
            LocalizationService.LanguageChanged += OnLanguageChanged;
            _pingService.SampleReceived += OnPingSampleReceived;
        }

        private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
        {
            if (_isLoaded)
                return;

            _isLoaded = true;
            Title = LocalizationService.GetString("AppTitle.Text");
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            ResultsListView.ItemsSource = _displayedResults;
            PortsListView.ItemsSource = _portResults;
            PingResultsList.ItemsSource = _pingSamples;
            TracerouteResultsList.ItemsSource = _tracerouteHops;
            ResultsListView.SelectionChanged += OnDeviceSelectionChanged;

            IpRangeTextBox.Text = NetworkInfoService.GetSuggestedScanRange();
            FilterComboBox.SelectedIndex = 0;
            LoadSettingsUi();
            ApplyTheme(AppSettingsService.Current.Theme);
            ApplyControlLabels();
            SetStatus("Status.Default");
            UpdateEmptyStates();
        }

        private void LoadSettingsUi()
        {
            LanguageComboBox.Items.Clear();
            foreach (var lang in LocalizationService.SupportedLanguages)
            {
                LanguageComboBox.Items.Add(new ComboBoxItem
                {
                    Content = LocalizationService.GetLanguageDisplayName(lang),
                    Tag = lang
                });
            }

            var currentLang = LocalizationService.CurrentLanguage;
            for (int i = 0; i < LanguageComboBox.Items.Count; i++)
            {
                if (LanguageComboBox.Items[i] is ComboBoxItem item && (string)item.Tag == currentLang)
                {
                    LanguageComboBox.SelectedIndex = i;
                    break;
                }
            }

            ThemeComboBox.SelectedIndex = AppSettingsService.Current.Theme switch
            {
                "Light" => 1,
                "Dark" => 2,
                _ => 0
            };

            PingTimeoutBox.Value = AppSettingsService.Current.PingTimeoutMs;
            ConcurrencyBox.Value = AppSettingsService.Current.Concurrency;
            OnlineOnlyToggle.IsOn = AppSettingsService.Current.OnlineOnly;
        }

        private void OnLanguageChanged()
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                Title = LocalizationService.GetString("AppTitle.Text");
                RefreshLocalizedCollections();
                ApplyControlLabels();
                UpdateLocalizedStrings();
                UpdateUI();
            });
        }

        private void RefreshLocalizedCollections()
        {
            foreach (var item in ScanResults)
                item.RefreshLocalizedProperties();

            foreach (var port in _portResults)
                port.RefreshLocalizedProperties();
        }

        private async void OnDeviceSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded)
                return;

            _portScanCts?.Cancel();
            _portScanCts?.Dispose();
            _portScanCts = new CancellationTokenSource();
            var version = ++_portScanVersion;

            var selectedItem = ResultsListView.SelectedItem as ScanResult;
            if (selectedItem == null || !selectedItem.IsOnline)
            {
                _portResults.Clear();
                SelectedDeviceInfo.Text = LocalizationService.GetString("SelectedDeviceInfo.SelectOnline");
                UpdateEmptyStates();
                return;
            }

            SelectedDeviceInfo.Text = LocalizationService.GetString(
                "SelectedDeviceInfo.Scanning", selectedItem.IPAddress, selectedItem.HostName);
            SetStatus("PortScan.Progress", selectedItem.IPAddress);
            MainPivot.SelectedIndex = 1;
            _portResults.Clear();

            try
            {
                var token = _portScanCts.Token;
                var scanner = new PortScanner();
                var progress = new Progress<PortScanResult>(result =>
                {
                    if (version == _portScanVersion)
                        _portResults.Add(result);
                });

                var results = await scanner.ScanPortsWithProgressAsync(
                    selectedItem.IPAddress, 1, 1024, progress, 200, token);

                if (version != _portScanVersion)
                    return;

                var openCount = results.Count(r => r.Status == PortStatus.Open);
                SetStatus("PortScan.Completed", selectedItem.IPAddress, openCount);
            }
            catch (OperationCanceledException)
            {
                if (version == _portScanVersion)
                    SetStatus("PortScan.Cancelled", selectedItem.IPAddress);
            }
            catch (Exception ex)
            {
                if (version == _portScanVersion)
                    SetStatus("PortScan.Error", ex.Message);
            }
            finally
            {
                UpdateEmptyStates();
                UpdateUI();
            }
        }

        private void OnScanResultsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!_isLoaded || _isApplyingFilter)
                return;

            ApplyFilterOptimized(e);
            UpdateEmptyStates();
        }

        private void ApplyFilterOptimized(NotifyCollectionChangedEventArgs e)
        {
            _isApplyingFilter = true;
            try
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    foreach (ScanResult newItem in e.NewItems)
                    {
                        if (ShouldIncludeItem(newItem))
                            _displayedResults.Add(newItem);
                    }
                }
                else if (e.Action == NotifyCollectionChangedAction.Remove)
                {
                    foreach (ScanResult removedItem in e.OldItems)
                        _displayedResults.Remove(removedItem);
                }
                else if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    _displayedResults.Clear();
                }
                else
                {
                    FullRefreshDisplayedResults();
                }
            }
            finally
            {
                _isApplyingFilter = false;
            }

            UpdateDeviceCount();
        }

        private bool ShouldIncludeItem(ScanResult item)
        {
            var statusMatches = _filterMode switch
            {
                FilterMode.Online => item.IsOnline,
                FilterMode.Offline => !item.IsOnline,
                _ => true
            };

            if (!statusMatches)
                return false;

            if (string.IsNullOrEmpty(_searchText))
                return true;

            var searchLower = _searchText.ToLowerInvariant();
            return item.IPAddress.ToLowerInvariant().Contains(searchLower)
                   || item.HostName.ToLowerInvariant().Contains(searchLower)
                   || item.MACAddress.ToLowerInvariant().Contains(searchLower)
                   || item.Vendor.ToLowerInvariant().Contains(searchLower)
                   || item.OpenPorts.ToLowerInvariant().Contains(searchLower);
        }

        private void FullRefreshDisplayedResults()
        {
            var filtered = ScanResults.Where(ShouldIncludeItem).ToList();
            foreach (var item in _displayedResults.Except(filtered).ToList())
                _displayedResults.Remove(item);
            foreach (var item in filtered.Except(_displayedResults).ToList())
                _displayedResults.Add(item);
        }

        private void RecalculateFilter()
        {
            if (!_isLoaded)
                return;

            _isApplyingFilter = true;
            try
            {
                FullRefreshDisplayedResults();
            }
            finally
            {
                _isApplyingFilter = false;
            }

            UpdateDeviceCount();
            UpdateEmptyStates();
        }

        private async void OnScanClicked(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded)
                return;

            if (_isScanning)
            {
                _cts?.Cancel();
                return;
            }

            var input = IpRangeTextBox.Text.Trim();
            if (string.IsNullOrEmpty(input))
            {
                await ShowDialog(LocalizationService.GetString("Dialog.Error.Title"),
                    LocalizationService.GetString("Dialog.Error.InvalidIP"));
                return;
            }

            var ipList = IpRangeParser.Parse(input);
            if (ipList == null || ipList.Count == 0)
            {
                await ShowDialog(LocalizationService.GetString("Dialog.Error.Title"),
                    LocalizationService.GetString("Dialog.Error.InvalidRange"));
                return;
            }

            _portScanCts?.Cancel();
            _portResults.Clear();
            ScanResults.Clear();
            _displayedResults.Clear();

            _isScanning = true;
            ScanProgressBar.Visibility = Visibility.Visible;
            ScanProgressBar.Value = 0;
            SetControlsEnabled(false);
            UpdateUI();

            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            var total = ipList.Count;
            var options = new ScanOptions
            {
                OnlineOnly = AppSettingsService.Current.OnlineOnly,
                PingTimeoutMs = AppSettingsService.Current.PingTimeoutMs,
                Concurrency = AppSettingsService.Current.Concurrency
            };

            try
            {
                SetStatus("Scanning.Progress", total);

                var progress = new Progress<ScanResult>(result => ScanResults.Add(result));
                var scanProgress = new Progress<(int completed, int total)>(p =>
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        ScanProgressBar.Maximum = p.total;
                        ScanProgressBar.Value = p.completed;
                        SetStatus("Scanning.ProgressCount", p.completed, p.total);
                    });
                });

                await _scanner.ScanIpRangeAsync(ipList, progress, scanProgress, options, token);

                var onlineCount = ScanResults.Count(r => r.IsOnline);
                SetStatus("Scanning.Completed", onlineCount, total);

                if (onlineCount > 0)
                {
                    var firstOnline = ScanResults.FirstOrDefault(r => r.IsOnline);
                    if (firstOnline != null)
                        ResultsListView.SelectedItem = firstOnline;
                }
            }
            catch (OperationCanceledException)
            {
                SetStatus("Scanning.Cancelled");
            }
            catch (Exception ex)
            {
                SetStatus("Scanning.Error", ex.Message);
                await ShowDialog(LocalizationService.GetString("Dialog.Error.Title"), ex.Message);
            }
            finally
            {
                _isScanning = false;
                ScanProgressBar.Visibility = Visibility.Collapsed;
                SetControlsEnabled(true);
                UpdateEmptyStates();
                UpdateUI();
            }
        }

        private void SetControlsEnabled(bool enabled)
        {
            IpRangeTextBox.IsEnabled = enabled;
            SearchBox.IsEnabled = enabled;
            FilterComboBox.IsEnabled = enabled;
        }

        private void OnClearClicked(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded)
                return;

            _portScanCts?.Cancel();
            _portResults.Clear();
            ScanResults.Clear();
            _displayedResults.Clear();
            SetStatus("ListCleared");
            UpdateEmptyStates();
            UpdateUI();
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isLoaded)
                return;

            _searchText = SearchBox.Text ?? string.Empty;
            RecalculateFilter();
        }

        private void OnFilterChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded || FilterComboBox.SelectedItem is not ComboBoxItem item)
                return;

            _filterMode = item.Tag?.ToString() switch
            {
                "online" => FilterMode.Online,
                "offline" => FilterMode.Offline,
                _ => FilterMode.All
            };
            RecalculateFilter();
        }

        private void OnClearFilterClicked(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded)
                return;

            SearchBox.Text = string.Empty;
            FilterComboBox.SelectedIndex = 0;
            _searchText = string.Empty;
            _filterMode = FilterMode.All;
            RecalculateFilter();
        }

        private void UpdateUI()
        {
            if (!_isLoaded)
                return;

            ScanButton.Content = _isScanning
                ? LocalizationService.GetString("Scan.Cancel")
                : LocalizationService.GetString("Scan.Start");

            StatusTextBlock.Text = GetStatusMessage();
            StatusTextBlockBottom.Text = GetStatusMessage();
            UpdateDeviceCount();
        }

        private string GetStatusMessage()
        {
            if (string.IsNullOrEmpty(_currentStatusKey))
                return LocalizationService.GetString("Status.Default");
            return LocalizationService.GetString(_currentStatusKey, _statusArgs);
        }

        private void UpdateDeviceCount()
        {
            if (!_isLoaded)
                return;

            DeviceCountTextBlock.Text = LocalizationService.GetString(
                "DeviceCount.Format",
                ScanResults.Count,
                ScanResults.Count(r => r.IsOnline),
                _displayedResults.Count,
                _portResults.Count);
        }

        private void UpdateEmptyStates()
        {
            if (!_isLoaded)
                return;

            EmptyDevicesText.Visibility = _displayedResults.Count == 0
                ? Visibility.Visible : Visibility.Collapsed;
            EmptyPortsText.Visibility = _portResults.Count == 0
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void OnExportClicked(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded)
                return;

            if (ScanResults.Count == 0)
            {
                await ShowDialog(LocalizationService.GetString("ExportDialog.NoTitle"),
                    LocalizationService.GetString("ExportDialog.NoData"));
                return;
            }

            var dialog = new ContentDialog
            {
                Title = LocalizationService.GetString("ExportDialog.Title"),
                XamlRoot = Content.XamlRoot,
                PrimaryButtonText = LocalizationService.GetString("ExportDialog.Primary"),
                SecondaryButtonText = LocalizationService.GetString("ExportDialog.Secondary")
            };

            var formats = new ComboBox
            {
                ItemsSource = new List<string>
                {
                    LocalizationService.GetString("Export.Format.Csv"),
                    LocalizationService.GetString("Export.Format.Json"),
                    LocalizationService.GetString("Export.Format.Txt"),
                    LocalizationService.GetString("Export.Format.Html")
                },
                SelectedIndex = 0,
                Margin = new Thickness(0, 12, 0, 0)
            };

            dialog.Content = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = LocalizationService.GetString("ExportDialog.ChooseFormat") },
                    formats
                }
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return;

            var format = formats.SelectedIndex switch
            {
                1 => ExportService.ExportFormat.Json,
                2 => ExportService.ExportFormat.Txt,
                3 => ExportService.ExportFormat.Html,
                _ => ExportService.ExportFormat.Csv
            };

            var hwnd = WindowNative.GetWindowHandle(this);
            var success = await new ExportService().ExportAsync(ScanResults, format, hwnd);

            if (success)
                SetStatus("ExportDialog.Success");
            else
                await ShowDialog(LocalizationService.GetString("Dialog.Error.Title"),
                    LocalizationService.GetString("ExportDialog.Failure"));
        }

        private void OnThemeToggleClicked(object sender, RoutedEventArgs e)
        {
            var current = AppSettingsService.Current.Theme;
            var next = current == "Dark" ? "Light" : "Dark";
            AppSettingsService.Current.Theme = next;
            AppSettingsService.Save();
            ApplyTheme(next);
            ThemeComboBox.SelectedIndex = next == "Light" ? 1 : 2;
        }

        private void ApplyTheme(string theme)
        {
            ElementTheme elementTheme = theme switch
            {
                "Dark" => ElementTheme.Dark,
                "Light" => ElementTheme.Light,
                _ => ElementTheme.Default
            };

            if (Content is FrameworkElement root)
                root.RequestedTheme = elementTheme;

            ThemeToggleButton.Content = new FontIcon
            {
                Glyph = elementTheme == ElementTheme.Dark ? "\uE706" : "\uE708",
                FontSize = 14
            };
        }

        private void OnNavSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (!_isLoaded)
                return;

            var isSettings = args.IsSettingsSelected;
            ScannerPanel.Visibility = isSettings ? Visibility.Collapsed : Visibility.Visible;
            SettingsPanel.Visibility = isSettings ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnNavItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                ScannerPanel.Visibility = Visibility.Collapsed;
                SettingsPanel.Visibility = Visibility.Visible;
            }
        }

        private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded || LanguageComboBox.SelectedItem is not ComboBoxItem item)
                return;

            var lang = item.Tag as string;
            if (!string.IsNullOrEmpty(lang) && lang != LocalizationService.CurrentLanguage)
            {
                LocalizationService.SetLanguage(lang);
                AppSettingsService.Current.Language = lang;
                AppSettingsService.Save();
            }
        }

        private void OnThemeSettingChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded || ThemeComboBox.SelectedItem is not ComboBoxItem item)
                return;

            var theme = item.Tag as string ?? "System";
            AppSettingsService.Current.Theme = theme;
            ApplyTheme(theme);
        }

        private async void OnSettingsSaveClicked(object sender, RoutedEventArgs e)
        {
            AppSettingsService.Current.PingTimeoutMs = (int)PingTimeoutBox.Value;
            AppSettingsService.Current.Concurrency = (int)ConcurrencyBox.Value;
            AppSettingsService.Current.OnlineOnly = OnlineOnlyToggle.IsOn;
            AppSettingsService.Save();
            SetStatus("Settings.Saved");
            await ShowDialog(LocalizationService.GetString("Settings.Title"),
                LocalizationService.GetString("Settings.Saved"));
        }

        private void OnDeviceClicked(object sender, ItemClickEventArgs e) { }

        private void OnDeviceRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (ResultsListView.SelectedItem is not ScanResult device)
                return;

            var flyout = new MenuFlyout();
            flyout.Items.Add(CreateAction(LocalizationService.GetString("QuickAction.Copy"), () => CopyIp(device)));
            flyout.Items.Add(CreateAction(LocalizationService.GetString("QuickAction.RDP"), () => LaunchUri($"ms-rd://full%20address={device.IPAddress}")));
            flyout.Items.Add(CreateAction(LocalizationService.GetString("QuickAction.HTTP"), () => LaunchUri($"http://{device.IPAddress}")));
            flyout.Items.Add(CreateAction(LocalizationService.GetString("QuickAction.HTTPS"), () => LaunchUri($"https://{device.IPAddress}")));
            flyout.Items.Add(CreateAction(LocalizationService.GetString("QuickAction.SMB"), () => LaunchUri($"file://{device.IPAddress}")));
            flyout.Items.Add(new MenuFlyoutSeparator());
            flyout.Items.Add(CreateAction(LocalizationService.GetString("QuickAction.WoL"), () => _ = SendWoL(device)));
            var favText = FavoritesService.IsFavorite(device.IPAddress)
                ? LocalizationService.GetString("QuickAction.FavoriteRemove")
                : LocalizationService.GetString("QuickAction.Favorite");
            flyout.Items.Add(CreateAction(favText, () =>
            {
                FavoritesService.Toggle(device);
                SetStatus(FavoritesService.IsFavorite(device.IPAddress)
                    ? LocalizationService.GetString("QuickAction.Favorite")
                    : LocalizationService.GetString("QuickAction.FavoriteRemove"));
            }));

            flyout.ShowAt(ResultsListView, e.GetPosition(ResultsListView));
        }

        private static MenuFlyoutItem CreateAction(string text, Action action)
        {
            var item = new MenuFlyoutItem { Text = text };
            item.Click += (_, _) => action();
            return item;
        }

        private void CopyIp(ScanResult device)
        {
            var package = new DataPackage();
            package.SetText(device.IPAddress);
            Clipboard.SetContent(package);
            SetStatus("QuickAction.Copied");
        }

        private static void LaunchUri(string uri)
        {
            try
            {
                Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
            }
            catch { }
        }

        private async Task SendWoL(ScanResult device)
        {
            var ok = await WakeOnLanService.SendAsync(device.MACAddress);
            SetStatus(ok ? "QuickAction.WoLSent" : "QuickAction.WoLFailed");
            if (!ok)
                await ShowDialog(LocalizationService.GetString("Dialog.Error.Title"),
                    LocalizationService.GetString("QuickAction.WoLFailed"));
        }

        private void OnPingStartClicked(object sender, RoutedEventArgs e)
        {
            var target = PingTargetBox.Text?.Trim();
            if (string.IsNullOrEmpty(target))
            {
                target = (ResultsListView.SelectedItem as ScanResult)?.IPAddress;
                if (!string.IsNullOrEmpty(target))
                    PingTargetBox.Text = target;
            }

            if (string.IsNullOrEmpty(target))
            {
                SetStatus("Ping.Waiting");
                return;
            }

            _pingSamples.Clear();
            _pingService.Start(target);
            PingStartButton.IsEnabled = false;
            PingStopButton.IsEnabled = true;
        }

        private void OnPingStopClicked(object sender, RoutedEventArgs e)
        {
            _pingService.Stop();
            PingStartButton.IsEnabled = true;
            PingStopButton.IsEnabled = false;
        }

        private void OnPingSampleReceived(PingSample sample)
        {
            DispatcherQueue.TryEnqueue(() => _pingSamples.Insert(0, sample));
        }

        private async void OnTracerouteStartClicked(object sender, RoutedEventArgs e)
        {
            var target = TracerouteTargetBox.Text?.Trim();
            if (string.IsNullOrEmpty(target))
            {
                target = (ResultsListView.SelectedItem as ScanResult)?.IPAddress;
                if (!string.IsNullOrEmpty(target))
                    TracerouteTargetBox.Text = target;
            }

            if (string.IsNullOrEmpty(target))
            {
                SetStatus("Traceroute.Waiting");
                return;
            }

            _tracerouteCts?.Cancel();
            _tracerouteCts?.Dispose();
            _tracerouteCts = new CancellationTokenSource();
            _tracerouteHops.Clear();
            TracerouteStartButton.IsEnabled = false;
            TracerouteStopButton.IsEnabled = true;

            try
            {
                var hops = await _tracerouteService.TraceAsync(target, 30, _tracerouteCts.Token);
                foreach (var hop in hops)
                    _tracerouteHops.Add(hop);
            }
            catch (OperationCanceledException) { }
            finally
            {
                TracerouteStartButton.IsEnabled = true;
                TracerouteStopButton.IsEnabled = false;
            }
        }

        private void OnTracerouteStopClicked(object sender, RoutedEventArgs e)
        {
            _tracerouteCts?.Cancel();
            TracerouteStartButton.IsEnabled = true;
            TracerouteStopButton.IsEnabled = false;
        }

        private async Task ShowDialog(string title, string content)
        {
            if (!_isLoaded)
                return;

            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = LocalizationService.GetString("Dialog.OK"),
                XamlRoot = Content.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private void SetStatus(string key, params object[] args)
        {
            _currentStatusKey = key;
            _statusArgs = args ?? Array.Empty<object>();
            UpdateUI();
        }

        private void ApplyControlLabels()
        {
            if (!_isLoaded)
                return;

            ToolTipService.SetToolTip(ThemeToggleButton, LocalizationService.GetString("ThemeToggle.ToolTip"));

            if (NavDevicesItem != null)
                NavDevicesItem.Content = LocalizationService.GetString("Navigation.Devices");

            ColumnIpHeader.Text = LocalizationService.GetString("Column.IP");
            ColumnHostHeader.Text = LocalizationService.GetString("Column.Host");
            ColumnMacHeader.Text = LocalizationService.GetString("Column.MAC");
            ColumnVendorHeader.Text = LocalizationService.GetString("Column.Vendor");
            ColumnPortsHeader.Text = LocalizationService.GetString("Column.Ports");
            ColumnRttHeader.Text = LocalizationService.GetString("Column.RTT");
            ColumnStatusHeader.Text = LocalizationService.GetString("Column.Status");

            ColumnPortStatusHeader.Text = LocalizationService.GetString("Column.PortStatus");
            ColumnPortHeader.Text = LocalizationService.GetString("Column.Port");
            ColumnProtocolHeader.Text = LocalizationService.GetString("Column.Protocol");
            ColumnServiceHeader.Text = LocalizationService.GetString("Column.Service");
            ColumnBannerHeader.Text = LocalizationService.GetString("Column.Banner");

            EmptyDevicesText.Text = LocalizationService.GetString("Empty.NoDevices");
            EmptyPortsText.Text = LocalizationService.GetString("Empty.NoPorts");

            PingStartButton.Content = LocalizationService.GetString("Ping.Start");
            PingStopButton.Content = LocalizationService.GetString("Ping.Stop");
            PingTargetBox.PlaceholderText = LocalizationService.GetString("Ping.Target");

            TracerouteStartButton.Content = LocalizationService.GetString("Traceroute.Start");
            TracerouteStopButton.Content = LocalizationService.GetString("Traceroute.Stop");
            TracerouteTargetBox.PlaceholderText = LocalizationService.GetString("Traceroute.Target");

            SettingsTitleText.Text = LocalizationService.GetString("Settings.Title");
            LanguageComboBox.Header = LocalizationService.GetString("Settings.Language");
            ThemeComboBox.Header = LocalizationService.GetString("Settings.Theme");
            ThemeSystemItem.Content = LocalizationService.GetString("Settings.ThemeSystem");
            ThemeLightItem.Content = LocalizationService.GetString("Settings.ThemeLight");
            ThemeDarkItem.Content = LocalizationService.GetString("Settings.ThemeDark");
            PingTimeoutBox.Header = LocalizationService.GetString("Settings.PingTimeout");
            ConcurrencyBox.Header = LocalizationService.GetString("Settings.Concurrency");
            OnlineOnlyToggle.Header = LocalizationService.GetString("Settings.OnlineOnly");
            SettingsSaveButton.Content = LocalizationService.GetString("Settings.Save");
        }

        private void UpdateLocalizedStrings()
        {
            if (string.IsNullOrEmpty(_currentStatusKey))
                _currentStatusKey = "Status.Default";
            UpdateUI();
        }
    }
}
