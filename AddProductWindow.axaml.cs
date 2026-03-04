using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

namespace MSM
{
    public partial class AddProductWindow : Window
    {
        public AddProductWindow()
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
                    nud.PointerWheelChanged += (s, e) => e.Handled = true;
                }
            };
        }
    }
}
