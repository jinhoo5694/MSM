using System;
using System.Collections.Generic;
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
        private List<ProductViewModel> _allProducts; // Store all products
        private string _barcode;
        private string _message;
        private readonly Window _owner;

        private bool _isDialogOpen;

        private bool _isBarcodeScanningEnabled = true;

        public bool IsBarcodeScanningEnabled
        {
            get => _isBarcodeScanningEnabled;
            set
            {
                if (SetAndRaiseIfChanged(ref _isBarcodeScanningEnabled, value))
                {
                    OnPropertyChanged(nameof(SearchWatermark));
                    FilterProducts(); // Ensure filtering is updated when the mode changes
                    Barcode = String.Empty;
                }
            }
        }

        public string SearchWatermark => IsBarcodeScanningEnabled ? "바코드 입력" : "상품명으로 검색";

        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set
            {
                if (_isDialogOpen != value)
                {
                    _isDialogOpen = value;
                    OnPropertyChanged(nameof(IsDialogOpen));
                }
            }
        }
        
        private DateTimeOffset? _startDate = DateTimeOffset.Now.AddDays(-7);
        public DateTimeOffset? StartDate
        {
            get => _startDate;
            set
            {
                if (_startDate != value)
                {
                    _startDate = value;
                    OnPropertyChanged(nameof(StartDate));
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
                    OnPropertyChanged(nameof(EndDate));
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
            set
            {
                if (SetAndRaiseIfChanged(ref _barcode, value))
                {
                    if (!IsBarcodeScanningEnabled)
                    {
                        FilterProducts();
                    }
                }
            }
        }

        private void FilterProducts()
        {
            if (string.IsNullOrWhiteSpace(Barcode) || IsBarcodeScanningEnabled)
            {
                Products.Clear();
                foreach (var p in _allProducts)
                {
                    Products.Add(p);
                }
            }
            else
            {
                var filtered = _allProducts.Where(p => p.Name.Contains(Barcode, StringComparison.OrdinalIgnoreCase)).ToList();
                Products.Clear();
                foreach (var p in filtered)
                {
                    Products.Add(p);
                }
            }
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
        
        public event Func<EditAndReduceStockViewModel, Task<Product?>>? ShowEditAndReduceStockWindow;
        public event Func<AddProductViewModel, Task<Product?>>? ShowAddProductWindow;

        public event Action? RequestFocusBarcode;
        public event Func<string, Task>? ShowAlert;
        public event Func<string, Task<bool>>? ShowConfirmation;
        public MainWindowViewModel(IStockService stockService, Window owner)
        {
            _stockService = stockService;
            _barcode = string.Empty;
            _message = string.Empty;
            _products = new ObservableCollection<ProductViewModel>();
            _allProducts = new List<ProductViewModel>();
            _owner = owner;

            ExportReportCommand = new AsyncRelayCommand(ExportReportAsync); 
            
            EditProductCommand = new AsyncRelayCommand(async parameter =>
            {
                if (parameter is ProductViewModel productViewModel && ShowEditAndReduceStockWindow != null)
                {
                    Product updatedProduct = await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        var editViewModel = new EditAndReduceStockViewModel(productViewModel.Product, _stockService);
                        return await ShowEditAndReduceStockWindow.Invoke(editViewModel);
                    });

                    if (updatedProduct != null)
                    {
                        var existingProductViewModel = Products.FirstOrDefault(p => p.Product.Barcode == updatedProduct.Barcode);
                        if (existingProductViewModel != null)
                        {
                            existingProductViewModel.Name = updatedProduct.Name;
                            existingProductViewModel.Quantity = updatedProduct.Quantity;
                            existingProductViewModel.DefaultReductionAmount = updatedProduct.DefaultReductionAmount;
                            existingProductViewModel.ImagePath = updatedProduct.ImagePath;
                            existingProductViewModel.AlertQuantity = updatedProduct.AlertQuantity;
                            existingProductViewModel.SafeQuantity = updatedProduct.SafeQuantity;
                        }
                    }
                }
            });

            
            DeleteProductCommand = new AsyncRelayCommand(async parameter =>
            {
                if (parameter is ProductViewModel productViewModel)
                {
                    bool shouldDelete = true;
        
                    // 🚨 확인 요청 이벤트가 구독되어 있다면 호출
                    if (ShowConfirmation != null)
                    {
                        // 사용자에게 메시지를 보여주고 응답을 기다립니다.
                        shouldDelete = await ShowConfirmation.Invoke(
                            $"{productViewModel.Name} (바코드: {productViewModel.Barcode}) 제품을 정말로 삭제하시겠습니까?"
                        );
                    }

                    if (shouldDelete)
                    {
                        _stockService.DeleteProduct(productViewModel.Product.Barcode);
                        Message = $"삭제 완료: {productViewModel.Name}";
                        LoadProducts();
                        RequestFocusBarcode?.Invoke();
                    }
                }
            });

            SearchCommand = new AsyncRelayCommand(async _ =>
            {
                if (!IsBarcodeScanningEnabled) return;

                if (IsDialogOpen) return;
                if (string.IsNullOrWhiteSpace(Barcode))
                {
                    Message = "바코드를 스캔해주세요.";
                    return;
                }

                var product = _stockService.GetProductByBarcode(Barcode);
                if (product != null)
                {
                    if (ShowEditAndReduceStockWindow != null)
                    {
                        var editAndReduceStockViewModel = new EditAndReduceStockViewModel(product, _stockService);
                        var updatedProduct = await ShowEditAndReduceStockWindow.Invoke(editAndReduceStockViewModel);

                        if (updatedProduct != null)
                        {
                            Message = $"{updatedProduct.Name}의 수량이 {updatedProduct.Quantity}로 변경.";
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
                            Message = $"새로운 상품 등록: {newProduct.Name}";
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
                InitialFileName = $"띵니 재고 관리 기록_{DateTime.Now:yyyyMMdd_HH:mm}.xlsx"
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
            _allProducts.Clear();
            foreach (var product in products ?? Enumerable.Empty<Product>())
            {
                _allProducts.Add(new ProductViewModel(product, EditProductCommand, DeleteProductCommand));
            }
            FilterProducts(); // This will apply the current filter
        }
        
       
    }
}