using System.Collections.ObjectModel;
using RemindMe.Models;
using RemindMe.Services;

namespace RemindMe.Pages;

public partial class PomodoroStatsPage : ContentPage
{
    private readonly DatabaseService _db;

    public ObservableCollection<DailyPomodoroStat> DailyStats { get; set; } = new();

    public string SummaryText { get; set; } = "Your focus progress over time.";

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
        await LoadData(7);
    }

    private async Task LoadData(int days)
    {
        var allSessions = await _db.GetPomodoroSessionsAsync();

        DateTime startDate = DateTime.Today.AddDays(-(days - 1));

        var filteredSessions = allSessions
            .Where(s => s.StartedAt.Date >= startDate)
            .ToList();

        var groupedStats = filteredSessions
            .GroupBy(s => s.StartedAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                FocusMinutes = g.Sum(s => s.FocusMinutes),
                SessionCount = g.Count(s => s.FocusMinutes > 0)
            })
            .OrderBy(x => x.Date)
            .ToList();

        int maxFocus = groupedStats.Count == 0
            ? 1
            : Math.Max(1, groupedStats.Max(x => x.FocusMinutes));

        DailyStats.Clear();

        foreach (var stat in groupedStats)
        {
            DailyStats.Add(new DailyPomodoroStat
            {
                Date = stat.Date,
                FocusMinutes = stat.FocusMinutes,
                SessionCount = stat.SessionCount,
                Progress = (double)stat.FocusMinutes / maxFocus
            });
        }

        int totalFocus = filteredSessions.Sum(s => s.FocusMinutes);
        int totalSessions = filteredSessions.Count(s => s.FocusMinutes > 0);

        SummaryText = $"{totalFocus} focused minutes • {totalSessions} focus sessions";

        RefreshBinding();
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

    public int FocusMinutes { get; set; }

    public int SessionCount { get; set; }

    public double Progress { get; set; }

    public string DateLabel => Date.ToString("MMM d");

    public string FocusText => $"{FocusMinutes} min";

    public string SessionsText => $"{SessionCount} session(s)";
}