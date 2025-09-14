using Avalonia.Controls;
using Avalonia.Interactivity;
using MSM.Models;

namespace MSM;

public partial class AddProductWindow : Window
{
    public Product? Product { get; private set; }

    private TextBox? _barcodeTextBox;
    private TextBox? _nameTextBox;
    private TextBox? _quantityTextBox;
    private Button? _saveButton;
    private Button? _cancelButton;

    public AddProductWindow()
    {
        InitializeComponent();
        _barcodeTextBox = this.FindControl<TextBox>("BarcodeTextBox");
        _nameTextBox = this.FindControl<TextBox>("NameTextBox");
        _quantityTextBox = this.FindControl<TextBox>("QuantityTextBox");
        _saveButton = this.FindControl<Button>("SaveButton");
        _cancelButton = this.FindControl<Button>("CancelButton");

        _saveButton!.Click += SaveButton_Click;
        _cancelButton!.Click += CancelButton_Click;
    }

    public AddProductWindow(string barcode) : this()
    {
        _barcodeTextBox.Text = barcode;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(_quantityTextBox.Text, out var quantity))
        {
            Product = new Product
            {
                Barcode = _barcodeTextBox.Text,
                Name = _nameTextBox.Text,
                Quantity = quantity
            };
            Close(Product);
        }
        else
        {
            // Handle invalid quantity input
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
