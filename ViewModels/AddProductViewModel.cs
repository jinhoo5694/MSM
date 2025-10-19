using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using MSM.Commands;
using MSM.Models;
using MSM.Services;

namespace MSM.ViewModels
{
    public class AddProductViewModel : ViewModelBase
    {
        private readonly IStockService _stockService;

        private string _name;
        public string Name
        {
            get => _name;
            set => SetAndRaiseIfChanged(ref _name, value);
        }

        private int _defaultReductionAmount = 1;
        public int DefaultReductionAmount
        {
            get => _defaultReductionAmount;
            set => SetAndRaiseIfChanged(ref _defaultReductionAmount, value);
        }

        private int _alertQuantity;
        public int AlertQuantity
        {
            get => _alertQuantity;
            set => SetAndRaiseIfChanged(ref _alertQuantity, value);
        }

        private int _safeQuantity;
        public int SafeQuantity
        {
            get => _safeQuantity;
            set => SetAndRaiseIfChanged(ref _safeQuantity, value);
        }

        private string _imagePath;
        public string ImagePath
        {
            get => _imagePath;
            set
            {
                if (SetAndRaiseIfChanged(ref _imagePath, value))
                {
                    OnPropertyChanged(nameof(ProductImage));
                }
            }
        }

        public string Barcode { get; }

        public Bitmap? ProductImage
        {
            get
            {
                if (!string.IsNullOrEmpty(_imagePath) && File.Exists(ImagePath))
                    return new Bitmap(ImagePath);
                return null;
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand BrowseImageCommand { get; }

        public event Func<Product, Task>? ProductSaved;
        public event Func<Task>? ProductCancelled;

        public AddProductViewModel(string barcode, IStockService stockService)
        {
            Barcode = barcode;
            _stockService = stockService;
            _name = string.Empty;
            _imagePath = string.Empty;

            SaveCommand = new AsyncRelayCommand(async _ => await Save(), _ => !string.IsNullOrWhiteSpace(Name) && DefaultReductionAmount > 0);
            CancelCommand = new RelayCommand(_ => CloseWindow(null));
            BrowseImageCommand = new AsyncRelayCommand(async _ => await BrowseImage());
        }

        private async Task Save()
        {
            var newProduct = new Product
            {
                Barcode = Barcode,
                Name = Name,
                Quantity = 0, // Initial quantity is 0
                DefaultReductionAmount = DefaultReductionAmount,
                AlertQuantity = AlertQuantity,
                SafeQuantity = SafeQuantity,
                ImagePath = ImagePath
            };

            _stockService.AddProduct(newProduct);
            CloseWindow(newProduct);
        }

        private async Task BrowseImage()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var window = desktop.MainWindow;
                var picker = new OpenFileDialog()
                {
                    AllowMultiple = false,
                    Title = "Select Product Image",
                    Filters = { new FileDialogFilter { Name = "Images", Extensions = { "png", "jpg", "jpeg" } } }
                };

                var result = await picker.ShowAsync(window);
                if (result != null && result.Length > 0)
                {
                    ImagePath = result[0];
                }
            }
        }

        private void CloseWindow(Product? product)
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
                window?.Close(product);
            }
        }
    }
}
