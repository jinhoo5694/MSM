using System.Windows.Input;
using MSM.Commands;
using MSM.Models;

namespace MSM.ViewModels
{
    public class ReduceStockViewModel : ViewModelBase
    {
        private Product _product;
        private int _reductionAmount;

        public Product Product
        {
            get => _product;
            set => SetAndRaiseIfChanged(ref _product, value);
        }

        public int ReductionAmount
        {
            get => _reductionAmount;
            set => SetAndRaiseIfChanged(ref _reductionAmount, value);
        }

        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }

        public ReduceStockViewModel(Product product)
        {
            _product = product;
            _reductionAmount = product.DefaultReductionAmount;

            OkCommand = new RelayCommand<int?>(parameter =>
            {
                if (ReductionAmount > 0 && ReductionAmount <= Product.Quantity)
                {
                    return Product.Quantity - ReductionAmount;
                }
                return (int?)null;
            });

            CancelCommand = new RelayCommand<int?>(parameter => (int?)null);
        }
    }
}