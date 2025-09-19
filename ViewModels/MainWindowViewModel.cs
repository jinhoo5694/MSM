using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
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

        public event Func<EditProductViewModel, Task<Product?>>? ShowEditProductWindow;
        public event Func<ReduceStockViewModel, Task<int?>>? ShowReduceStockWindow;
        public event Func<AddProductViewModel, Task<Product?>>? ShowAddProductWindow;

        public MainWindowViewModel(IStockService stockService)
        {
            _stockService = stockService;
            _barcode = string.Empty;
            _message = string.Empty;
            _products = new ObservableCollection<ProductViewModel>();

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
                    var products = _stockService.GetAllProducts().ToList();
                    var productToDelete = products.FirstOrDefault(p => p.Barcode == productViewModel.Product.Barcode);
                    if (productToDelete != null)
                    {
                        products.Remove(productToDelete);
                        _stockService.SaveProducts(products);
                        LoadProducts();
                    }
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
            });

            LoadProducts();
        }

        private void LoadProducts()
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