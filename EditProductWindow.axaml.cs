using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using MSM.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Dialogs.Internal;
using MSM.Models;

namespace MSM
{
    public partial class EditProductWindow : Window
    {
        public EditProductWindow()
        {
            InitializeComponent();
            this.Opened += OnOpened;
#if DEBUG
            this.AttachDevTools();
#endif
        }

        public EditProductWindow(EditProductViewModel viewModel) : this()
        {
            DataContext = viewModel;
            viewModel.ShowFilePicker += () => PickImageFileAsync();
        }
        
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnOpened(object? sender, System.EventArgs e)
        {
            if (DataContext is EditProductViewModel vm)
            {
                vm.ShowFilePicker += async () => await PickImageFileAsync();
            }
        }

        private async Task<string?> PickImageFileAsync()
        {
            var files = await this.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "이미지 선택",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("이미지 파일")
                    {
                        Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp" }
                    }
                }
            });

            var file = files.FirstOrDefault();
            return file?.Path.LocalPath;
        }
        
        public static async Task<Product?> ShowDialog(Window owner, EditProductViewModel viewModel)
        {
            var window = new EditProductWindow(viewModel);
            var result = await window.ShowDialog<Product>(owner);
            return result;
        }
    }
}