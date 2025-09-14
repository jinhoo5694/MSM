using System.Reactive;
using MSM.Models;
using ReactiveUI;

namespace MSM.ViewModels
{
    public class ReduceStockViewModel : ViewModelBase
    {
        private Product _product;
        private int _reductionAmount;

        public Product Product
        {
            get => _product;
            set => this.RaiseAndSetIfChanged(ref _product, value);
        }

        public int ReductionAmount
        {
            get => _reductionAmount;
            set => this.RaiseAndSetIfChanged(ref _reductionAmount, value);
        }

        public ReactiveCommand<Unit, int?> OkCommand { get; }
        public ReactiveCommand<Unit, int?> CancelCommand { get; }

        public ReduceStockViewModel(Product product)
        {
            _product = product;
            _reductionAmount = product.DefaultReductionAmount;

            OkCommand = ReactiveCommand.Create(() =>
            {
                if (ReductionAmount > 0 && ReductionAmount <= Product.Quantity)
                {
                    return Product.Quantity - ReductionAmount;
                }
                return (int?)null;
            });

            CancelCommand = ReactiveCommand.Create(() => (int?)null);
        }
    }
}