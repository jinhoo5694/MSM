using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using MSM.Services;
using MSM.ViewModels;
using MSM.Models;
using MSM.Views;
using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input;
using Avalonia.Threading;
using Path = System.IO.Path;

namespace MSM
{
    public partial class MainWindow : Window
    {
        private TextBox? _barcodeTextBox;
        private TextBlock? _messageTextBlock;
        private ItemsControl? _productsItemsControl;
        private readonly IStockService _stockService;
        private readonly DispatcherTimer _autoSaveTimer;
        private readonly DispatcherTimer _idleTimer;
        private ScreensaverWindow? _screensaverWindow;
        private string? _autoSaveDirectory;

        // Idle timeout: 5 minutes (300 seconds) for production
        private const int IdleTimeoutSeconds = 300;

        public MainWindow()
        {
            InitializeComponent();

            _barcodeTextBox = this.FindControl<TextBox>("BarcodeTextBox");
            _messageTextBlock = this.FindControl<TextBlock>("MessageTextBlock");
            _productsItemsControl = this.FindControl<ItemsControl>("ProductsItemsControl");
            var startPicker = this.FindControl<DatePicker>("StartDatePicker");
            var endPicker = this.FindControl<DatePicker>("EndDatePicker");

            if (startPicker != null)
                startPicker.SelectedDateChanged += OnDateChanged;

            if (endPicker != null)
                endPicker.SelectedDateChanged += OnDateChanged;
            // StockService를 한 번만 생성해서 ViewModel과 공유
            _stockService = new StockService("stock.xlsx");

            // Set default auto-save directory to application directory
            _autoSaveDirectory = AppDomain.CurrentDomain.BaseDirectory;

            if (Design.IsDesignMode)
            {
                DataContext = new MainWindowViewModel(_stockService, this);
            }
            else
            {
                DataContext = new MainWindowViewModel(_stockService, this);
                this.Opened += (_, _) =>
                {
                    _barcodeTextBox?.Focus();
                };
            }

            _autoSaveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(1)
            };
            _autoSaveTimer.Tick += AutoSaveTimer_Tick;
            _autoSaveTimer.Start();

            // Idle detection timer for screensaver
            _idleTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(IdleTimeoutSeconds)
            };
            _idleTimer.Tick += IdleTimer_Tick;
            _idleTimer.Start();

            // Reset idle timer on any user interaction
            PointerMoved += ResetIdleTimer;
            PointerPressed += ResetIdleTimer;
            KeyDown += ResetIdleTimer;
            KeyUp += ResetIdleTimer;

            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.ShowEditAndReduceStockWindow += async editAndReduceViewModel =>
                {
                    viewModel.IsDialogOpen = true;
                    var dialog = new EditAndReduceStockWindow { DataContext = editAndReduceViewModel };
                    var result = await dialog.ShowDialog<Product>(this);
                    viewModel.IsDialogOpen = false;
                    if (result != null)
                    {
                        viewModel.LoadProducts();
                    }
                    _barcodeTextBox?.Focus();
                    return result;
                };

                viewModel.ShowAddProductWindow += async addViewModel =>
                {
                    viewModel.IsDialogOpen = true;
                    var dialog = new AddProductWindow { DataContext = addViewModel };
                    var result = await dialog.ShowDialog<Product>(this);
                    viewModel.IsDialogOpen = false;
                    _barcodeTextBox?.Focus();
                    return result;
                };

