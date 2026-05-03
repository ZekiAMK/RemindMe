using RemindMe.Models;
using RemindMe.Services;
namespace RemindMe.Pages;

public partial class AddReminderPage : ContentPage
{
    public static Action<ReminderItem>? OnReminderAdded;
    public static Action<ReminderItem>? OnReminderDeleted;
    private ReminderItem? _reminderToEdit;

    public AddReminderPage()
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
        _reminderToEdit = null;
    }

    public AddReminderPage(ReminderItem reminder)
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
        _reminderToEdit = reminder;

        // Initialize the form after components are created
        this.Loaded += (s, e) => PreFillForm();
    }

    private void PreFillForm()
    {
        if (_reminderToEdit == null)
        {
            // Add mode: Show Save and Cancel
            SaveAction.IsVisible = true;
            RestoreAction.IsVisible = false;
            DeleteAction.IsVisible = false;
            CancelAction.IsVisible = true;
            return;
        }

        // Update page title
        PageTitle.Text = "Edit Reminder";

        if (_reminderToEdit.IsCompleted)
        {
            // Completed edit mode: Show Restore, Delete, Cancel
            SaveAction.IsVisible = false;
            RestoreAction.IsVisible = true;
            DeleteAction.IsVisible = true;
            CancelAction.IsVisible = true;
        }
        else
        {
            // Active edit mode: Show Save, Delete, Cancel
            SaveAction.IsVisible = true;
            RestoreAction.IsVisible = false;
            DeleteAction.IsVisible = true;
            CancelAction.IsVisible = true;
        }

        ImportantRow.IsVisible = !_reminderToEdit.IsCompleted;
        SetAlertRow.IsVisible = !_reminderToEdit.IsCompleted;
        DateRow.IsVisible = _reminderToEdit.HasAlert && !_reminderToEdit.IsCompleted;
        TimeRow.IsVisible = _reminderToEdit.HasAlert && !_reminderToEdit.IsCompleted;

        // Pre-fill the fields
        TitleEntry.Text = _reminderToEdit.Title;
        DescriptionEntry.Text = _reminderToEdit.Description;
        ImportantSwitch.IsToggled = _reminderToEdit.IsImportant;
        SetAlertSwitch.IsToggled = _reminderToEdit.HasAlert;

        // Pre-fill date and time if alert is set
        if (_reminderToEdit.HasAlert && _reminderToEdit.ReminderTime.HasValue)
        {
            ReminderDatePicker.Date = _reminderToEdit.ReminderTime.Value.Date;
            ReminderTimePicker.Time = _reminderToEdit.ReminderTime.Value.TimeOfDay;
        }
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (_reminderToEdit == null)
            return;

        bool confirmed = await DisplayAlertAsync(
            "Delete reminder?",
            "This action cannot be undone.",
            "Delete",
            "Cancel");

        if (!confirmed)
            return;

        await NotificationService.CancelNotification(_reminderToEdit.Id);

        OnReminderDeleted?.Invoke(_reminderToEdit);
        await Navigation.PopAsync();
    }

    private async void OnRestoreClicked(object? sender, EventArgs e)
    {
        if (_reminderToEdit == null)
            return;

        if (string.IsNullOrWhiteSpace(TitleEntry.Text))
        {
            await DisplayAlertAsync("Title required", "Please enter a reminder title.", "OK");
            return;
        }

        _reminderToEdit.Title = TitleEntry.Text.Trim();
        _reminderToEdit.Description = DescriptionEntry.Text?.Trim() ?? string.Empty;
        _reminderToEdit.IsCompleted = false;

        OnReminderAdded?.Invoke(_reminderToEdit);

        await Navigation.PopAsync();
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleEntry.Text))
        {
            await DisplayAlertAsync("Title required", "Please enter a reminder title.", "OK");
            return;
        }

        DateTime? reminderTime = null;
        bool hasAlert = SetAlertSwitch.IsToggled;

        if (hasAlert)
        {
            DateTime selectedDate = ReminderDatePicker.Date.GetValueOrDefault(DateTime.Today);
            TimeSpan selectedTime = ReminderTimePicker.Time.GetValueOrDefault(TimeSpan.Zero);
            reminderTime = selectedDate.Date + selectedTime;
        }

        ReminderItem reminder;

        if (_reminderToEdit != null)
        {
            // Update existing reminder
            _reminderToEdit.Title = TitleEntry.Text.Trim();
            _reminderToEdit.Description = DescriptionEntry.Text?.Trim() ?? string.Empty;
            _reminderToEdit.ReminderTime = reminderTime;
            _reminderToEdit.IsImportant = ImportantSwitch.IsToggled;
            _reminderToEdit.HasAlert = hasAlert;
            reminder = _reminderToEdit;
        }
        else
        {
            // Create new reminder
            reminder = new ReminderItem
            {
                Title = TitleEntry.Text.Trim(),
                Description = DescriptionEntry.Text?.Trim() ?? string.Empty,
                ReminderTime = reminderTime,
                IsCompleted = false,
                IsImportant = ImportantSwitch.IsToggled,
                HasAlert = hasAlert
            };
        }

        OnReminderAdded?.Invoke(reminder);

        await Navigation.PopAsync();
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}