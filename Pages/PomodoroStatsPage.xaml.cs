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

    public int CurrentStreak { get; set; }
    public int BestStreak { get; set; }

    public string CurrentStreakText => $"{CurrentStreak} day(s)";
    public string BestStreakText => $"{BestStreak} day(s)";

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

        CalculateStreaks(allSessions);

        SummaryText = $"{totalFocus} focused minutes • {totalSessions} focus sessions";

        RefreshBinding();
    }

    private void CalculateStreaks(List<RemindMe.Models.PomodoroSession> allSessions)
    {
        var focusDays = allSessions
            .Where(s => s.FocusMinutes > 0)
            .Select(s => s.StartedAt.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        CurrentStreak = 0;
        BestStreak = 0;

        if (focusDays.Count == 0)
            return;

        DateTime currentDay = DateTime.Today;

        if (!focusDays.Contains(currentDay))
            currentDay = DateTime.Today.AddDays(-1);

        while (focusDays.Contains(currentDay))
        {
            CurrentStreak++;
            currentDay = currentDay.AddDays(-1);
        }

        int runningStreak = 0;
        DateTime? previousDay = null;

        foreach (var day in focusDays.OrderBy(d => d))
        {
            if (previousDay == null || day == previousDay.Value.AddDays(1))
                runningStreak++;
            else
                runningStreak = 1;

            BestStreak = Math.Max(BestStreak, runningStreak);
            previousDay = day;
        }
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

            result.Add(new DailyPomodoroStat
            {
                Date = weekStart,
                Label = $"{weekStart:MMM d} - {weekEnd:MMM d}",
                FocusMinutes = weekSessions.Sum(s => s.FocusMinutes),
                SessionCount = weekSessions.Count(s => s.FocusMinutes > 0)
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

        DateTime monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-(months - 1));

        for (int i = 0; i < months; i++)
        {
            DateTime currentMonth = monthStart.AddMonths(i);
            DateTime nextMonth = currentMonth.AddMonths(1);

            var monthSessions = sessions
                .Where(s => s.StartedAt.Date >= currentMonth.Date &&
                            s.StartedAt.Date < nextMonth.Date)
                .ToList();

            result.Add(new DailyPomodoroStat
            {
                Date = currentMonth,
                Label = currentMonth.ToString("MMM yyyy"),
                FocusMinutes = monthSessions.Sum(s => s.FocusMinutes),
                SessionCount = monthSessions.Count(s => s.FocusMinutes > 0)
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

        var visibleStats = _stats.TakeLast(12).ToList();

        if (visibleStats.All(s => s.FocusMinutes == 0))
        {
            DrawEmptyChart(canvas, dirtyRect, visibleStats);
            return;
        }

        float width = dirtyRect.Width;
        float height = dirtyRect.Height;

        float leftPadding = 36;
        float rightPadding = 18;
        float topPadding = 18;
        float bottomPadding = 30;

        float chartLeft = leftPadding;
        float chartRight = width - rightPadding;
        float chartTop = topPadding;
        float chartBottom = height - bottomPadding;

        float chartWidth = chartRight - chartLeft;
        float chartHeight = chartBottom - chartTop;

        int maxFocus = Math.Max(5, visibleStats.Max(s => s.FocusMinutes));

        DrawGrid(canvas, chartLeft, chartRight, chartTop, chartBottom, chartHeight, maxFocus, leftPadding);

        var points = BuildPoints(visibleStats, chartLeft, chartWidth, chartBottom, chartHeight, maxFocus);

        canvas.Alpha = 1f;
        canvas.StrokeColor = Color.FromArgb("#2E7D32");
        canvas.StrokeSize = 4;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;

        for (int i = 0; i < points.Count - 1; i++)
            canvas.DrawLine(points[i].X, points[i].Y, points[i + 1].X, points[i + 1].Y);

        for (int i = 0; i < points.Count; i++)
        {
            bool hasFocus = visibleStats[i].FocusMinutes > 0;

            canvas.Alpha = hasFocus ? 1f : 0.35f;
            DrawPoint(canvas, points[i].X, points[i].Y, hasFocus, i == points.Count - 1);

            canvas.Alpha = 1f;
            DrawXLabel(canvas, visibleStats, i, points[i].X, chartBottom);
        }

        canvas.Alpha = 1f;
    }

    private static List<PointF> BuildPoints(
        List<DailyPomodoroStat> visibleStats,
        float chartLeft,
        float chartWidth,
        float chartBottom,
        float chartHeight,
        int maxFocus)
    {
        var points = new List<PointF>();

        for (int i = 0; i < visibleStats.Count; i++)
        {
            float x = chartLeft + (i / (float)(visibleStats.Count - 1)) * chartWidth;
            float normalized = visibleStats[i].FocusMinutes / (float)maxFocus;
            float y = chartBottom - normalized * chartHeight;

            points.Add(new PointF(x, y));
        }

        return points;
    }

    private static void DrawGrid(
        ICanvas canvas,
        float chartLeft,
        float chartRight,
        float chartTop,
        float chartBottom,
        float chartHeight,
        int maxFocus,
        float leftPadding)
    {
        canvas.StrokeColor = Color.FromArgb("#E8F5E9");
        canvas.StrokeSize = 1;
        canvas.FontColor = Color.FromArgb("#5F6E63");
        canvas.FontSize = 10;

        int steps = 2;

        for (int i = 0; i <= steps; i++)
        {
            int value = (int)Math.Round(i * (maxFocus / (double)steps));
            float normalized = value / (float)maxFocus;
            float y = chartBottom - normalized * chartHeight;

            canvas.DrawLine(chartLeft, y, chartRight, y);

            canvas.DrawString(
                value.ToString(),
                0,
                y - 8,
                leftPadding - 6,
                16,
                HorizontalAlignment.Right,
                VerticalAlignment.Center);
        }
    }

    private static void DrawXLabel(
        ICanvas canvas,
        List<DailyPomodoroStat> visibleStats,
        int index,
        float x,
        float chartBottom)
    {
        string label;

        if (visibleStats.Count <= 7)
            label = visibleStats[index].Date.ToString("MMM d");
        else
            label = index % 2 == 0 ? visibleStats[index].Date.ToString("MMM") : "";

        if (string.IsNullOrWhiteSpace(label))
            return;

        canvas.FontColor = Color.FromArgb("#0B2B14");
        canvas.FontSize = 10;

        canvas.DrawString(
            label,
            x - 22,
            chartBottom + 8,
            44,
            18,
            HorizontalAlignment.Center,
            VerticalAlignment.Top);
    }

    private static void DrawPoint(ICanvas canvas, float x, float y, bool hasFocus, bool isLastPoint)
    {
        float radius = isLastPoint ? 6 : 4;

        canvas.FillColor = hasFocus
            ? Color.FromArgb("#2E7D32")
            : Color.FromArgb("#B8CDB2");

        canvas.FillCircle(x, y, radius);

        canvas.StrokeColor = Colors.White;
        canvas.StrokeSize = 2;
        canvas.DrawCircle(x, y, radius);
    }

    private static void DrawEmptyChart(ICanvas canvas, RectF dirtyRect, List<DailyPomodoroStat> visibleStats)
    {
        canvas.FontColor = Color.FromArgb("#5F6E63");
        canvas.FontSize = 14;

        canvas.DrawString(
            "No focus data in this range",
            0,
            dirtyRect.Height / 2 - 10,
            dirtyRect.Width,
            24,
            HorizontalAlignment.Center,
            VerticalAlignment.Center);
    }
}