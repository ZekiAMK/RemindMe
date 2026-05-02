using Microsoft.Maui.Graphics;
using RemindMe.Models;
using RemindMe.Pages;
using RemindMe.Services;

namespace RemindMe;

public partial class MainPage : ContentPage
{
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
                new ReminderItem { Id = Random.Shared.Next(), Title = "Gym", Description = "Leg day", ReminderTime = DateTime.Now.AddHours(2), IsCompleted = false, HasAlert = true },
                new ReminderItem { Id = Random.Shared.Next(), Title = "Study", Description = "Algorithms", ReminderTime = DateTime.Now.AddHours(4), IsCompleted = false, HasAlert = true }
            };

            SaveReminders();
        }

        ApplyFilter(_activeFilter);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        AllReminders = LoadReminders();
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
        ReminderStorageService.SaveReminders(AllReminders);
    }

    private List<ReminderItem> LoadReminders()
    {
        return ReminderStorageService.LoadReminders();
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

        AddReminderPage.OnReminderDeleted = async (deletedReminder) =>
        {
            await NotificationService.CancelNotification(deletedReminder.Id);
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

        await NotificationService.CancelNotification(reminder.Id);

        reminder.IsCompleted = true;
        SaveReminders();

        ApplyFilter(_activeFilter);
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
            }

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
        {
            await NotificationService.CancelNotification(reminder.Id);
            AllReminders.Remove(reminder);
        }

        SaveReminders();
        IsSelectionMode = false;
        ApplyFilter(_activeFilter);
    }
}