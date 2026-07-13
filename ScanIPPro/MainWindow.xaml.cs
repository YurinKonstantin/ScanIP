using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using ScanIPPro.Helpers;
using ScanIPPro.Models;
using ScanIPPro.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace ScanIPPro
{
    public sealed partial class MainWindow : Window
    {
        private readonly NetworkScanner _scanner;
        private CancellationTokenSource _cts;
        private CancellationTokenSource _portScanCts;
        private bool _isScanning;
        private string _searchText = string.Empty;
        private string _filterStatus = "Все устройства";
        private bool _isLoaded = false;
        private bool _isApplyingFilter = false;
        private ScanResult _selectedDevice;

        // Основная коллекция с результатами
        public ObservableCollection<ScanResult> ScanResults { get; } = new();

        // Отфильтрованная коллекция для отображения
        private readonly ObservableCollection<ScanResult> _displayedResults = new();

        // Коллекция для портов
        private readonly ObservableCollection<PortScanResult> _portResults = new();

        public string StatusMessage { get; private set; } = "Готов к сканированию";

        public MainWindow()
        {
            this.InitializeComponent();

            _scanner = new NetworkScanner();

            this.Activated += OnWindowActivated;
            ScanResults.CollectionChanged += OnScanResultsChanged;
        }

        private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
        {
            if (_isLoaded)
                return;

            _isLoaded = true;

            Title = "ScanIP Pro";
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            ResultsListView.ItemsSource = _displayedResults;
            PortsListView.ItemsSource = _portResults;

            // Подписываемся на событие выбора элемента
            ResultsListView.SelectionChanged += OnDeviceSelectionChanged;

            LoadTheme();
            UpdateUI();
        }

        // Обработчик выбора устройства в списке
        private async void OnDeviceSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded || DeviceCountTextBlock == null)
                return;

            // Отменяем предыдущее сканирование портов
            _portScanCts?.Cancel();
            _portScanCts?.Dispose();
            _portScanCts = new CancellationTokenSource();

            // Получаем выбранное устройство
            var selectedItem = ResultsListView.SelectedItem as ScanResult;
            if (selectedItem == null || !selectedItem.IsOnline)
            {
                _portResults.Clear();
                _selectedDevice = null;
                SelectedDeviceInfo.Text = "Выберите онлайн-устройство в списке для сканирования портов";
                return;
            }

            _selectedDevice = selectedItem;
            SelectedDeviceInfo.Text = $"Сканирование портов для: {selectedItem.IPAddress} ({selectedItem.HostName})";
            StatusMessage = $"Сканирование портов для {selectedItem.IPAddress}...";
            UpdateUI();

            // Переключаемся на вкладку портов
            MainPivot.SelectedIndex = 1;

            // Очищаем предыдущие результаты портов
            _portResults.Clear();

            try
            {
                var token = _portScanCts.Token;
                var scanner = new PortScanner();

                // Сканируем топ-100 портов для выбранного устройства
                var progress = new Progress<PortScanResult>(result =>
                {
                    // Добавляем результат в коллекцию
                    _portResults.Add(result);
                });

                var results = await scanner.ScanPortsWithProgressAsync(
                    selectedItem.IPAddress,
                    1,
                    1024,
                    progress,
                    200,
                    token);

                var openCount = results.Count(r => r.Status == PortStatus.Open);
                StatusMessage = $"Порты для {selectedItem.IPAddress}: найдено {openCount} открытых портов";
            }
            catch (OperationCanceledException)
            {
                StatusMessage = $"Сканирование портов для {selectedItem.IPAddress} отменено";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка сканирования портов: {ex.Message}";
            }
            finally
            {
                UpdateUI();
            }
        }

        private void OnScanResultsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!_isLoaded || DeviceCountTextBlock == null || _isApplyingFilter)
                return;

            ApplyFilterOptimized(e);
        }

        // Оптимизированная фильтрация
        private void ApplyFilterOptimized(NotifyCollectionChangedEventArgs e)
        {
            if (!_isLoaded || DeviceCountTextBlock == null)
                return;

            _isApplyingFilter = true;

            try
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    foreach (ScanResult newItem in e.NewItems)
                    {
                        if (ShouldIncludeItem(newItem))
                        {
                            _displayedResults.Add(newItem);
                        }
                    }
                }
                else if (e.Action == NotifyCollectionChangedAction.Remove)
                {
                    foreach (ScanResult removedItem in e.OldItems)
                    {
                        _displayedResults.Remove(removedItem);
                    }
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
            bool statusMatches = _filterStatus switch
            {
                "Только онлайн" => item.IsOnline,
                "Только офлайн" => !item.IsOnline,
                _ => true
            };

            if (!statusMatches)
                return false;

            if (!string.IsNullOrEmpty(_searchText))
            {
                var searchLower = _searchText.ToLower();
                return item.IPAddress.ToLower().Contains(searchLower) ||
                       item.HostName.ToLower().Contains(searchLower) ||
                       item.MACAddress.ToLower().Contains(searchLower) ||
                       item.Vendor.ToLower().Contains(searchLower) ||
                       item.OpenPorts.ToLower().Contains(searchLower);
            }

            return true;
        }

        private void FullRefreshDisplayedResults()
        {
            var filtered = ScanResults.Where(ShouldIncludeItem).ToList();

            var toRemove = _displayedResults.Except(filtered).ToList();
            var toAdd = filtered.Except(_displayedResults).ToList();

            foreach (var item in toRemove)
            {
                _displayedResults.Remove(item);
            }

            foreach (var item in toAdd)
            {
                _displayedResults.Add(item);
            }
        }

        private void RecalculateFilter()
        {
            if (!_isLoaded || DeviceCountTextBlock == null)
                return;

            _isApplyingFilter = true;

            try
            {
                var filtered = ScanResults.Where(ShouldIncludeItem).ToList();

                var toRemove = _displayedResults.Except(filtered).ToList();
                var toAdd = filtered.Except(_displayedResults).ToList();

                foreach (var item in toRemove)
                {
                    _displayedResults.Remove(item);
                }

                foreach (var item in toAdd)
                {
                    _displayedResults.Add(item);
                }
            }
            finally
            {
                _isApplyingFilter = false;
            }

            UpdateDeviceCount();
        }

        private async void OnScanClicked(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded || DeviceCountTextBlock == null)
                return;

            if (_isScanning)
            {
                _cts?.Cancel();
                return;
            }

            var input = IpRangeTextBox.Text.Trim();
            if (string.IsNullOrEmpty(input))
            {
                await ShowDialog("Ошибка", "Введите IP-адрес или диапазон (например, 192.168.1.1-254)");
                return;
            }

            var ipList = IpRangeParser.Parse(input);
            if (ipList == null || !ipList.Any())
            {
                await ShowDialog("Ошибка", "Неверный формат IP-адреса или диапазона");
                return;
            }

            // Отменяем сканирование портов
            _portScanCts?.Cancel();
            _portResults.Clear();

            ScanResults.Clear();
            _displayedResults.Clear();

            _isScanning = true;
            UpdateUI();

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try
            {
                StatusMessage = $"Сканирование {ipList.Count} адресов...";
                UpdateUI();

                var progress = new Progress<ScanResult>(result =>
                {
                    ScanResults.Add(result);
                });

                await _scanner.ScanIpRangeAsync(ipList, progress, token);

                var onlineCount = ScanResults.Count(r => r.IsOnline);
                StatusMessage = $"Сканирование завершено. Найдено {onlineCount} устройств из {ipList.Count}.";

                // Автоматически выбираем первое онлайн устройство
                if (onlineCount > 0)
                {
                    var firstOnline = ScanResults.FirstOrDefault(r => r.IsOnline);
                    if (firstOnline != null)
                    {
                        ResultsListView.SelectedItem = firstOnline;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Сканирование отменено пользователем.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка: {ex.Message}";
                await ShowDialog("Ошибка", ex.Message);
            }
            finally
            {
                _isScanning = false;
                UpdateUI();
            }
        }

        private void OnClearClicked(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded || DeviceCountTextBlock == null)
                return;

            _portScanCts?.Cancel();
            _portResults.Clear();

            ScanResults.Clear();
            _displayedResults.Clear();
            StatusMessage = "Список очищен";
            UpdateUI();
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isLoaded || DeviceCountTextBlock == null)
                return;

            _searchText = SearchBox.Text;
            RecalculateFilter();
        }

        private void OnFilterChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded || DeviceCountTextBlock == null)
                return;

            if (FilterComboBox.SelectedItem is ComboBoxItem item)
            {
                _filterStatus = item.Content.ToString();
                RecalculateFilter();
            }
        }

        private void OnClearFilterClicked(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded || DeviceCountTextBlock == null)
                return;

            SearchBox.Text = string.Empty;
            FilterComboBox.SelectedIndex = 0;
            _searchText = string.Empty;
            _filterStatus = "Все устройства";
            RecalculateFilter();
        }

        private void UpdateUI()
        {
            if (!_isLoaded)
                return;

            if (ScanButton != null)
                ScanButton.Content = _isScanning ? "Отмена" : "Сканировать";

            if (StatusTextBlock != null)
                StatusTextBlock.Text = StatusMessage;

            if (StatusTextBlockBottom != null)
                StatusTextBlockBottom.Text = StatusMessage;

            UpdateDeviceCount();
        }

        private void UpdateDeviceCount()
        {
            if (!_isLoaded || DeviceCountTextBlock == null)
                return;

            var total = ScanResults.Count;
            var online = ScanResults.Count(r => r.IsOnline);
            var displayed = _displayedResults.Count;
            var ports = _portResults.Count;
            DeviceCountTextBlock.Text = $"Устройств: {total} | Онлайн: {online} | Отображено: {displayed} | Портов: {ports}";
        }

        private async void OnExportClicked(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded || DeviceCountTextBlock == null)
                return;

            if (ScanResults.Count == 0)
            {
                await ShowDialog("Экспорт", "Нет данных для экспорта. Сначала выполните сканирование.");
                return;
            }

            var dialog = new ContentDialog
            {
                Title = "Выберите формат экспорта",
                XamlRoot = this.Content.XamlRoot,
                PrimaryButtonText = "Экспортировать",
                SecondaryButtonText = "Отмена"
            };

            var formats = new ComboBox
            {
                ItemsSource = new List<string> { "CSV", "JSON", "TXT", "HTML" },
                SelectedIndex = 0,
                Margin = new Thickness(0, 12, 0, 0)
            };

            dialog.Content = new StackPanel
            {
                Children = {
                    new TextBlock { Text = "Выберите формат файла:" },
                    formats
                }
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var format = formats.SelectedItem.ToString() switch
                {
                    "CSV" => ExportService.ExportFormat.Csv,
                    "JSON" => ExportService.ExportFormat.Json,
                    "TXT" => ExportService.ExportFormat.Txt,
                    "HTML" => ExportService.ExportFormat.Html,
                    _ => ExportService.ExportFormat.Csv
                };

                var exportService = new ExportService();
                var success = await exportService.ExportAsync(ScanResults, format);

                if (success)
                {
                    StatusMessage = "Экспорт выполнен успешно";
                    UpdateUI();
                }
                else
                {
                    await ShowDialog("Ошибка", "Не удалось экспортировать данные.");
                }
            }
        }

        private bool _isDarkTheme = false;

        private void OnThemeToggleClicked(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded || DeviceCountTextBlock == null)
                return;

            _isDarkTheme = !_isDarkTheme;
            var theme = _isDarkTheme ? ElementTheme.Dark : ElementTheme.Light;

            // Применяем тему ко всему приложению
            ((FrameworkElement)this.Content).RequestedTheme = theme;

            // Сохраняем настройку
            ApplicationData.Current.LocalSettings.Values["Theme"] = _isDarkTheme ? "Dark" : "Light";
            ThemeToggleButton.Content = _isDarkTheme ? "☀️" : "🌓";

            // Принудительно обновляем фон вкладок
            MainPivot.Background = theme == ElementTheme.Dark ?
                new SolidColorBrush(Microsoft.UI.Colors.Black) :
                new SolidColorBrush(Microsoft.UI.Colors.White);
        }

        private void LoadTheme()
        {
            if (!_isLoaded || DeviceCountTextBlock == null)
                return;

            if (ApplicationData.Current.LocalSettings.Values["Theme"] is string theme)
            {
                _isDarkTheme = theme == "Dark";
                var elementTheme = _isDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
                ((FrameworkElement)this.Content).RequestedTheme = elementTheme;
                ThemeToggleButton.Content = _isDarkTheme ? "☀️" : "🌓";

                // Обновляем фон вкладок
                MainPivot.Background = elementTheme == ElementTheme.Dark ?
                    new SolidColorBrush(Microsoft.UI.Colors.Black) :
                    new SolidColorBrush(Microsoft.UI.Colors.White);
            }
        }

        private async Task ShowDialog(string title, string content)
        {
            if (!_isLoaded || DeviceCountTextBlock == null)
                return;

            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}