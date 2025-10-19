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
    public class EditAndReduceStockViewModel : ViewModelBase
    {
        private readonly IStockService _stockService;
        private readonly Product _originalProduct;

        private string _name;
        public string Name
        {
            get => _name;
            set => SetAndRaiseIfChanged(ref _name, value);
        }

        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set => SetAndRaiseIfChanged(ref _quantity, value);
        }

        private int _defaultReductionAmount;
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

        public string Barcode => _originalProduct.Barcode;

        public Bitmap? ProductImage
        {
            get
            {
                if (!string.IsNullOrEmpty(_imagePath) && File.Exists(ImagePath))
                    return new Bitmap(ImagePath);
                return null;
            }
        }

        private int _reductionAmount;
        public int ReductionAmount
        {
            get => _reductionAmount;
            set => SetAndRaiseIfChanged(ref _reductionAmount, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand BrowseImageCommand { get; }
        public ICommand ReduceAndSaveCommand { get; }

        public event Func<Task<string?>>? ShowFilePicker;

        public EditAndReduceStockViewModel(Product product, IStockService stockService)
        {
            _originalProduct = product;
            _stockService = stockService;

            _name = product.Name ?? string.Empty;
            _quantity = product.Quantity;
            _defaultReductionAmount = product.DefaultReductionAmount;
            _alertQuantity = product.AlertQuantity;
            _safeQuantity = product.SafeQuantity;
            _imagePath = product.ImagePath ?? string.Empty;
            _reductionAmount = product.DefaultReductionAmount;

            SaveCommand = new AsyncRelayCommand(async _ => await Save(false));
            ReduceAndSaveCommand = new AsyncRelayCommand(async _ => await Save(true));
            CancelCommand = new RelayCommand(_ => CloseWindow(null));
            BrowseImageCommand = new AsyncRelayCommand(async _ => await BrowseImage());
        }

        private async Task Save(bool reduceStock)
        {
            _originalProduct.Name = Name;
            _originalProduct.DefaultReductionAmount = DefaultReductionAmount;
            _originalProduct.ImagePath = ImagePath;
            _originalProduct.AlertQuantity = AlertQuantity;
            _originalProduct.SafeQuantity = SafeQuantity;

            if (reduceStock)
            {
                if (ReductionAmount > 0 && ReductionAmount <= _originalProduct.Quantity)
                {
                    _originalProduct.Quantity -= ReductionAmount;
                }
            }
            else
            {
                _originalProduct.Quantity = Quantity;
            }

            _stockService.UpdateProduct(_originalProduct);
            CloseWindow(_originalProduct);
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

        private void CloseWindow(Product? result)
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            { 
                var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
                window?.Close(result);
            }
        }
    }
}
