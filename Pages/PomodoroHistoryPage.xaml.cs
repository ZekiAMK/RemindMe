using System.Collections.ObjectModel;
using RemindMe.Models;
using RemindMe.Services;

namespace RemindMe.Pages;

public partial class PomodoroHistoryPage : ContentPage
{
    public ObservableCollection<PomodoroSession> Sessions { get; set; } = new();

    private readonly DatabaseService _db;

    public bool IsSelectionMode { get; set; }

    public int SelectedCount => Sessions.Count(s => s.IsSelected);

    public PomodoroHistoryPage()
    {
        InitializeComponent();

        string dbPath = Path.Combine(FileSystem.AppDataDirectory, "reminders.db");
        _db = new DatabaseService(dbPath);

        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadSessionsAsync();
    }

    private async Task LoadSessionsAsync()
    {
        var data = await _db.GetPomodoroSessionsAsync();

        Sessions.Clear();

        foreach (var item in data)
            Sessions.Add(item);

        RefreshBinding();
    }

    private void OnSelectSessionClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is PomodoroSession session)
            EnterSelectionMode(session);
    }

    private void OnSessionTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not PomodoroSession session)
            return;

        if (!IsSelectionMode)
            return;

        ToggleSelection(session);
    }

    private void EnterSelectionMode(PomodoroSession session)
    {
        IsSelectionMode = true;
        session.IsSelected = true;
        RefreshBinding();
    }

    private void ToggleSelection(PomodoroSession session)
    {
        session.IsSelected = !session.IsSelected;

        if (!Sessions.Any(s => s.IsSelected))
            IsSelectionMode = false;

        RefreshBinding();
    }

    private void OnCancelSelectionClicked(object? sender, EventArgs e)
    {
        foreach (var session in Sessions)
            session.IsSelected = false;

        IsSelectionMode = false;
        RefreshBinding();
    }

    private async void OnDeleteSelectedClicked(object? sender, EventArgs e)
    {
        var selected = Sessions.Where(s => s.IsSelected).ToList();

        if (selected.Count == 0)
            return;

        bool confirmed = await DisplayAlertAsync(
            "Delete selected sessions?",
            $"This will delete {selected.Count} Pomodoro session(s).",
            "Delete",
            "Cancel");

        if (!confirmed)
            return;

        foreach (var session in selected)
            await _db.DeletePomodoroSessionAsync(session);

        IsSelectionMode = false;
        await LoadSessionsAsync();
    }

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