using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
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

        public string Name { get; set; }
        public int DefaultReductionAmount { get; set; }
        public string ImagePath { get; set; }
        public string Barcode => _originalProduct.Barcode;
        
        private string _name;
        

        private int _defaultReductionAmount;


        private string _imagePath;



        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand BrowseImageCommand { get; }

        public event Func<Task<string?>>? ShowFilePicker;

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
            
        }

        private async Task Save()
        {
            _originalProduct.Name = Name;
            _originalProduct.DefaultReductionAmount = DefaultReductionAmount;
            _originalProduct.ImagePath = ImagePath;

            _stockService.UpdateProduct(_originalProduct);
        }

        private async Task Cancel()
        {
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
        
    }
}