                viewModel.RequestFocusBarcode += () =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        _barcodeTextBox?.Focus();
                        if (_barcodeTextBox != null)
                            _barcodeTextBox.CaretIndex = +_barcodeTextBox.Text?.Length ?? 0;
                    }, DispatcherPriority.Background);
                };

                viewModel.ShowAlert += async (message) =>
                {
                    var dlg = new Window
                    {
                        Title = "알림",
                        Width = 300,
                        Height = 150
                    };

                    var okButton = new Button
                    {
                        Content = "확인",
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Width = 60,
                        Margin = new Thickness(0, 10, 0, 0)
                    };

                    okButton.Click += (_, __) => dlg.Close();

                    dlg.Content = new StackPanel
                    {
                        Margin = new Thickness(10),
                        Children =
                        {
                            new TextBlock { Text = message, Margin = new Thickness(0,0,0,10) },
                            okButton
                        }
                    };

                    await dlg.ShowDialog(this);

                    // Alert 닫힌 뒤에도 SearchBox에 포커스 맞추기
                    _barcodeTextBox?.Focus();
                    if (_barcodeTextBox != null)
                        _barcodeTextBox.CaretIndex = _barcodeTextBox.Text?.Length ?? 0;
                };
                
                viewModel.ShowConfirmation += async (message) =>
                {
                    viewModel.IsDialogOpen = true; 
                    
                    var confirmationDialog = new Window
                    {
                        Title = "삭제 확인",
                        Width = 400,
                        Height = 150,
                        CanResize = false
                    };

                    var yesButton = new Button { Content = "예", Width = 80, IsDefault = true }; // 기본 버튼으로 설정
                    var noButton = new Button { Content = "아니오", Width = 80, IsCancel = true };
                    
                    bool result = false;

                    yesButton.Click += (_, __) => { result = true; confirmationDialog.Close(); };
                    noButton.Click += (_, __) => { result = false; confirmationDialog.Close(); };

                    confirmationDialog.Content = new StackPanel
                    {
                        Margin = new Thickness(15),
                        Children =
                        {
                            new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                            new StackPanel
                            {
                                Orientation = Avalonia.Layout.Orientation.Horizontal,
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                                Spacing = 10,
                                Margin = new Thickness(0, 20, 0, 0),
                                Children = { yesButton, noButton }
                            }
                        }
                    };

                    await confirmationDialog.ShowDialog(this);
                    
                    viewModel.IsDialogOpen = false;
                    _barcodeTextBox?.Focus();
                    
                    return result; // 사용자의 선택 반환
                };
            }
        }

        private void AutoSaveTimer_Tick(object? sender, EventArgs e)
        {
            var now = DateTime.Now;
            if (_autoSaveDirectory != null && now.Hour == 2 && now.Minute == 0)
            {
                string fileName = $"띵니_재고관리내역_{now:yyyyMMdd_HHmm}.xlsx";
                string path = Path.Combine(_autoSaveDirectory, fileName);
                _stockService.ExportStockReport(path);
            }
        }

        private async void OnSetAutoSaveDirectoryClick(object? sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "자동 저장 경로 선택",
                Directory = _autoSaveDirectory
            };

            var result = await dialog.ShowAsync(this);
            if (!string.IsNullOrEmpty(result))
            {
                _autoSaveDirectory = result;

                // Show confirmation
                var confirmDialog = new Window
                {
                    Title = "설정 완료",
                    Width = 400,
                    Height = 150,
                    CanResize = false
                };

                var okButton = new Button { Content = "확인", Width = 80, IsDefault = true };
                okButton.Click += (_, __) => confirmDialog.Close();

                confirmDialog.Content = new StackPanel
                {
                    Margin = new Thickness(15),
                    Children =
                    {
                        new TextBlock
                        {
                            Text = $"자동 저장 경로가 설정되었습니다:\n{result}",
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap
                        },
                        new StackPanel
                        {
                            Orientation = Avalonia.Layout.Orientation.Horizontal,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                            Margin = new Thickness(0, 15, 0, 0),
                            Children = { okButton }
                        }
                    }
                };

                await confirmDialog.ShowDialog(this);
                _barcodeTextBox?.Focus();
            }
        }

        private async void OnExportReportClick(object? sender, RoutedEventArgs e)
        {
            await ExportReportManualAsync();
        }
        
        private async void OnExportAndCloseClick(object? sender, RoutedEventArgs e)
        {
            // 1. 저장 작업을 실행하고 성공 여부를 받습니다.
            bool saveSuccessful = await ExportReportManualAsync();

            // 2. 저장이 성공했을 경우에만 창을 닫아 프로그램을 종료합니다.
            if (saveSuccessful)
            {
                this.Close();
            }
            // saveSuccessful이 false(취소)이면 아무것도 하지 않아 프로그램이 계속 실행됩니다.
        }

        private async  void OnCloseClick(object? sender, RoutedEventArgs e)
        {
            var confirmationDialog = new Window
            {
                Title = "프로그램 종료",
                Width = 400,
                Height = 150,
                CanResize = false
            };

            var yesButton = new Button { Content = "예", Width = 80, IsDefault = true };
            var noButton = new Button { Content = "아니오", Width = 80, IsCancel = true };

            bool shouldClose = false;

            yesButton.Click += (_, __) => { shouldClose = true; confirmationDialog.Close(); };
            noButton.Click += (_, __) => { shouldClose = false; confirmationDialog.Close(); };

            confirmationDialog.Content = new StackPanel
            {
                Margin = new Thickness(15),
                Children =
                {
                    new TextBlock { Text = "종료하시겠습니까?", TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                    new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                        Spacing = 10,
                        Margin = new Thickness(0, 20, 0, 0),
                        Children = { yesButton, noButton }
                    }
                }
            };

            await confirmationDialog.ShowDialog(this);

            // 사용자가 "예"를 눌렀을 때만 프로그램 종료
            if (shouldClose)
            {
                this.Close();
            }
        }
        public async Task<bool> ExportReportManualAsync()
        {
            var sfd = new SaveFileDialog
            {
                Title = "재고 보고서 저장",
                Filters = { new FileDialogFilter { Name = "Excel Files", Extensions = { "xlsx" } } },
                InitialFileName = $"띵니_재고관리내역_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            var result = await sfd.ShowAsync(this);
            if (!string.IsNullOrEmpty(result))
            {
                // 저장이 실제로 수행되었을 때
                _stockService.ExportStockReport(result);

                try { this.Activate(); } catch { /* 무시 */ }

                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await Task.Delay(50);
                    _barcodeTextBox?.Focus();
                    if (_barcodeTextBox != null)
                        _barcodeTextBox.CaretIndex = _barcodeTextBox.Text?.Length ?? 0;
                }, DispatcherPriority.Background);

                // 저장이 성공했으므로 true 반환
                return true; 
            }
    
            // 사용자가 취소했거나 경로가 유효하지 않을 때 false 반환
            return false;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void BarcodeTextBox_KeyUp(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (DataContext is MainWindowViewModel vm)
                {
                    if (vm.SearchCommand?.CanExecute(null) == true)
                    {
                        vm.SearchCommand.Execute(null);
                    }
                }

                Dispatcher.UIThread.Post(() =>
                {
                    _barcodeTextBox?.Focus();
                }, DispatcherPriority.Background);

                _barcodeTextBox?.Focus();
            }
        }
        private void OnDateChanged(object? sender, DatePickerSelectedValueChangedEventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _barcodeTextBox?.Focus();
                if (_barcodeTextBox != null)
                    _barcodeTextBox.CaretIndex = _barcodeTextBox.Text?.Length ?? 0;
            }, DispatcherPriority.Background);
        }
        
        private void ToggleButton_FocusBarcode(object? sender, RoutedEventArgs e)
        {
            // UI 스레드에서 백그라운드 우선순위로 포커스를 요청합니다.
            // 이는 토글 상태 변경 처리가 끝난 후 포커스가 확실히 적용되도록 보장합니다.
            Dispatcher.UIThread.Post(() =>
            {
                _barcodeTextBox?.Focus();

                // (선택 사항) 포커스가 들어갈 때 텍스트가 있다면 커서를 끝으로 이동시킵니다.
                if (_barcodeTextBox != null)
                    _barcodeTextBox.CaretIndex = _barcodeTextBox.Text?.Length ?? 0;
            }, DispatcherPriority.Background);
        }

        private void ResetIdleTimer(object? sender, EventArgs e)
        {
            _idleTimer.Stop();
            _idleTimer.Start();
        }

        private void IdleTimer_Tick(object? sender, EventArgs e)
        {
            _idleTimer.Stop();
            ShowScreensaver();
        }

        private void ShowScreensaver()
        {
            if (_screensaverWindow != null) return;

            _screensaverWindow = new ScreensaverWindow();
            _screensaverWindow.Closed += (_, _) =>
            {
                _screensaverWindow = null;
                _idleTimer.Start();

                // Refocus barcode textbox after screensaver closes
                Dispatcher.UIThread.Post(() =>
                {
                    _barcodeTextBox?.Focus();
                }, DispatcherPriority.Background);
            };
            _screensaverWindow.Show();
        }

    }
}
