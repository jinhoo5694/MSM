using System.Reactive; // Add this
using MSM.Models;
using ReactiveUI;

namespace MSM.ViewModels
{
    public class ProductViewModel : ViewModelBase
    {
        private Product _product;
        public Product Product => _product; // Add this public property

        public string Barcode => _product.Barcode;
        public string Name => _product.Name;
        public int Quantity => _product.Quantity;
        public string ImagePath => _product.ImagePath;
        public int DefaultReductionAmount => _product.DefaultReductionAmount;

        public ReactiveCommand<ProductViewModel, Unit> EditCommand { get; }
        public ReactiveCommand<ProductViewModel, Unit> DeleteCommand { get; } // Add this

        public ProductViewModel(Product product, ReactiveCommand<ProductViewModel, Unit> editCommand, ReactiveCommand<ProductViewModel, Unit> deleteCommand)
        {
            _product = product;
            EditCommand = editCommand; // Assign the command
            DeleteCommand = deleteCommand; // Assign the command
        }
    }
}