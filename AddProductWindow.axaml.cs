using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MSM.ViewModels;
using MSM.Models;
using System.Threading.Tasks;

namespace MSM
{
    public partial class AddProductWindow : Window
    {
        public AddProductWindow()
        {
            InitializeComponent();
        }

        public AddProductWindow(string barcode) : this()
        {
            if (DataContext is AddProductViewModel viewModel)
            {
                viewModel.ProductSaved += (product) =>
                {
                    Close(product);
                    return Task.CompletedTask;
                };

                viewModel.ProductCancelled += () =>
                {
                    Close(null);
                    return Task.CompletedTask;
                };
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}