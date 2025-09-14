using System;
using System.Reactive;
using System.Reactive.Linq; // Add this
using System.Threading.Tasks; // Add this
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MSM.Models;
using MSM.Services;
using ReactiveUI;

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
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        private int _defaultReductionAmount;
        public int DefaultReductionAmount
        {
            get => _defaultReductionAmount;
            set => this.RaiseAndSetIfChanged(ref _defaultReductionAmount, value);
        }

        private string _imagePath;
        public string ImagePath
        {
            get => _imagePath;
            set => this.RaiseAndSetIfChanged(ref _imagePath, value);
        }

        public string Barcode => _originalProduct.Barcode;

        public ReactiveCommand<Unit, Product> SaveCommand { get; } // Change return type to Product
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }
        public ReactiveCommand<Unit, Unit> BrowseImageCommand { get; }

        public Interaction<Unit, string> ShowFilePicker { get; } = new Interaction<Unit, string>();
        public Interaction<Unit, Unit> CloseWindow { get; } = new Interaction<Unit, Unit>(); // Add this for closing the window

        public EditProductViewModel(Product product, IStockService stockService)
        {
            _originalProduct = product;
            _stockService = stockService;

            Name = product.Name;
            DefaultReductionAmount = product.DefaultReductionAmount;
            ImagePath = product.ImagePath;

            SaveCommand = ReactiveCommand.CreateFromTask(Save, outputScheduler: RxApp.MainThreadScheduler);
            CancelCommand = ReactiveCommand.CreateFromTask(Cancel, outputScheduler: RxApp.MainThreadScheduler);
            BrowseImageCommand = ReactiveCommand.CreateFromTask(BrowseImage, outputScheduler: RxApp.MainThreadScheduler);
        }

        private async Task<Product> Save() // Change return type to Task<Product>
        {
            _originalProduct.Name = Name;
            _originalProduct.DefaultReductionAmount = DefaultReductionAmount;
            _originalProduct.ImagePath = ImagePath;

            _stockService.UpdateProduct(_originalProduct);
            await CloseWindow.Handle(Unit.Default); // Close the window
            return _originalProduct; // Return the updated product
        }

        private async Task Cancel() // Change to async Task
        {
            await CloseWindow.Handle(Unit.Default); // Close the window without saving
        }

        private async System.Threading.Tasks.Task BrowseImage()
        {
            var result = await ShowFilePicker.Handle(Unit.Default).FirstAsync(); // Added .FirstAsync()
            if (!string.IsNullOrEmpty(result))
            {
                ImagePath = result;
            }
        }
    }
}