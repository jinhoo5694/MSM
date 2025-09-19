using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using MSM.Services;
using MSM.ViewModels;
using MSM.Models;
using MSM.Views;
using System;
using System.Threading.Tasks;

namespace MSM
{
    public partial class MainWindow : Window
    {
        private TextBox? _barcodeTextBox;
        private Button? _searchButton;
        private TextBlock? _messageTextBlock;
        private ItemsControl? _productsItemsControl;

        public MainWindow()
        {
            InitializeComponent();
            // For design time
            if (Design.IsDesignMode)
            {
                DataContext = new MainWindowViewModel(new StockService());
            }
            else
            {
                // In a real application, you'd likely use a DI container to inject IStockService
                // For simplicity, we'll create it directly here.
                DataContext = new MainWindowViewModel(new StockService());
            }

            _barcodeTextBox = this.FindControl<TextBox>("BarcodeTextBox");
            _searchButton = this.FindControl<Button>("SearchButton");
            _messageTextBlock = this.FindControl<TextBlock>("MessageTextBlock");
            _productsItemsControl = this.FindControl<ItemsControl>("ProductsItemsControl");

            _searchButton!.Click += SearchButton_Click;

            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.ShowEditProductWindow += async editViewModel =>
                {
                    var dialog = new EditProductWindow(editViewModel);
                    var result = await dialog.ShowDialog<Product>(this);
                    return result;
                };

                viewModel.ShowAddProductWindow += async addViewModel =>
                {
                    var dialog = new AddProductWindow();
                    dialog.DataContext = addViewModel;
                    var result = await dialog.ShowDialog<Product>(this);
                    return result;
                };

                viewModel.ShowReduceStockWindow += async reduceViewModel =>
                {
                    var dialog = new ReduceStockWindow();
                    dialog.DataContext = reduceViewModel;
                    var result = await dialog.ShowDialog<int?>(this);
                    return result;
                };
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                                viewModel.Barcode = _barcodeTextBox!.Text;
                viewModel.SearchCommand.Execute(null);
                _messageTextBlock!.Text = viewModel.Message;
            }
        }

        private void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel && sender is Button button && button.DataContext is ProductViewModel productViewModel)
            {
                viewModel.DeleteProductCommand.Execute(productViewModel);
            }
        }
    }
}