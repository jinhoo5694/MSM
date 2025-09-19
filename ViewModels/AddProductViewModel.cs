using System.Windows.Input;
using MSM.Commands;
using MSM.Models;
using System;
using System.Threading.Tasks;
using Avalonia.Logging;

namespace MSM.ViewModels
{
    public class AddProductViewModel : ViewModelBase
    {
        private string _barcode;
        private string _name;
        private int _quantity;
        private string _imagePath;
        private int _defaultReductionAmount;

        public string Barcode
        {
            get => _barcode;
            set => SetAndRaiseIfChanged(ref _barcode, value);
        }

        public string Name
        {
            get => _name;
            set => SetAndRaiseIfChanged(ref _name, value);
        }

        public int Quantity
        {
            get => _quantity;
            set => SetAndRaiseIfChanged(ref _quantity, value);
        }

        public string ImagePath
        {
            get => _imagePath;
            set => SetAndRaiseIfChanged(ref _imagePath, value);
        }

        public int DefaultReductionAmount
        {
            get => _defaultReductionAmount;
            set => SetAndRaiseIfChanged(ref _defaultReductionAmount, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public event Func<Product, Task>? ProductSaved;
        public event Func<Task>? ProductCancelled;
        private readonly Services.IStockService _stockService;

        public AddProductViewModel(string barcode, Services.IStockService stockService)
        {
            _barcode = barcode;
            _name = string.Empty;
            _quantity = 0;
            _imagePath = string.Empty;
            _defaultReductionAmount = 1;
            _stockService = stockService;

            SaveCommand = new AsyncRelayCommand(async _ =>
            {
                var newProduct = new Product
                {
                    Barcode = Barcode,
                    Name = Name,
                    Quantity = Quantity,
                    ImagePath = ImagePath,
                    DefaultReductionAmount = DefaultReductionAmount
                };
                System.Diagnostics.Debug.WriteLine($"Barcode: {newProduct.Barcode}");
                _stockService.AddProduct(newProduct);
                if (ProductSaved != null)
                {
                    await ProductSaved.Invoke(newProduct);
                }
            });

            CancelCommand = new AsyncRelayCommand(async _ =>
            {
                if (ProductCancelled != null)
                {
                    await ProductCancelled.Invoke();
                }
            });
        }
    }
}