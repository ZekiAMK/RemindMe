using Microsoft.Maui.Graphics;
using RemindMe.Models;
using RemindMe.Pages;
using System.Text.Json;

namespace RemindMe;

public partial class MainPage : ContentPage
{
    private const string StorageKey = "reminders";

    private List<ReminderItem> AllReminders { get; set; }
    public List<ReminderItem> Reminders { get; set; } = new();

    private string _activeFilter = "All";

    public string SelectionPrimaryActionText =>
        _activeFilter == "Completed" ? "Restore" : "Complete";

    public Color SelectionPrimaryActionColor =>
        _activeFilter == "Completed"
            ? Color.FromArgb("#1976D2")
            : Color.FromArgb("#2E7D32");

    public bool IsSelectionMode { get; set; }

    public int SelectedCount => AllReminders.Count(r => r.IsSelected);

    public MainPage()
    {
        InitializeComponent();

        AllReminders = LoadReminders();

        if (AllReminders.Count == 0)
        {
            AllReminders = new List<ReminderItem>
            {
                new ReminderItem { Title = "Gym", Description = "Leg day", ReminderTime = DateTime.Now.AddHours(2), IsCompleted = false, HasAlert = true },
                new ReminderItem { Title = "Study", Description = "Algorithms", ReminderTime = DateTime.Now.AddHours(4), IsCompleted = false, HasAlert = true }
            };

            SaveReminders();
        }

        ApplyFilter(_activeFilter);
    }

    private async void OnAddReminderClicked(object? sender, EventArgs e)
    {
        AddReminderPage.OnReminderAdded = (reminder) =>
        {
            AllReminders.Add(reminder);
            SaveReminders();
            ApplyFilter(_activeFilter);
        };

        await Navigation.PushAsync(new AddReminderPage());
    }

    private void OnAllClicked(object? sender, EventArgs e) => ApplyFilter("All");
    private void OnTodayClicked(object? sender, EventArgs e) => ApplyFilter("Today");
    private void OnNoAlertClicked(object? sender, EventArgs e) => ApplyFilter("No Alert");
    private void OnImportantClicked(object? sender, EventArgs e) => ApplyFilter("Important");
    private void OnCompletedClicked(object? sender, EventArgs e) => ApplyFilter("Completed");
    private void OnPastClicked(object? sender, EventArgs e) => ApplyFilter("Past");

    private void ApplyFilter(string filter)
    {
        _activeFilter = filter;

        Reminders = filter switch
        {
            "All" => AllReminders
                .Where(r => r.HasAlert && !r.IsCompleted)
                .OrderBy(r => r.ReminderTime)
                .ToList(),

            "Today" => AllReminders
                .Where(r => r.HasAlert &&
                            !r.IsCompleted &&
                            r.ReminderTime.HasValue &&
                            r.ReminderTime.Value.Date == DateTime.Today)
                .OrderBy(r => r.ReminderTime)
                .ToList(),

            "No Alert" => AllReminders
                .Where(r => !r.HasAlert && !r.IsCompleted)
                .OrderBy(r => r.Title)
                .ToList(),

            "Important" => AllReminders
                .Where(r => r.IsImportant && !r.IsCompleted)
                .OrderBy(r => r.ReminderTime)
                .ToList(),

            "Completed" => AllReminders
                .Where(r => r.IsCompleted)
                .OrderBy(r => r.ReminderTime)
                .ToList(),

            "Past" => AllReminders
                .Where(r => r.HasAlert &&
                            !r.IsCompleted &&
                            r.ReminderTime.HasValue &&
                            r.ReminderTime.Value < DateTime.Now)
                .OrderBy(r => r.ReminderTime)
                .ToList(),

            _ => AllReminders
                .Where(r => r.HasAlert && !r.IsCompleted)
                .OrderBy(r => r.ReminderTime)
                .ToList()
        };

        RefreshBinding();
    }

    private void RefreshBinding()
    {
        BindingContext = null;
        BindingContext = this;
    }

    private void SaveReminders()
    {
        var json = JsonSerializer.Serialize(AllReminders);
        Preferences.Set(StorageKey, json);
    }

    private List<ReminderItem> LoadReminders()
    {
        var json = Preferences.Get(StorageKey, string.Empty);

        if (string.IsNullOrWhiteSpace(json))
            return new List<ReminderItem>();

        return JsonSerializer.Deserialize<List<ReminderItem>>(json) ?? new List<ReminderItem>();
    }

    private async void OnReminderCardTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not ReminderItem reminder)
            return;

        if (IsSelectionMode)
        {
            ToggleSelection(reminder);
            return;
        }

        AddReminderPage.OnReminderAdded = (updatedReminder) =>
        {
            int index = AllReminders.IndexOf(reminder);

            if (index >= 0)
                AllReminders[index] = updatedReminder;

            SaveReminders();
            ApplyFilter(_activeFilter);
        };

        AddReminderPage.OnReminderDeleted = (deletedReminder) =>
        {
            AllReminders.Remove(deletedReminder);
            SaveReminders();
            ApplyFilter(_activeFilter);
        };

        await Navigation.PushAsync(new AddReminderPage(reminder));
    }

    private async void OnCompleteTapped(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not ReminderItem reminder)
            return;

        if (IsSelectionMode)
        {
            ToggleSelection(reminder);
            return;
        }

        button.Text = "✓";
        button.BackgroundColor = Color.FromArgb("#2E7D32");
        button.TextColor = Colors.White;
        button.BorderColor = Colors.Transparent;
        button.IsEnabled = false;

        if (button.Parent is Grid grid && grid.Parent is Border card)
            await card.FadeToAsync(0, 300);

        reminder.IsCompleted = true;
        SaveReminders();

        await Task.Delay(300);
        ApplyFilter(_activeFilter);
    }

    private void OnCompleteSelectedClicked(object? sender, EventArgs e)
    {
        var selected = AllReminders.Where(r => r.IsSelected).ToList();

        if (selected.Count == 0)
            return;

        bool shouldRestore = _activeFilter == "Completed";

        foreach (var reminder in selected)
        {
            reminder.IsCompleted = !shouldRestore;
            reminder.IsSelected = false;
        }

        SaveReminders();
        IsSelectionMode = false;
        ApplyFilter(_activeFilter);
    }

    private void OnSelectModeClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is ReminderItem reminder)
            EnterSelectionMode(reminder);
    }

    private void ToggleSelection(ReminderItem reminder)
    {
        reminder.IsSelected = !reminder.IsSelected;

        if (!AllReminders.Any(r => r.IsSelected))
            IsSelectionMode = false;

        RefreshBinding();
    }

    private void EnterSelectionMode(ReminderItem reminder)
    {
        IsSelectionMode = true;
        reminder.IsSelected = true;
        RefreshBinding();
    }

    private void OnCancelSelectionClicked(object? sender, EventArgs e)
    {
        foreach (var reminder in AllReminders)
            reminder.IsSelected = false;

        IsSelectionMode = false;
        RefreshBinding();
    }

    private async void OnDeleteSelectedClicked(object? sender, EventArgs e)
    {
        var selected = AllReminders.Where(r => r.IsSelected).ToList();

        if (selected.Count == 0)
            return;

        bool confirmed = await DisplayAlertAsync(
            "Delete selected reminders?",
            $"This will delete {selected.Count} reminder(s).",
            "Delete",
            "Cancel");

        if (!confirmed)
            return;

        foreach (var reminder in selected)
            AllReminders.Remove(reminder);

        SaveReminders();
        IsSelectionMode = false;
        ApplyFilter(_activeFilter);
    }
}