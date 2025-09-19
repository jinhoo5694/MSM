using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using MSM.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using MSM.Models;

namespace MSM
{
    public partial class EditProductWindow : Window
    {
        public EditProductWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        public EditProductWindow(EditProductViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
        
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        public static async Task<Product?> ShowDialog(Window owner, EditProductViewModel viewModel)
        {
            var window = new EditProductWindow(viewModel);
            var result = await window.ShowDialog<Product>(owner);
            return result;
        }
    }
}