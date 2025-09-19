using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MSM.Views
{
    public partial class EditProductView : UserControl
    {
        public EditProductView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}