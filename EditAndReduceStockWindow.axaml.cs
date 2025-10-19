using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MSM.ViewModels;
using MSM.Models;

namespace MSM
{
    public partial class EditAndReduceStockWindow : Window
    {
        public EditAndReduceStockWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}