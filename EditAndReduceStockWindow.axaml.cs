using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

namespace MSM
{
    public partial class EditAndReduceStockWindow : Window
    {
        public EditAndReduceStockWindow()
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
    }
}