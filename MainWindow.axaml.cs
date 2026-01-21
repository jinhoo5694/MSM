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
        private TextBlock? _primaryPathStatus;
        private TextBlock? _secondaryPathStatus;
        private TextBlock? _versionText;
        private readonly IStockService _stockService;
        private readonly DispatcherTimer _autoSaveTimer;
        private readonly DispatcherTimer _idleTimer;
        private readonly DispatcherTimer _pathCheckTimer;
        private ScreensaverWindow? _screensaverWindow;
        private AutoSaveSettings _autoSaveSettings;

        // Idle timeout: 10 seconds for testing (change to 300 for production)
        private const int IdleTimeoutSeconds = 600;

        public MainWindow()
        {
            InitializeComponent();

            _barcodeTextBox = this.FindControl<TextBox>("BarcodeTextBox");
            _messageTextBlock = this.FindControl<TextBlock>("MessageTextBlock");
            _productsItemsControl = this.FindControl<ItemsControl>("ProductsItemsControl");
            _primaryPathStatus = this.FindControl<TextBlock>("PrimaryPathStatus");
            _secondaryPathStatus = this.FindControl<TextBlock>("SecondaryPathStatus");
            _versionText = this.FindControl<TextBlock>("VersionText");

            // Set version text
            if (_versionText != null)
            {
                _versionText.Text = $"v{UpdateService.GetCurrentVersion()}";
            }

            var startPicker = this.FindControl<DatePicker>("StartDatePicker");
            var endPicker = this.FindControl<DatePicker>("EndDatePicker");

            if (startPicker != null)
                startPicker.SelectedDateChanged += OnDateChanged;

            if (endPicker != null)
                endPicker.SelectedDateChanged += OnDateChanged;
            // StockService를 한 번만 생성해서 ViewModel과 공유
            _stockService = new StockService("stock.xlsx");

            // Load auto-save settings
            _autoSaveSettings = AutoSaveSettings.Load();

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

            // Path check timer - check every 30 seconds
            _pathCheckTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _pathCheckTimer.Tick += (_, _) => UpdatePathStatusDisplay();
            _pathCheckTimer.Start();

            // Initial path status update
            UpdatePathStatusDisplay();

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
            if (now.Hour == 2 && now.Minute == 0)
            {
                PerformAutoSave();
            }
        }

        private void PerformAutoSave()
        {
            var now = DateTime.Now;
            string fileName = $"띵니_재고관리내역_{now:yyyyMMdd_HHmm}.xlsx";

            // Try primary path
            if (AutoSaveSettings.IsPathValid(_autoSaveSettings.PrimaryPath))
            {
                try
                {
                    string path = Path.Combine(_autoSaveSettings.PrimaryPath!, fileName);
                    _stockService.ExportStockReport(path);
                }
                catch
                {
                    // Primary failed, will try secondary
                }
            }

            // Try secondary path
            if (AutoSaveSettings.IsPathValid(_autoSaveSettings.SecondaryPath))
            {
                try
                {
                    string path = Path.Combine(_autoSaveSettings.SecondaryPath!, fileName);
                    _stockService.ExportStockReport(path);
                }
                catch
                {
                    // Secondary failed, will use fallback
                }
            }

            // Always save to fallback as safety net
            try
            {
                string fallbackPath = Path.Combine(AutoSaveSettings.GetFallbackPath(), fileName);
                _stockService.ExportStockReport(fallbackPath);
            }
            catch
            {
                // Last resort failed
            }
        }

        private void UpdatePathStatusDisplay()
        {
            if (_primaryPathStatus != null)
            {
                bool isPrimaryValid = AutoSaveSettings.IsPathValid(_autoSaveSettings.PrimaryPath);
                _primaryPathStatus.Text = isPrimaryValid ? "[정상]" : "[오류]";
                _primaryPathStatus.Foreground = isPrimaryValid
                    ? Avalonia.Media.Brushes.Green
                    : Avalonia.Media.Brushes.Red;
            }

            if (_secondaryPathStatus != null)
            {
                bool isSecondaryValid = AutoSaveSettings.IsPathValid(_autoSaveSettings.SecondaryPath);
                _secondaryPathStatus.Text = isSecondaryValid ? "[정상]" : "[오류]";
                _secondaryPathStatus.Foreground = isSecondaryValid
                    ? Avalonia.Media.Brushes.Green
                    : Avalonia.Media.Brushes.Red;
            }
        }

        private async void OnSetAutoSaveDirectoryClick(object? sender, RoutedEventArgs e)
        {
            // First, ask which path to set
            var choiceDialog = new Window
            {
                Title = "자동 저장 경로 설정",
                Width = 500,
                Height = 280,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            string? selectedPathType = null;

            var primaryButton = new Button
            {
                Content = "기본 경로 설정",
                Width = 200,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Thickness(0, 5, 0, 5)
            };
            var secondaryButton = new Button
            {
                Content = "보조 경로 설정",
                Width = 200,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Thickness(0, 5, 0, 5)
            };
            var cancelButton = new Button
            {
                Content = "취소",
                Width = 200,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Thickness(0, 5, 0, 5)
            };

            primaryButton.Click += (_, __) => { selectedPathType = "primary"; choiceDialog.Close(); };
            secondaryButton.Click += (_, __) => { selectedPathType = "secondary"; choiceDialog.Close(); };
            cancelButton.Click += (_, __) => { selectedPathType = null; choiceDialog.Close(); };

            // Status display
            var primaryStatus = AutoSaveSettings.IsPathValid(_autoSaveSettings.PrimaryPath) ? "정상" : "오류";
            var secondaryStatus = AutoSaveSettings.IsPathValid(_autoSaveSettings.SecondaryPath) ? "정상" : "오류";
            var primaryColor = AutoSaveSettings.IsPathValid(_autoSaveSettings.PrimaryPath)
                ? Avalonia.Media.Brushes.Green : Avalonia.Media.Brushes.Red;
            var secondaryColor = AutoSaveSettings.IsPathValid(_autoSaveSettings.SecondaryPath)
                ? Avalonia.Media.Brushes.Green : Avalonia.Media.Brushes.Red;

            var fallbackPath = AutoSaveSettings.GetFallbackPath();

            choiceDialog.Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = "현재 자동 저장 경로 상태:",
                        FontWeight = Avalonia.Media.FontWeight.Bold,
                        Margin = new Thickness(0, 0, 0, 8)
                    },
                    new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        Spacing = 8,
                        Children =
                        {
                            new TextBlock { Text = "기본:", Width = 40 },
                            new TextBlock { Text = $"[{primaryStatus}]", Foreground = primaryColor, Width = 50 },
                            new TextBlock
                            {
                                Text = string.IsNullOrEmpty(_autoSaveSettings.PrimaryPath) ? "(설정 안됨)" : _autoSaveSettings.PrimaryPath,
                                TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
                                MaxWidth = 350
                            }
                        }
                    },
                    new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        Spacing = 8,
                        Children =
                        {
                            new TextBlock { Text = "보조:", Width = 40 },
                            new TextBlock { Text = $"[{secondaryStatus}]", Foreground = secondaryColor, Width = 50 },
                            new TextBlock
                            {
                                Text = string.IsNullOrEmpty(_autoSaveSettings.SecondaryPath) ? "(설정 안됨)" : _autoSaveSettings.SecondaryPath,
                                TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
                                MaxWidth = 350
                            }
                        }
                    },
                    new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        Spacing = 8,
                        Children =
                        {
                            new TextBlock { Text = "대체:", Width = 40 },
                            new TextBlock { Text = "[항상]", Foreground = Avalonia.Media.Brushes.Blue, Width = 50 },
                            new TextBlock
                            {
                                Text = fallbackPath,
                                TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
                                MaxWidth = 350
                            }
                        }
                    },
                    new Border { Height = 10 },
                    new TextBlock
                    {
                        Text = "설정할 경로를 선택하세요:",
                        Margin = new Thickness(0, 5, 0, 5)
                    },
                    new StackPanel
                    {
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Children = { primaryButton, secondaryButton, cancelButton }
                    }
                }
            };

            await choiceDialog.ShowDialog(this);

            if (selectedPathType == null)
            {
                _barcodeTextBox?.Focus();
                return;
            }

            // Now show folder picker
            var folderDialog = new OpenFolderDialog
            {
                Title = selectedPathType == "primary" ? "기본 자동 저장 경로 선택" : "보조 자동 저장 경로 선택"
            };

            var result = await folderDialog.ShowAsync(this);
            if (!string.IsNullOrEmpty(result))
            {
                if (selectedPathType == "primary")
                    _autoSaveSettings.PrimaryPath = result;
                else
                    _autoSaveSettings.SecondaryPath = result;

                _autoSaveSettings.Save();
                UpdatePathStatusDisplay();

                // Show confirmation
                var confirmDialog = new Window
                {
                    Title = "설정 완료",
                    Width = 400,
                    Height = 150,
                    CanResize = false,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                var okButton = new Button { Content = "확인", Width = 80, IsDefault = true };
                okButton.Click += (_, __) => confirmDialog.Close();

                var pathTypeName = selectedPathType == "primary" ? "기본" : "보조";
                confirmDialog.Content = new StackPanel
                {
                    Margin = new Thickness(15),
                    Children =
                    {
                        new TextBlock
                        {
                            Text = $"{pathTypeName} 자동 저장 경로가 설정되었습니다:\n{result}",
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
            }

            _barcodeTextBox?.Focus();
        }

        private async void OnShowChangeHistoryClick(object? sender, RoutedEventArgs e)
        {
            var historyWindow = new ChangeHistoryWindow(_stockService);
            await historyWindow.ShowDialog(this);
            _barcodeTextBox?.Focus();
        }

        private async void OnCheckUpdateClick(object? sender, RoutedEventArgs e)
        {
            // Show checking dialog
            var checkingDialog = new Window
            {
                Title = "업데이트 확인",
                Width = 400,
                Height = 150,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            checkingDialog.Content = new StackPanel
            {
                Margin = new Thickness(20),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Children =
                {
                    new TextBlock
                    {
                        Text = "업데이트를 확인하는 중...",
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        FontSize = 16
                    },
                    new ProgressBar
                    {
                        IsIndeterminate = true,
                        Margin = new Thickness(0, 20, 0, 0),
                        Height = 20
                    }
                }
            };

            // Check for updates in background
            var checkTask = UpdateService.CheckForUpdateAsync();

            // Show dialog briefly then check result
            _ = Task.Run(async () =>
            {
                var updateInfo = await checkTask;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    checkingDialog.Close();
                    ShowUpdateResultDialog(updateInfo);
                });
            });

            await checkingDialog.ShowDialog(this);
        }

        private async void ShowUpdateResultDialog(UpdateInfo? updateInfo)
        {
            if (updateInfo == null)
            {
                await ShowSimpleDialog("오류", "업데이트 정보를 가져올 수 없습니다.");
                return;
            }

            if (!string.IsNullOrEmpty(updateInfo.Error))
            {
                await ShowSimpleDialog("오류", $"업데이트 확인 중 오류가 발생했습니다:\n{updateInfo.Error}");
                return;
            }

            if (!updateInfo.IsUpdateAvailable)
            {
                await ShowSimpleDialog("최신 버전", $"현재 최신 버전입니다. (v{updateInfo.CurrentVersion})");
                return;
            }

            // Update available - show update dialog
            var updateDialog = new Window
            {
                Title = "새 버전 발견",
                Width = 450,
                Height = 250,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var downloadButton = new Button
            {
                Content = "다운로드 및 설치",
                Classes = { "Primary" },
                Width = 150,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };
            var cancelButton = new Button
            {
                Content = "나중에",
                Width = 100,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            bool shouldUpdate = false;
            downloadButton.Click += (_, _) => { shouldUpdate = true; updateDialog.Close(); };
            cancelButton.Click += (_, _) => { shouldUpdate = false; updateDialog.Close(); };

            updateDialog.Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 12,
                Children =
                {
                    new TextBlock
                    {
                        Text = "새 버전이 있습니다!",
                        FontSize = 18,
                        FontWeight = Avalonia.Media.FontWeight.Bold
                    },
                    new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        Spacing = 8,
                        Children =
                        {
                            new TextBlock { Text = $"현재 버전: v{updateInfo.CurrentVersion}", FontSize = 14 },
                            new TextBlock { Text = "→", FontSize = 14 },
                            new TextBlock { Text = $"새 버전: v{updateInfo.LatestVersion}", FontSize = 14, FontWeight = Avalonia.Media.FontWeight.Bold, Foreground = Avalonia.Media.Brushes.Green }
                        }
                    },
                    new TextBlock
                    {
                        Text = string.IsNullOrEmpty(updateInfo.ReleaseNotes) ? "" : updateInfo.ReleaseNotes,
                        FontSize = 12,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        MaxHeight = 60
                    },
                    new Border { Height = 10 },
                    new TextBlock
                    {
                        Text = "업데이트 후 프로그램이 자동으로 재시작됩니다.\n(재고 데이터는 유지됩니다)",
                        FontSize = 12,
                        Foreground = Avalonia.Media.Brushes.Gray
                    },
                    new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                        Spacing = 10,
                        Margin = new Thickness(0, 10, 0, 0),
                        Children = { downloadButton, cancelButton }
                    }
                }
            };

            await updateDialog.ShowDialog(this);

            if (shouldUpdate && !string.IsNullOrEmpty(updateInfo.DownloadUrl))
            {
                await DownloadAndInstallUpdate(updateInfo.DownloadUrl);
            }

            _barcodeTextBox?.Focus();
        }

        private async Task DownloadAndInstallUpdate(string downloadUrl)
        {
            var progressDialog = new Window
            {
                Title = "업데이트 다운로드",
                Width = 400,
                Height = 150,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var progressBar = new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Height = 20
            };
            var progressText = new TextBlock
            {
                Text = "다운로드 중... 0%",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            progressDialog.Content = new StackPanel
            {
                Margin = new Thickness(20),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Spacing = 12,
                Children =
                {
                    new TextBlock
                    {
                        Text = "업데이트를 다운로드하는 중...",
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        FontSize = 16
                    },
                    progressBar,
                    progressText
                }
            };

            var downloadTask = Task.Run(async () =>
            {
                // DownloadAndInstallAsync will call Environment.Exit(0) on success
                var result = await UpdateService.DownloadAndInstallAsync(downloadUrl, progress =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        progressBar.Value = progress;
                        progressText.Text = $"다운로드 중... {progress}%";
                    });
                });

                // Only reaches here on failure (success exits the app)
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    progressDialog.Close();
                    await ShowSimpleDialog("오류", "업데이트 설치 중 오류가 발생했습니다.");
                });
            });

            await progressDialog.ShowDialog(this);
        }

        private async Task ShowSimpleDialog(string title, string message)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 400,
                Height = 150,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var okButton = new Button { Content = "확인", Width = 80, IsDefault = true };
            okButton.Click += (_, _) => dialog.Close();

            dialog.Content = new StackPanel
            {
                Margin = new Thickness(20),
                Children =
                {
                    new TextBlock
                    {
                        Text = message,
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

            await dialog.ShowDialog(this);
            _barcodeTextBox?.Focus();
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
