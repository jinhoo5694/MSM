using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using MSM.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;

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

            viewModel.ShowFilePicker += async () =>
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null)
                {
                    return null;
                }

                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Select Image File",
                    AllowMultiple = false,
                    FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
                });

                return files?.FirstOrDefault()?.Path.LocalPath;
            };

                        viewModel.CloseWindow += (product) =>
            {
                Close(product);
                return Task.CompletedTask;
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}