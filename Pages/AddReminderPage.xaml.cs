using RemindMe.Models;

namespace RemindMe.Pages;

public partial class AddReminderPage : ContentPage
{
    public static Action<ReminderItem>? OnReminderAdded;

    public AddReminderPage()
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
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

        var reminder = new ReminderItem
        {
            Title = TitleEntry.Text.Trim(),
            Description = DescriptionEntry.Text?.Trim() ?? string.Empty,
            ReminderTime = reminderTime,
            IsCompleted = false,
            IsImportant = ImportantSwitch.IsToggled,
            HasAlert = hasAlert
        };

        OnReminderAdded?.Invoke(reminder);

        await Navigation.PopAsync();
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}