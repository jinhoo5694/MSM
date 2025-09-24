using System.IO;
using System.Net;
using System.Windows.Input;
using Avalonia.Media.Imaging;
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
                    OnPropertyChanged(nameof(IsAlert));
                    OnPropertyChanged(nameof(IsWarning));
                    OnPropertyChanged(nameof(IsSafe));
                }
            }
        }
        
        public int AlertQuantity
        {
            get => _product.AlertQuantity;
            set
            {
                if (_product.AlertQuantity != value)
                {
                    _product.AlertQuantity = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsAlert));
                    OnPropertyChanged(nameof(IsWarning));
                    OnPropertyChanged(nameof(IsSafe));
                }
            }
        }
        
        public int SafeQuantity
        {
            get => _product.SafeQuantity;
            set
            {
                if (_product.SafeQuantity != value)
                {
                    _product.SafeQuantity = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsAlert));
                    OnPropertyChanged(nameof(IsWarning));
                    OnPropertyChanged(nameof(IsSafe));
                }
            }
        }
        
        public bool IsAlert => Quantity <= AlertQuantity;
        public bool IsWarning => Quantity > AlertQuantity && Quantity < SafeQuantity;
        public bool IsSafe => Quantity >= SafeQuantity;
        
        public string ImagePath
        {
            get => _product.ImagePath;
            set
            {
                if (_product.ImagePath != value)
                {
                    _product.ImagePath = value;
                    OnPropertyChanged();
                    UpdateProductImage();
                }
            }
        }
        
        public string QuantityStatus
        {
            get
            {
                if (Quantity <= AlertQuantity)
                    return "Alert";
                if (Quantity < SafeQuantity)
                    return "Warning";
                return "Safe";
            }
        }

        private Bitmap? _productImage;
        public Bitmap? ProductImage
        {
            get => _productImage;
            private set
            {
                if (_productImage != value)
                {
                    _productImage?.Dispose();
                    _productImage = value;
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

            UpdateProductImage();
        }

        private void UpdateProductImage()
        {
            if (!string.IsNullOrEmpty(_product.ImagePath) && File.Exists(_product.ImagePath))
            {
                try
                {
                    ProductImage = new Bitmap(_product.ImagePath);
                }
                catch
                {
                    ProductImage = null;
                }
            }
            else
            {
                ProductImage = null;
            }
        }
    }
}