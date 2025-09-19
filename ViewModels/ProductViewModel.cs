using System.Windows.Input;
using MSM.Models;

namespace MSM.ViewModels
{
    public class ProductViewModel : ViewModelBase
    {
        private Product _product;
        public Product Product => _product;

        public string Barcode => _product.Barcode;
        public string Name
        {
            get => _product.Name;
            set
            {
                if (_product.Name != value)
                {
                    _product.Name = value;
                    OnPropertyChanged();
                }
            }
        }
        public int Quantity
        {
            get => _product.Quantity;
            set
            {
                if (_product.Quantity != value)
                {
                    _product.Quantity = value;
                    OnPropertyChanged();
                }
            }
        }
        public string ImagePath
        {
            get => _product.ImagePath;
            set
            {
                if (_product.ImagePath != value)
                {
                    _product.ImagePath = value;
                    OnPropertyChanged();
                }
            }
        }
        public int DefaultReductionAmount
        {
            get => _product.DefaultReductionAmount;
            set
            {
                if (_product.DefaultReductionAmount != value)
                {
                    _product.DefaultReductionAmount = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public ProductViewModel(Product product, ICommand editCommand, ICommand deleteCommand)
        {
            _product = product;
            EditCommand = editCommand;
            DeleteCommand = deleteCommand;
        }
    }
}