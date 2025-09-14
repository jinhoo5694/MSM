using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MSM.ViewModels;
using MSM.Models;

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
            DataContext = new AddProductViewModel(barcode);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}