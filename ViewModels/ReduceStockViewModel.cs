using System.Linq;
using System.Net.Mime;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using MSM.Commands;
using MSM.Models;
using MSM.Services;

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

            OkCommand = new RelayCommand(_ =>
            {
                if (ReductionAmount > 0 && ReductionAmount <= Product.Quantity)
                {
                    CloseWindow(Product.Quantity - ReductionAmount);
                }
                else
                {
                    CloseWindow(null);
                }
            });
            CancelCommand = new RelayCommand(_ => CloseWindow(null));
            
        }

        private void CloseWindow(int? result)
        {
            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
                window?.Close(result);
            }
        }
    }
}