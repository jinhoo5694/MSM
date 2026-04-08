using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using MSM.Commands;
using MSM.Models;
using MSM.Services;

namespace MSM.ViewModels
{
    public class ProductViewModel : ViewModelBase
    {
        private Product _product;
        private readonly IStockService _stockService;
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
                    OnPropertyChanged(nameof(QuantityStatus));
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
                    OnPropertyChanged(nameof(QuantityStatus));
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
                    OnPropertyChanged(nameof(QuantityStatus));
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
                    return "부족";
                if (Quantity < SafeQuantity)
                    return "경고";
                return "안전";
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

        private int _deltaQuantity;
        public int DeltaQuantity
        {
            get => _deltaQuantity;
            set => SetAndRaiseIfChanged(ref _deltaQuantity, value);
        }

        public event Func<string, Task<bool>>? ShowConfirmation;

        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand HistoryCommand { get; }
        public ICommand AddStockCommand { get; }
        public ICommand ReduceStockCommand { get; }

        public ProductViewModel(Product product, ICommand editCommand, ICommand deleteCommand, ICommand historyCommand, IStockService stockService)
        {
            _product = product;
            _stockService = stockService;
            EditCommand = editCommand;
            DeleteCommand = deleteCommand;
            HistoryCommand = historyCommand;

            AddStockCommand = new AsyncRelayCommand(async _ =>
            {
                if (DeltaQuantity <= 0) return;
                var confirmed = ShowConfirmation == null ||
                    await ShowConfirmation.Invoke($"{Name}의 재고를 {DeltaQuantity}개 추가하시겠습니까?\n({Quantity} → {Quantity + DeltaQuantity})");
                if (!confirmed) return;
                Quantity += DeltaQuantity;
                _stockService.UpdateStock(_product.Barcode, Quantity);
                DeltaQuantity = 0;
            });

            ReduceStockCommand = new AsyncRelayCommand(async _ =>
            {
                if (DeltaQuantity <= 0 || DeltaQuantity > Quantity) return;
                var confirmed = ShowConfirmation == null ||
                    await ShowConfirmation.Invoke($"{Name}의 재고를 {DeltaQuantity}개 차감하시겠습니까?\n({Quantity} → {Quantity - DeltaQuantity})");
                if (!confirmed) return;
                Quantity -= DeltaQuantity;
                _stockService.UpdateStock(_product.Barcode, Quantity);
                DeltaQuantity = 0;
            });

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