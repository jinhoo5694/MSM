using System.Reactive;
using MSM.Models;
using ReactiveUI;

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
            set => this.RaiseAndSetIfChanged(ref _barcode, value);
        }

        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        public int Quantity
        {
            get => _quantity;
            set => this.RaiseAndSetIfChanged(ref _quantity, value);
        }

        public string ImagePath
        {
            get => _imagePath;
            set => this.RaiseAndSetIfChanged(ref _imagePath, value);
        }

        public int DefaultReductionAmount
        {
            get => _defaultReductionAmount;
            set => this.RaiseAndSetIfChanged(ref _defaultReductionAmount, value);
        }

        public ReactiveCommand<Unit, Product> SaveCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }

        public AddProductViewModel(string barcode)
        {
            _barcode = barcode;
            _name = string.Empty;
            _quantity = 0;
            _imagePath = string.Empty;
            _defaultReductionAmount = 1;

            SaveCommand = ReactiveCommand.Create(() => new Product
            {
                Barcode = Barcode,
                Name = Name,
                Quantity = Quantity,
                ImagePath = ImagePath,
                DefaultReductionAmount = DefaultReductionAmount
            });

            CancelCommand = ReactiveCommand.Create(() => { });
        }
    }
}