using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using MSM.ViewModels;
using MSM.Models;

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
                    nud.PointerWheelChanged += (s, e) => e.Handled = true;
                }
            };
        }
    }
}