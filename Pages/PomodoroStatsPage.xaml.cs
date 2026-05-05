using System.Collections.ObjectModel;
using Microsoft.Maui.Graphics;
using RemindMe.Services;

namespace RemindMe.Pages;

public partial class PomodoroStatsPage : ContentPage
{
    private readonly DatabaseService _db;
    private int _selectedDays = 7;

    public ObservableCollection<DailyPomodoroStat> DailyStats { get; set; } = new();

    public string RangeText { get; set; } = "Last 7 days";
    public string SummaryText { get; set; } = "Your focus progress over time.";

    private Color ActiveColor => Color.FromArgb("#2E7D32");
    private Color InactiveColor => Color.FromArgb("#214B27");

    public Color OneWeekColor => _selectedDays == 7 ? ActiveColor : InactiveColor;
    public Color ThreeWeekColor => _selectedDays == 21 ? ActiveColor : InactiveColor;
    public Color OneMonthColor => _selectedDays == 30 ? ActiveColor : InactiveColor;
    public Color ThreeMonthColor => _selectedDays == 90 ? ActiveColor : InactiveColor;
    public Color SixMonthColor => _selectedDays == 180 ? ActiveColor : InactiveColor;
    public Color OneYearColor => _selectedDays == 365 ? ActiveColor : InactiveColor;

    public PomodoroStatsPage()
    {
        InitializeComponent();

        string dbPath = Path.Combine(FileSystem.AppDataDirectory, "reminders.db");
        _db = new DatabaseService(dbPath);

        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadData(_selectedDays);
    }

    private async Task LoadData(int days)
    {
        _selectedDays = days;

        var allSessions = await _db.GetPomodoroSessionsAsync();

        DateTime startDate = DateTime.Today.AddDays(-(days - 1));

        var filteredSessions = allSessions
            .Where(s => s.StartedAt.Date >= startDate)
            .ToList();

        List<DailyPomodoroStat> stats = days switch
        {
            90 => BuildWeeklyStats(filteredSessions, startDate, 13),
            180 => BuildWeeklyStats(filteredSessions, startDate, 26),
            365 => BuildMonthlyStats(filteredSessions, startDate, 12),
            _ => BuildDailyStats(filteredSessions, startDate, days)
        };

        int maxFocus = Math.Max(1, stats.Max(s => s.FocusMinutes));

        DailyStats.Clear();

        foreach (var stat in stats)
        {
            stat.Progress = (double)stat.FocusMinutes / maxFocus;
            DailyStats.Add(stat);
        }

        ChartCanvas.Drawable = new PomodoroChartDrawable(DailyStats.ToList());
        ChartCanvas.Invalidate();

        int totalFocus = filteredSessions.Sum(s => s.FocusMinutes);
        int totalSessions = filteredSessions.Count(s => s.FocusMinutes > 0);

        RangeText = days switch
        {
            7 => "Last 7 days",
            21 => "Last 3 weeks",
            30 => "Last 1 month",
            90 => "Last 3 months • weekly view",
            180 => "Last 6 months • weekly view",
            365 => "Last 1 year • monthly view",
            _ => $"Last {days} days"
        };

        SummaryText = $"{totalFocus} focused minutes • {totalSessions} focus sessions";

        RefreshBinding();
    }

