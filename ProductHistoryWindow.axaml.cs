using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MSM.Services;

namespace MSM
{
    public partial class ProductHistoryWindow : Window
    {
        private readonly DataGrid _historyDataGrid;
        private readonly TextBlock _totalCountText;
        private readonly TextBlock _totalChangedText;
        private readonly TextBlock _productNameText;
        private readonly TextBlock _barcodeText;
        private readonly Button _closeButton;

        public ObservableCollection<StockChangeLogEntry> LogEntries { get; } = new();

        public ProductHistoryWindow()
        {
            InitializeComponent();
            _historyDataGrid = this.FindControl<DataGrid>("HistoryDataGrid")!;
            _totalCountText = this.FindControl<TextBlock>("TotalCountText")!;
            _totalChangedText = this.FindControl<TextBlock>("TotalChangedText")!;
            _productNameText = this.FindControl<TextBlock>("ProductNameText")!;
            _barcodeText = this.FindControl<TextBlock>("BarcodeText")!;
            _closeButton = this.FindControl<Button>("CloseButton")!;
            _closeButton.Click += OnCloseClick;
        }

        public ProductHistoryWindow(IStockService stockService, string barcode)
        {
            InitializeComponent();

            _historyDataGrid = this.FindControl<DataGrid>("HistoryDataGrid")!;
            _totalCountText = this.FindControl<TextBlock>("TotalCountText")!;
            _totalChangedText = this.FindControl<TextBlock>("TotalChangedText")!;
            _productNameText = this.FindControl<TextBlock>("ProductNameText")!;
            _barcodeText = this.FindControl<TextBlock>("BarcodeText")!;
            _closeButton = this.FindControl<Button>("CloseButton")!;

            _barcodeText.Text = barcode;
            _historyDataGrid.ItemsSource = LogEntries;
            _closeButton.Click += OnCloseClick;

            var logs = stockService.GetLogsByBarcode(barcode);
            foreach (var log in logs)
            {
                LogEntries.Add(log);
            }

            // Set product name from first log entry (or fallback to product lookup)
            if (LogEntries.Count > 0)
            {
                _productNameText.Text = LogEntries.First().Name;
            }
            else
            {
                var product = stockService.GetProductByBarcode(barcode);
                _productNameText.Text = product?.Name ?? barcode;
            }

            _totalCountText.Text = $"{LogEntries.Count}건";
            var totalChanged = LogEntries.Sum(x => x.ChangedQty);
            _totalChangedText.Text = $"{totalChanged}개";
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnCloseClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
