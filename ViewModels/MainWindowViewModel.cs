using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MSM.Models;
using MSM.Services;
using ReactiveUI;

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
            set => this.RaiseAndSetIfChanged(ref _products, value);
        }

        public string Barcode
        {
            get => _barcode;
            set => this.RaiseAndSetIfChanged(ref _barcode, value);
        }

        public string Message
        {
            get => _message;
            set => this.RaiseAndSetIfChanged(ref _message, value);
        }

        public ReactiveCommand<ProductViewModel, Unit> EditProductCommand { get; }
        public ReactiveCommand<ProductViewModel, Unit> DeleteProductCommand { get; }
        public ReactiveCommand<Unit, Unit> SearchCommand { get; }
        public Interaction<EditProductViewModel, Product> ShowEditProductWindow { get; } = new Interaction<EditProductViewModel, Product>();
        public Interaction<ReduceStockViewModel, int?> ShowReduceStockWindow { get; } = new Interaction<ReduceStockViewModel, int?>();
        public Interaction<AddProductViewModel, Product> ShowAddProductWindow { get; } = new Interaction<AddProductViewModel, Product>();

        public MainWindowViewModel(IStockService stockService)
        {
            _stockService = stockService;
            _barcode = string.Empty;
            _message = string.Empty;
            _products = new ObservableCollection<ProductViewModel>();

            EditProductCommand = ReactiveCommand.CreateFromTask<ProductViewModel>(async productViewModel =>
            {
                var editViewModel = new EditProductViewModel(productViewModel.Product, _stockService);
                var updatedProduct = await ShowEditProductWindow.Handle(editViewModel);

                if (updatedProduct != null)
                {
                    LoadProducts();
                }
            });

            DeleteProductCommand = ReactiveCommand.CreateFromTask<ProductViewModel>(async productViewModel =>
            {
                var products = _stockService.GetAllProducts().ToList();
                var productToDelete = products.FirstOrDefault(p => p.Barcode == productViewModel.Product.Barcode);
                if (productToDelete != null)
                {
                    products.Remove(productToDelete);
                    _stockService.SaveProducts(products);
                    LoadProducts();
                }
            });

            SearchCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (string.IsNullOrWhiteSpace(Barcode))
                {
                    Message = "Please enter a barcode.";
                    return;
                }

                var product = _stockService.GetProductByBarcode(Barcode);
                if (product != null)
                {
                    var reduceStockViewModel = new ReduceStockViewModel(product);
                    var newQuantity = await ShowReduceStockWindow.Handle(reduceStockViewModel);

                    if (newQuantity.HasValue)
                    {
                        _stockService.UpdateStock(product.Barcode, newQuantity.Value);
                        Message = $"Stock for {product.Name} updated to {newQuantity.Value}.";
                        LoadProducts();
                    }
                }
                else
                {
                    var addProductViewModel = new AddProductViewModel(Barcode);
                    var newProduct = await ShowAddProductWindow.Handle(addProductViewModel);

                    if (newProduct != null)
                    {
                        _stockService.AddProduct(newProduct);
                        Message = $"New product added: {newProduct.Name}";
                        LoadProducts();
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