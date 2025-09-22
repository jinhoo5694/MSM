using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using MSM.Services;
using MSM.ViewModels;
using MSM.Models;
using MSM.Views;
using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input;
using Avalonia.Threading;
using Path = System.IO.Path;

namespace MSM
{
    public partial class MainWindow : Window
    {
        private TextBox? _barcodeTextBox;
        private TextBlock? _messageTextBlock;
        private ItemsControl? _productsItemsControl;
        private readonly IStockService _stockService;
        private readonly DispatcherTimer _autoSaveTimer;
        private string? _autoSaveDirectory;

        public MainWindow()
        {
            InitializeComponent();

            _barcodeTextBox = this.FindControl<TextBox>("BarcodeTextBox");
            _messageTextBlock = this.FindControl<TextBlock>("MessageTextBlock");
            _productsItemsControl = this.FindControl<ItemsControl>("ProductsItemsControl");
            var startPicker = this.FindControl<DatePicker>("StartDatePicker");
            var endPicker = this.FindControl<DatePicker>("EndDatePicker");

            if (startPicker != null)
                startPicker.SelectedDateChanged += OnDateChanged;

            if (endPicker != null)
                endPicker.SelectedDateChanged += OnDateChanged;
            // StockService를 한 번만 생성해서 ViewModel과 공유
            _stockService = new StockService("stock.xlsx");

            if (Design.IsDesignMode)
            {
                DataContext = new MainWindowViewModel(_stockService, this);
            }
            else
            {
                DataContext = new MainWindowViewModel(_stockService, this);
                this.Opened += (_, _) =>
                {
                    _barcodeTextBox?.Focus();
                };
            }

            _autoSaveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(1)
            };
            _autoSaveTimer.Tick += AutoSaveTimer_Tick;
            _autoSaveTimer.Start();

            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.ShowEditProductWindow += async editViewModel =>
                {
                    viewModel.IsDialogOpen = true;
                    var dialog = new EditProductWindow { DataContext = editViewModel };
                    var result = await dialog.ShowDialog<Product>(this);
                    viewModel.IsDialogOpen = false;
                    if (result != null)
                    {
                        viewModel.LoadProducts();
                    }
                    _barcodeTextBox?.Focus();
                    return result;
                };

                viewModel.ShowAddProductWindow += async addViewModel =>
                {
                    viewModel.IsDialogOpen = true;
                    var dialog = new AddProductWindow { DataContext = addViewModel };
                    var result = await dialog.ShowDialog<Product>(this);
                    viewModel.IsDialogOpen = false;
                    _barcodeTextBox?.Focus();
                    return result;
                };

                viewModel.ShowReduceStockWindow += async reduceViewModel =>
                {
                    viewModel.IsDialogOpen = true;
                    var dialog = new ReduceStockWindow { DataContext = reduceViewModel };
                    var result = await dialog.ShowDialog<int?>(this);
                    viewModel.IsDialogOpen = false;
                    _barcodeTextBox?.Focus();
                    return result;
                };

                viewModel.RequestFocusBarcode += () =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        _barcodeTextBox?.Focus();
                        if (_barcodeTextBox != null)
                            _barcodeTextBox.CaretIndex = +_barcodeTextBox.Text?.Length ?? 0;
                    }, DispatcherPriority.Background);
                };

                viewModel.ShowAlert += async (message) =>
                {
                    var dlg = new Window
                    {
                        Title = "알림",
                        Width = 300,
                        Height = 150
                    };

                    var okButton = new Button
                    {
                        Content = "확인",
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Width = 60,
                        Margin = new Thickness(0, 10, 0, 0)
                    };

                    okButton.Click += (_, __) => dlg.Close();

                    dlg.Content = new StackPanel
                    {
                        Margin = new Thickness(10),
                        Children =
                        {
                            new TextBlock { Text = message, Margin = new Thickness(0,0,0,10) },
                            okButton
                        }
                    };

                    await dlg.ShowDialog(this);

                    // Alert 닫힌 뒤에도 SearchBox에 포커스 맞추기
                    _barcodeTextBox?.Focus();
                    if (_barcodeTextBox != null)
                        _barcodeTextBox.CaretIndex = _barcodeTextBox.Text?.Length ?? 0;
                };
            }
        }

        private void AutoSaveTimer_Tick(object? sender, EventArgs e)
        {
            var now = DateTime.Now;
            if (_autoSaveDirectory != null && now.Hour == 2 && now.Minute == 0)
            {
                string fileName = $"띵니_재고관리내역_{now:yyyyMMdd_HHmm}.xlsx";
                string path = Path.Combine(_autoSaveDirectory, fileName);
                _stockService.ExportStockReport(path);
            }
        }

        private async void OnExportReportClick(object? sender, RoutedEventArgs e)
        {
            await ExportReportManualAsync();
        }

        public async Task ExportReportManualAsync()
        {
            var sfd = new SaveFileDialog
            {
                Title = "재고 보고서 저장",
                Filters = { new FileDialogFilter { Name = "Excel Files", Extensions = { "xlsx" } } },
                InitialFileName = $"띵니_재고관리내역_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            var result = await sfd.ShowAsync(this);
            if (!string.IsNullOrEmpty(result))
            {
                _stockService.ExportStockReport(result);
                _autoSaveDirectory = Path.GetDirectoryName(result);

                try { this.Activate(); } catch { /* 무시 */ }

                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await Task.Delay(50);
                    _barcodeTextBox?.Focus();
                    if (_barcodeTextBox != null)
                        _barcodeTextBox.CaretIndex = _barcodeTextBox.Text?.Length ?? 0;
                }, DispatcherPriority.Background);
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void BarcodeTextBox_KeyUp(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (DataContext is MainWindowViewModel vm)
                {
                    if (vm.SearchCommand?.CanExecute(null) == true)
                    {
                        vm.SearchCommand.Execute(null);
                    }
                }

                Dispatcher.UIThread.Post(() =>
                {
                    _barcodeTextBox?.Focus();
                }, DispatcherPriority.Background);

                _barcodeTextBox?.Focus();
            }
        }
        private void OnDateChanged(object? sender, DatePickerSelectedValueChangedEventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _barcodeTextBox?.Focus();
                if (_barcodeTextBox != null)
                    _barcodeTextBox.CaretIndex = _barcodeTextBox.Text?.Length ?? 0;
            }, DispatcherPriority.Background);
        }

    }
}
