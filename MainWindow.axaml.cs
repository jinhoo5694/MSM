using Avalonia.Controls;
using Avalonia.Interactivity;
using MSM.Models; // Add this using directive
using MSM.Services;

namespace MSM;

public partial class MainWindow : Window
{
    private readonly IStockService _stockService;
    private TextBox? _barcodeTextBox;
    private Button? _searchButton;
    private TextBlock? _messageTextBlock;

    public MainWindow()
    {
        InitializeComponent();
        _stockService = new StockService();

        _barcodeTextBox = this.FindControl<TextBox>("BarcodeTextBox");
        _searchButton = this.FindControl<Button>("SearchButton");
        _messageTextBlock = this.FindControl<TextBlock>("MessageTextBlock");

        _searchButton!.Click += SearchButton_Click;
    }

    private async void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        var barcode = _barcodeTextBox.Text;
        if (string.IsNullOrWhiteSpace(barcode))
        {
            _messageTextBlock.Text = "Please enter a barcode.";
            return;
        }

        var product = _stockService.GetProductByBarcode(barcode);
        if (product != null)
        {
            var reduceStockWindow = new ReduceStockWindow(product);
            var newQuantity = await reduceStockWindow.ShowDialog<int?>(this);

            if (newQuantity.HasValue)
            {
                _stockService.UpdateStock(product.Barcode, newQuantity.Value);
                _messageTextBlock.Text = $"Stock for {product.Name} updated to {newQuantity.Value}.";
            }
        }
        else
        {
            var addProductWindow = new AddProductWindow(barcode);
            var newProduct = await addProductWindow.ShowDialog<Product>(this);

            if (newProduct != null)
            {
                _stockService.AddProduct(newProduct);
                _messageTextBlock.Text = $"New product added: {newProduct.Name}";
            }
        }
    }
}