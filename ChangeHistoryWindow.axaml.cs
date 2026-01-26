using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MSM.Services;

namespace MSM
{
    public partial class ChangeHistoryWindow : Window
    {
        private readonly IStockService _stockService;
        private readonly CalendarDatePicker _startDatePicker;
        private readonly CalendarDatePicker _endDatePicker;
        private readonly DataGrid _historyDataGrid;
        private readonly TextBlock _totalCountText;
        private readonly TextBlock _totalChangedText;
        private readonly Button _searchButton;
        private readonly Button _todayButton;
        private readonly Button _closeButton;

        public ObservableCollection<StockChangeLogEntry> LogEntries { get; } = new();

        public ChangeHistoryWindow()
        {
            InitializeComponent();
            _stockService = new StockService("stock.xlsx");

            _startDatePicker = this.FindControl<CalendarDatePicker>("StartDatePicker")!;
            _endDatePicker = this.FindControl<CalendarDatePicker>("EndDatePicker")!;
            _historyDataGrid = this.FindControl<DataGrid>("HistoryDataGrid")!;
            _totalCountText = this.FindControl<TextBlock>("TotalCountText")!;
            _totalChangedText = this.FindControl<TextBlock>("TotalChangedText")!;
            _searchButton = this.FindControl<Button>("SearchButton")!;
            _todayButton = this.FindControl<Button>("TodayButton")!;
            _closeButton = this.FindControl<Button>("CloseButton")!;

            // Set default dates: start of month to today
            _startDatePicker.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            _endDatePicker.SelectedDate = DateTime.Today;

            _historyDataGrid.ItemsSource = LogEntries;

            _searchButton.Click += OnSearchClick;
            _todayButton.Click += OnTodayClick;
            _closeButton.Click += OnCloseClick;

            // Load today's data on open
            LoadData();
        }

        public ChangeHistoryWindow(IStockService stockService) : this()
        {
            _stockService = stockService;
            LoadData();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnSearchClick(object? sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void OnTodayClick(object? sender, RoutedEventArgs e)
        {
            _startDatePicker.SelectedDate = DateTime.Today;
            _endDatePicker.SelectedDate = DateTime.Today;
            LoadData();
        }

        private void OnCloseClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private void LoadData()
        {
            var startDate = _startDatePicker.SelectedDate ?? DateTime.Today;
            var endDate = _endDatePicker.SelectedDate ?? DateTime.Today;

            LogEntries.Clear();

            var logs = _stockService.GetLogsByDateRange(startDate, endDate);
            foreach (var log in logs)
            {
                LogEntries.Add(log);
            }

            // Update summary
            _totalCountText.Text = $"{LogEntries.Count}건";
            var totalChanged = LogEntries.Sum(x => x.ChangedQty);
            _totalChangedText.Text = $"{totalChanged}개";
        }
    }
}
