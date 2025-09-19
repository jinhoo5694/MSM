using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MSM.Commands;
using MSM.Models;
using MSM.Services;

namespace MSM.ViewModels
{
    public class EditProductViewModel : ViewModelBase
    {
        private readonly IStockService _stockService;
        private Product _originalProduct;

        private string _name;
        public string Name
        {
            get => _name;
            set => SetAndRaiseIfChanged(ref _name, value);
        }

        private int _defaultReductionAmount;
        public int DefaultReductionAmount
        {
            get => _defaultReductionAmount;
            set => SetAndRaiseIfChanged(ref _defaultReductionAmount, value);
        }

        private string _imagePath;
        public string ImagePath
        {
            get => _imagePath;
            set => SetAndRaiseIfChanged(ref _imagePath, value);
        }

        public string Barcode => _originalProduct.Barcode;

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand BrowseImageCommand { get; }

        public event Func<Task<string?>>? ShowFilePicker;
        public event Func<Product?, Task>? CloseWindow;

        public EditProductViewModel(Product product, IStockService stockService)
        {
            _originalProduct = product;
            _stockService = stockService;

            Name = product.Name;
            DefaultReductionAmount = product.DefaultReductionAmount;
            ImagePath = product.ImagePath;

            SaveCommand = new AsyncRelayCommand(async _ => await Save(), _ => !string.IsNullOrWhiteSpace(Name) && DefaultReductionAmount > 0);
            CancelCommand = new AsyncRelayCommand(async _ => await Cancel());
            BrowseImageCommand = new AsyncRelayCommand(async _ => await BrowseImage());

            this.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(Name) || args.PropertyName == nameof(DefaultReductionAmount))
                {
                    ((AsyncRelayCommand)SaveCommand).RaiseCanExecuteChanged();
                }
            };
        }

        private async Task Save()
        {
            _originalProduct.Name = Name;
            _originalProduct.DefaultReductionAmount = DefaultReductionAmount;
            _originalProduct.ImagePath = ImagePath;

            _stockService.UpdateProduct(_originalProduct);
            if (CloseWindow != null)
            {
                await CloseWindow.Invoke(_originalProduct);
            }
        }

        private async Task Cancel()
        {
            if (CloseWindow != null)
            {
                await CloseWindow.Invoke(null);
            }
        }

        private async System.Threading.Tasks.Task BrowseImage()
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
    }
}