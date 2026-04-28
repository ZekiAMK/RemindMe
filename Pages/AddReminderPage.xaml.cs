using RemindMe.Models;

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
            return;

        // Update page title
        PageTitle.Text = "Edit Reminder";
        DeleteButton.IsVisible = true;

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

        OnReminderDeleted?.Invoke(_reminderToEdit);
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