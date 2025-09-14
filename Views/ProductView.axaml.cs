using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using MSM.ViewModels;

namespace MSM.Views
{
    public partial class ProductView : UserControl
    {
        public ProductView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ProductViewModel viewModel)
            {
                // Manually invoke the command on the UI thread
                viewModel.EditCommand.Execute(viewModel);
            }
        }
    }
}