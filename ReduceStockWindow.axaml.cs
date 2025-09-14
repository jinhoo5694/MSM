using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MSM.ViewModels;
using MSM.Models;

namespace MSM
{
    public partial class ReduceStockWindow : Window
    {
        public ReduceStockWindow()
        {
            InitializeComponent();
        }

        public ReduceStockWindow(Product product) : this()
        {
            DataContext = new ReduceStockViewModel(product);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}