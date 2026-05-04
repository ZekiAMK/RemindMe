using Microsoft.Maui.Graphics;
using RemindMe.Models;
using RemindMe.Pages;
using RemindMe.Services;

namespace RemindMe;

public partial class MainPage : ContentPage
{
    private readonly DatabaseService _db;

    private List<ReminderItem> AllReminders { get; set; } = new();
    public List<ReminderItem> Reminders { get; set; } = new();

    private string _activeFilter = "All";

    public string ActiveFilter => _activeFilter;

    public bool HasNoReminders => Reminders.Count == 0;

    public bool HasReminders => Reminders.Count > 0;

    public string EmptyStateMessage => _activeFilter switch
    {
        "All" => "No reminders yet",
        "Today" => "Nothing scheduled for today",
        "Important" => "No important reminders",
        "No Alert" => "No reminders without alerts",
        "Completed" => "No completed reminders yet",
        "Past" => "No overdue reminders",
        _ => "No reminders yet"
    };

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

        string dbPath = Path.Combine(FileSystem.AppDataDirectory, "reminders.db");
        _db = new DatabaseService(dbPath);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadRemindersAsync();
    }

    private async Task LoadRemindersAsync()
    {
        AllReminders = await _db.GetRemindersAsync();

        ApplyFilter(_activeFilter);
    }

    private async void OnAddReminderClicked(object? sender, EventArgs e)
    {
        AddReminderPage.OnReminderAdded = async (reminder) =>
        {
            await _db.SaveReminderAsync(reminder);

            if (reminder.HasAlert && reminder.ReminderTime.HasValue && !reminder.IsCompleted)
            {
                await NotificationService.CancelNotification(reminder.Id);

                await NotificationService.ScheduleNotification(
                    reminder.Id,
                    reminder.Title,
                    reminder.Description,
                    reminder.ReminderTime.Value);
            }

            await LoadRemindersAsync();
        };

        await Navigation.PushAsync(new AddReminderPage());
    }

    private async void OnPomodoroClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new PomodoroPage());
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

    private async void OnReminderCardTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not ReminderItem reminder)
            return;

        if (IsSelectionMode)
        {
            ToggleSelection(reminder);
            return;
        }

        AddReminderPage.OnReminderAdded = async (updatedReminder) =>
        {
            await _db.SaveReminderAsync(updatedReminder);

            await NotificationService.CancelNotification(updatedReminder.Id);

            if (updatedReminder.HasAlert &&
                updatedReminder.ReminderTime.HasValue &&
                !updatedReminder.IsCompleted)
            {
                await NotificationService.ScheduleNotification(
                    updatedReminder.Id,
                    updatedReminder.Title,
                    updatedReminder.Description,
                    updatedReminder.ReminderTime.Value);
            }

            await LoadRemindersAsync();
        };

        AddReminderPage.OnReminderDeleted = async (deletedReminder) =>
        {
            await NotificationService.CancelNotification(deletedReminder.Id);
            await _db.DeleteReminderAsync(deletedReminder);

            await LoadRemindersAsync();
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

        if (button.Parent is VerticalStackLayout stack && stack.Parent is Grid grid && grid.Parent is Border card)
        {
            await card.FadeToAsync(0, 300);
        }

        await NotificationService.CancelNotification(reminder.Id);

        reminder.IsCompleted = true;
        reminder.IsSelected = false;

        await _db.SaveReminderAsync(reminder);

        await Task.Delay(300);
        await LoadRemindersAsync();
    }

    private async void OnCompleteSelectedClicked(object? sender, EventArgs e)
    {
        var selected = AllReminders.Where(r => r.IsSelected).ToList();

        if (selected.Count == 0)
            return;

        bool shouldRestore = _activeFilter == "Completed";

        foreach (var reminder in selected)
        {
            if (shouldRestore)
            {
                reminder.IsCompleted = false;
                reminder.IsSelected = false;

                await _db.SaveReminderAsync(reminder);

                if (reminder.HasAlert && reminder.ReminderTime.HasValue)
                {
                    await NotificationService.ScheduleNotification(
                        reminder.Id,
                        reminder.Title,
                        reminder.Description,
                        reminder.ReminderTime.Value);
                }
            }
            else
            {
                await NotificationService.CancelNotification(reminder.Id);

                reminder.IsCompleted = true;
                reminder.IsSelected = false;

                await _db.SaveReminderAsync(reminder);
            }
        }

        IsSelectionMode = false;
        await LoadRemindersAsync();
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
        {
            await NotificationService.CancelNotification(reminder.Id);
            await _db.DeleteReminderAsync(reminder);
        }

        IsSelectionMode = false;
        await LoadRemindersAsync();
    }
}