using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using MSM.Commands;
using MSM.Models;
using MSM.Services;

namespace MSM.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly IStockService _stockService;
        private ObservableCollection<ProductViewModel> _products;
        private string _barcode;
        private string _message;
        private readonly Window _owner;
        private DateTimeOffset? _startDate = DateTimeOffset.Now.AddDays(-7);
        public DateTimeOffset? StartDate
        {
            get => _startDate;
            set
            {
                if (_startDate != value)
                {
                    _startDate = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StartDateFormatted));
                }
            }
        }

        private DateTimeOffset? _endDate = DateTimeOffset.Now;
        public DateTimeOffset? EndDate
        {
            get => _endDate;
            set
            {
                if (_endDate != value)
                {
                    _endDate = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(EndDateFormatted));
                }
            }
        }
        public string StartDateFormatted => StartDate?.ToString("yyyy-MM-dd") ?? "";
        public string EndDateFormatted   => EndDate?.ToString("yyyy-MM-dd") ?? "";
        public ObservableCollection<ProductViewModel> Products
        {
            get => _products;
            set => SetAndRaiseIfChanged(ref _products, value);
        }

        public string Barcode
        {
            get => _barcode;
            set => SetAndRaiseIfChanged(ref _barcode, value);
        }

        public string Message
        {
            get => _message;
            set => SetAndRaiseIfChanged(ref _message, value);
        }

        public ICommand EditProductCommand { get; }
        public ICommand DeleteProductCommand { get; }
        public ICommand SearchCommand { get; }

        public ICommand ExportReportCommand { get; }
        
        public event Func<EditProductViewModel, Task<Product?>>? ShowEditProductWindow;
        public event Func<ReduceStockViewModel, Task<int?>>? ShowReduceStockWindow;
        public event Func<AddProductViewModel, Task<Product?>>? ShowAddProductWindow;
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event Action? RequestFocusBarcode;
        
        public MainWindowViewModel(IStockService stockService, Window owner)
        {
            _stockService = stockService;
            _barcode = string.Empty;
            _message = string.Empty;
            _products = new ObservableCollection<ProductViewModel>();
            _owner = owner;

            ExportReportCommand = new AsyncRelayCommand(ExportReportAsync); 
            
            EditProductCommand = new AsyncRelayCommand(async parameter =>
            {
                if (parameter is ProductViewModel productViewModel && ShowEditProductWindow != null)
                {
                    Product updatedProduct = await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        var editViewModel = new EditProductViewModel(productViewModel.Product, _stockService);
                        return await ShowEditProductWindow.Invoke(editViewModel);
                    });

                    if (updatedProduct != null)
                    {
                        var existingProductViewModel = Products.FirstOrDefault(p => p.Product.Barcode == updatedProduct.Barcode);
                        if (existingProductViewModel != null)
                        {
                            existingProductViewModel.Name = updatedProduct.Name;
                            existingProductViewModel.DefaultReductionAmount = updatedProduct.DefaultReductionAmount;
                            existingProductViewModel.ImagePath = updatedProduct.ImagePath;
                        }
                    }
                }
            });

            
            DeleteProductCommand = new RelayCommand(parameter =>
            {
                if (parameter is ProductViewModel productViewModel)
                {
                    _stockService.DeleteProduct(productViewModel.Product.Barcode);
                    LoadProducts();
                    RequestFocusBarcode?.Invoke();
                }
            });

            SearchCommand = new AsyncRelayCommand(async _ =>
            {
                if (string.IsNullOrWhiteSpace(Barcode))
                {
                    Message = "바코드를 스캔해주세요.";
                    return;
                }

                var product = _stockService.GetProductByBarcode(Barcode);
                if (product != null)
                {
                    if (ShowReduceStockWindow != null)
                    {
                        var reduceStockViewModel = new ReduceStockViewModel(product);
                        var newQuantity = await ShowReduceStockWindow.Invoke(reduceStockViewModel);

                        if (newQuantity.HasValue)
                        {
                            _stockService.UpdateStock(product.Barcode, newQuantity.Value);
                            Message = $"Stock for {product.Name} updated to {newQuantity.Value}.";
                            LoadProducts();
                        }
                    }
                }
                else
                {
                    if (ShowAddProductWindow != null)
                    {
                        var addProductViewModel = new AddProductViewModel(Barcode, _stockService);
                        var newProduct = await ShowAddProductWindow.Invoke(addProductViewModel);

                        if (newProduct != null)
                        {
                            _stockService.AddProduct(newProduct);
                            Message = $"New product added: {newProduct.Name}";
                            LoadProducts();
                        }
                    }
                }

                Barcode = string.Empty;
            });

            LoadProducts();
        }
        
        private async Task ExportReportAsync(object? parameter)
        {
            var dlg = new SaveFileDialog
            {
                Title = "재고 보고서 저장",
                Filters = { new FileDialogFilter { Name = "Excel Files", Extensions = { "xlsx" } } },
                InitialFileName = $"StockReport_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            };

            var result = await dlg.ShowAsync(_owner); // _owner는 MainWindow
            if (!string.IsNullOrEmpty(result))
            {
                _stockService.ExportStockReport(result);
            }
        }

        public void LoadProducts()
        {
            var products = _stockService.GetAllProducts();
            Products.Clear();
            foreach (var product in products ?? Enumerable.Empty<Product>())
            {
                Products.Add(new ProductViewModel(product, EditProductCommand, DeleteProductCommand));
            }
        }
        
       
    }
}