using RemindMe.Models;
using RemindMe.Pages;
using System.Collections.Generic;

namespace RemindMe;

public partial class MainPage : ContentPage
{
    private List<ReminderItem> AllReminders { get; set; }
    public List<ReminderItem> Reminders { get; set; }

    public MainPage()
    {
        InitializeComponent();

        AllReminders = new List<ReminderItem>
        {
            new ReminderItem { Title = "Gym", Description = "Leg day", ReminderTime = DateTime.Now.AddHours(2), IsCompleted = false, HasAlert = true },
            new ReminderItem { Title = "Study", Description = "Algorithms", ReminderTime = DateTime.Now.AddHours(4), IsCompleted = false, HasAlert = true }
        };

        Reminders = AllReminders.Where(r => r.HasAlert).ToList();

        RefreshBinding();
    }

    private async void OnAddReminderClicked(object? sender, EventArgs e)
    {
        AddReminderPage.OnReminderAdded = (reminder) =>
        {
            AllReminders.Add(reminder);

            if (reminder.HasAlert)
                Reminders = AllReminders.Where(r => r.HasAlert).ToList();
            else
                Reminders = AllReminders.Where(r => !r.HasAlert).ToList();

            RefreshBinding();
        };

        await Navigation.PushAsync(new AddReminderPage());
    }

    private void OnAllClicked(object? sender, EventArgs e)
    {
        Reminders = AllReminders.Where(r => r.HasAlert).ToList();
        RefreshBinding();
    }

    private void OnTodayClicked(object? sender, EventArgs e)
    {
        Reminders = AllReminders
            .Where(r => r.HasAlert &&
                        r.ReminderTime.HasValue &&
                        r.ReminderTime.Value.Date == DateTime.Today)
            .ToList();

        RefreshBinding();
    }

    private void OnNoAlertClicked(object? sender, EventArgs e)
    {
        Reminders = AllReminders.Where(r => !r.HasAlert).ToList();
        RefreshBinding();
    }

    private void OnImportantClicked(object? sender, EventArgs e)
    {
        Reminders = AllReminders.Where(r => r.IsImportant).ToList();
        RefreshBinding();
    }

    private void OnCompletedClicked(object? sender, EventArgs e)
    {
        Reminders = AllReminders.Where(r => r.IsCompleted).ToList();
        RefreshBinding();
    }

    private void OnPastClicked(object? sender, EventArgs e)
    {
        Reminders = AllReminders
            .Where(r => r.HasAlert &&
                        !r.IsCompleted &&
                        r.ReminderTime.HasValue &&
                        r.ReminderTime.Value < DateTime.Now)
            .ToList();

        RefreshBinding();
    }

    private void RefreshBinding()
    {
        BindingContext = null;
        BindingContext = this;
    }
}