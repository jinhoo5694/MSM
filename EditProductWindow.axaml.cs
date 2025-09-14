using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using MSM.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;

namespace MSM.Views
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
            viewModel.ShowFilePicker.RegisterHandler(async interaction =>
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null)
                {
                    interaction.SetOutput(null);
                    return;
                }

                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Select Image File",
                    AllowMultiple = false,
                    FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
                });

                interaction.SetOutput(files?.FirstOrDefault()?.Path.LocalPath);
            });

            viewModel.SaveCommand.Subscribe(product =>
            {
                Close(product);
            });

            viewModel.CancelCommand.Subscribe(_ =>
            {
                Close(null);
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}