    private List<DailyPomodoroStat> BuildDailyStats(
        List<RemindMe.Models.PomodoroSession> sessions,
        DateTime startDate,
        int days)
    {
        var grouped = sessions
            .GroupBy(s => s.StartedAt.Date)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    FocusMinutes = g.Sum(s => s.FocusMinutes),
                    SessionCount = g.Count(s => s.FocusMinutes > 0)
                });

        return Enumerable.Range(0, days)
            .Select(i =>
            {
                DateTime day = startDate.AddDays(i);

                int focus = grouped.ContainsKey(day) ? grouped[day].FocusMinutes : 0;
                int count = grouped.ContainsKey(day) ? grouped[day].SessionCount : 0;

                return new DailyPomodoroStat
                {
                    Date = day,
                    Label = day.ToString("MMM d"),
                    FocusMinutes = focus,
                    SessionCount = count
                };
            })
            .ToList();
    }

    private List<DailyPomodoroStat> BuildWeeklyStats(
        List<RemindMe.Models.PomodoroSession> sessions,
        DateTime startDate,
        int weeks)
    {
        var result = new List<DailyPomodoroStat>();

        for (int i = 0; i < weeks; i++)
        {
            DateTime weekStart = startDate.AddDays(i * 7);
            DateTime weekEnd = weekStart.AddDays(6);

            var weekSessions = sessions
                .Where(s => s.StartedAt.Date >= weekStart.Date &&
                            s.StartedAt.Date <= weekEnd.Date)
                .ToList();

            int focus = weekSessions.Sum(s => s.FocusMinutes);
            int count = weekSessions.Count(s => s.FocusMinutes > 0);

            result.Add(new DailyPomodoroStat
            {
                Date = weekStart,
                Label = $"{weekStart:MMM d} - {weekEnd:MMM d}",
                FocusMinutes = focus,
                SessionCount = count
            });
        }

        return result;
    }

    private List<DailyPomodoroStat> BuildMonthlyStats(
        List<RemindMe.Models.PomodoroSession> sessions,
        DateTime startDate,
        int months)
    {
        var result = new List<DailyPomodoroStat>();

        DateTime monthStart = new DateTime(startDate.Year, startDate.Month, 1);

        for (int i = 0; i < months; i++)
        {
            DateTime currentMonth = monthStart.AddMonths(i);
            DateTime nextMonth = currentMonth.AddMonths(1);

            var monthSessions = sessions
                .Where(s => s.StartedAt.Date >= currentMonth.Date &&
                            s.StartedAt.Date < nextMonth.Date)
                .ToList();

            int focus = monthSessions.Sum(s => s.FocusMinutes);
            int count = monthSessions.Count(s => s.FocusMinutes > 0);

            result.Add(new DailyPomodoroStat
            {
                Date = currentMonth,
                Label = currentMonth.ToString("MMM yyyy"),
                FocusMinutes = focus,
                SessionCount = count
            });
        }

        return result;
    }

    private async void On1Week(object? sender, EventArgs e) => await LoadData(7);
    private async void On3Week(object? sender, EventArgs e) => await LoadData(21);
    private async void On1Month(object? sender, EventArgs e) => await LoadData(30);
    private async void On3Month(object? sender, EventArgs e) => await LoadData(90);
    private async void On6Month(object? sender, EventArgs e) => await LoadData(180);
    private async void On1Year(object? sender, EventArgs e) => await LoadData(365);

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private void RefreshBinding()
    {
        BindingContext = null;
        BindingContext = this;
    }
}

public class DailyPomodoroStat
{
    public DateTime Date { get; set; }

    public string Label { get; set; } = "";

    public int FocusMinutes { get; set; }

    public int SessionCount { get; set; }

    public double Progress { get; set; }

    public string DateLabel => Label;

    public string FocusText => $"{FocusMinutes} min";

    public string SessionsText => SessionCount == 1
        ? "1 session"
        : $"{SessionCount} sessions";

    public double CardOpacity => FocusMinutes == 0 ? 0.55 : 1.0;
}

public class PomodoroChartDrawable : IDrawable
{
    private readonly List<DailyPomodoroStat> _stats;

    public PomodoroChartDrawable(List<DailyPomodoroStat> stats)
    {
        _stats = stats;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FillColor = Colors.White;
        canvas.FillRectangle(dirtyRect);

        if (_stats.Count == 0)
            return;

        float width = dirtyRect.Width;
        float height = dirtyRect.Height;

        float chartTop = 20;
        float chartBottom = height - 34;
        float chartHeight = chartBottom - chartTop;

        int maxFocus = Math.Max(1, _stats.Max(s => s.FocusMinutes));

        int visibleCount = Math.Min(_stats.Count, 12);
        var visibleStats = _stats.TakeLast(visibleCount).ToList();

        float gap = 8;
        float barWidth = (width - ((visibleCount - 1) * gap)) / visibleCount;

        for (int i = 0; i < visibleStats.Count; i++)
        {
            var stat = visibleStats[i];

            float x = i * (barWidth + gap);
            float normalized = stat.FocusMinutes / (float)maxFocus;
            float barHeight = Math.Max(4, normalized * chartHeight);
            float y = chartBottom - barHeight;

            canvas.FillColor = Color.FromArgb("#DCECCF");
            canvas.FillRoundedRectangle(x, chartTop, barWidth, chartHeight, 8);

            canvas.FillColor = stat.FocusMinutes == 0
                ? Color.FromArgb("#B8CDB2")
                : Color.FromArgb("#2E7D32");

            canvas.FillRoundedRectangle(x, y, barWidth, barHeight, 8);

            canvas.FontColor = Color.FromArgb("#0B2B14");
            canvas.FontSize = 10;

            string label = stat.DateLabel.Split(' ')[0];
            canvas.DrawString(
                label,
                x,
                chartBottom + 8,
                barWidth,
                20,
                HorizontalAlignment.Center,
                VerticalAlignment.Top);
        }
    }
}