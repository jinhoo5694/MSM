using Avalonia.Controls;
using Avalonia.Interactivity;
using MSM.Models;

namespace MSM;

public partial class ReduceStockWindow : Window
{
    public int? NewQuantity { get; private set; }

    private TextBlock? _nameTextBlock;
    private TextBlock? _currentQuantityTextBlock;
    private TextBox? _reduceByTextBox;
    private Button? _okButton;
    private Button? _cancelButton;

    private readonly Product _product;

    public ReduceStockWindow()
    {
        InitializeComponent();
        _nameTextBlock = this.FindControl<TextBlock>("NameTextBlock");
        _currentQuantityTextBlock = this.FindControl<TextBlock>("CurrentQuantityTextBlock");
        _reduceByTextBox = this.FindControl<TextBox>("ReduceByTextBox");
        _okButton = this.FindControl<Button>("OkButton");
        _cancelButton = this.FindControl<Button>("CancelButton");

        _okButton!.Click += OkButton_Click;
        _cancelButton!.Click += CancelButton_Click;
    }

    public ReduceStockWindow(Product product) : this()
    {
        _product = product;
        _nameTextBlock.Text = _product.Name;
        _currentQuantityTextBlock.Text = _product.Quantity.ToString();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(_reduceByTextBox.Text, out var reduceBy))
        {
            NewQuantity = _product.Quantity - reduceBy;
            Close(NewQuantity);
        }
        else
        {
            // Handle invalid input
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
