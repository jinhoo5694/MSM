using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using MSM.Services;
using MSM.ViewModels;
using MSM.Models;
using MSM.Views;
using System;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Threading;

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
                this.Opened += (_, _) =>
                {
                    _barcodeTextBox?.Focus();
                };
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
                    var dialog = new EditProductWindow();
                    dialog.DataContext = editViewModel;
                    var result = await dialog.ShowDialog<Product>(this);
                    _barcodeTextBox?.Focus();
                    return result;
                };

                viewModel.ShowAddProductWindow += async addViewModel =>
                {
                    var dialog = new AddProductWindow();
                    dialog.DataContext = addViewModel;
                    var result = await dialog.ShowDialog<Product>(this);
                    _barcodeTextBox?.Focus();
                    return result;
                };

                viewModel.ShowReduceStockWindow += async reduceViewModel =>
                {
                    var dialog = new ReduceStockWindow();
                    dialog.DataContext = reduceViewModel;
                    var result = await dialog.ShowDialog<int?>(this);
                    _barcodeTextBox?.Focus();
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
        
        private void BarcodeTextBox_KeyUp(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (DataContext is MainWindowViewModel vm)
                {
                    if (vm.SearchCommand?.CanExecute(null) == true)
                    {
                        vm.SearchCommand.Execute(null);
                    }
                }
                
                Dispatcher.UIThread.Post(() =>
                {
                    _barcodeTextBox?.Focus();
                }, DispatcherPriority.Background);

                // 포커스 유지
                _barcodeTextBox?.Focus();
            }
        }
    }
}