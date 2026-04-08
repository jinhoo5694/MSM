using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using MSM.ViewModels;

namespace MSM.Views
{
    public partial class ProductView : UserControl
    {
        public ProductView()
        {
            InitializeComponent();
            DisableNumericUpDownScroll();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void DisableNumericUpDownScroll()
        {
            this.Loaded += (_, _) =>
            {
                foreach (var nud in this.GetVisualDescendants().OfType<NumericUpDown>())
                {
                    nud.AddHandler(PointerWheelChangedEvent, (s, e) => e.Handled = true, RoutingStrategies.Tunnel);
                }
            };
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ProductViewModel viewModel)
            {
                viewModel.EditCommand.Execute(viewModel);
            }
        }
    }
}