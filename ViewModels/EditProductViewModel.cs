using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using MSM.Commands;
using MSM.Models;
using MSM.Services;

namespace MSM.ViewModels
{
    public class EditProductViewModel : ViewModelBase
    {
        private readonly IStockService _stockService;
        private readonly Product _originalProduct;

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _alertQuantity;
        public int AlertQuantity
        {
            get => _alertQuantity;
            set
            {
                if (_alertQuantity != value)
                {
                    _alertQuantity = value;
                    OnPropertyChanged();
                }
            }
        }
        
        private int _safeQuantity;
        public int SafeQuantity
        {
            get => _safeQuantity;
            set
            {
                if (_safeQuantity != value)
                {
                    _safeQuantity = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public int DefaultReductionAmount
        {
            get => _defaultReductionAmount;
            set { if (_defaultReductionAmount != value) { _defaultReductionAmount = value; OnPropertyChanged(); } }
        }

        private string _imagePath;
        public string ImagePath
        {
            get => _imagePath;
            set
            {
                if (_imagePath != value)
                {
                    _imagePath = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ProductImage));
                }
            }
        }
        public string Barcode => _originalProduct.Barcode;
        
        private string _name;
        private int _defaultReductionAmount;

        public Bitmap? ProductImage
        {
            get
            {
                if (!string.IsNullOrEmpty(_imagePath) && File.Exists(ImagePath))
                    return new Bitmap(ImagePath);
                return null;
            }
        }

        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged();
                }
            }
        }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand BrowseImageCommand { get; }

        public event Func<Task<string?>>? ShowFilePicker;

        public EditProductViewModel(Product product, IStockService stockService)
        {
            _originalProduct = product;
            _stockService = stockService;

            _name = product.Name ?? string.Empty;
            _defaultReductionAmount = product.DefaultReductionAmount;
            _imagePath = product.ImagePath ?? string.Empty;
            _quantity = product.Quantity;
            _alertQuantity = product.AlertQuantity;
            _safeQuantity = product.SafeQuantity;
            
            SaveCommand = new AsyncRelayCommand(async _ => await Save(), _ => !string.IsNullOrWhiteSpace(Name) && DefaultReductionAmount > 0);
            CancelCommand = new AsyncRelayCommand(async _ => await Cancel());
            BrowseImageCommand = new AsyncRelayCommand(async _ => await BrowseImage());
            
        }

        private async Task Save()
        {
            _originalProduct.Name = Name;
            _originalProduct.DefaultReductionAmount = DefaultReductionAmount;
            _originalProduct.ImagePath = ImagePath;
            _originalProduct.Quantity = Quantity;
            _originalProduct.AlertQuantity = AlertQuantity;
            _originalProduct.SafeQuantity = SafeQuantity;
            
            _stockService.UpdateProduct(_originalProduct);

            CloseWindow(_originalProduct);
        }

        private async Task Cancel()
        {
            CloseWindow(null);
        }

        private async Task BrowseImage()
        {
            if (ShowFilePicker != null)
            {
                var result = await ShowFilePicker.Invoke();
                if (!string.IsNullOrEmpty(result))
                {
                    ImagePath = result;
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