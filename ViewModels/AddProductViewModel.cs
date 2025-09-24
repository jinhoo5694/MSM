using System.Windows.Input;
using MSM.Commands;
using MSM.Models;
using System;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
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
        private int _alertQuantity;
        private int _safeQuantity;

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
        
        public int AlertQuantity
        {
            get => _alertQuantity;
            set => SetAndRaiseIfChanged(ref _alertQuantity, value);
        }
        
        public int SafeQuantity
        {
            get => _safeQuantity;
            set => SetAndRaiseIfChanged(ref _safeQuantity, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand BrowseImageCommand { get; }

        public event Func<Product, Task>? ProductSaved;
        public event Func<Task>? ProductCancelled;
        private readonly Services.IStockService _stockService;

        public AddProductViewModel(string barcode, Services.IStockService stockService)
        {
            _barcode = barcode;
            _name = string.Empty;
            _quantity = 0;
            _alertQuantity = 0;
            _safeQuantity = 1;
            _imagePath = string.Empty;
            _defaultReductionAmount = 1;
            _stockService = stockService;

            SaveCommand = new RelayCommand(_ => Save());

            CancelCommand = new RelayCommand(_ => CloseWindow(null));
            BrowseImageCommand = new RelayCommand(async _ => await BrowserImage());
        }

        private void Save()
        {
            var newProduct = new Product
            {
                Barcode = Barcode,
                Name = Name,
                Quantity = Quantity,
                ImagePath = ImagePath,
                DefaultReductionAmount = DefaultReductionAmount,
                AlertQuantity = AlertQuantity,
                SafeQuantity = SafeQuantity,
            };
            
            // _stockService.AddProduct(newProduct);

            CloseWindow(newProduct);
        }

        private async Task BrowserImage()
        {
            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
                if (window != null)
                {
                    var dialog = new OpenFileDialog
                    {
                        AllowMultiple = false,
                        Filters = { new FileDialogFilter { Name = "Images", Extensions = { "png", "jpg", "jpeg" } } }
                    };
                    
                    var result = await dialog.ShowAsync(window);
                    if (result != null && result.Length > 0)
                    {
                        ImagePath = result[0];
                    }
                }
            }
        }

        private void CloseWindow(Product? result)
        {
            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
                window?.Close(result);
            }
        }
    }
}