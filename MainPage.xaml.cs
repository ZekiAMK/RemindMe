using Microsoft.Maui.Graphics;
using RemindMe.Models;
using RemindMe.Pages;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RemindMe;

public partial class MainPage : ContentPage
{
    private List<ReminderItem> AllReminders { get; set; }
    public List<ReminderItem> Reminders { get; set; }
    private string _activeFilter = "All";

    public MainPage()
    {
        InitializeComponent();

        AllReminders = new List<ReminderItem>
        {
            new ReminderItem { Title = "Gym", Description = "Leg day", ReminderTime = DateTime.Now.AddHours(2), IsCompleted = false, HasAlert = true },
            new ReminderItem { Title = "Study", Description = "Algorithms", ReminderTime = DateTime.Now.AddHours(4), IsCompleted = false, HasAlert = true }
        };

        ApplyFilter(_activeFilter);
    }

    private async void OnAddReminderClicked(object? sender, EventArgs e)
    {
        AddReminderPage.OnReminderAdded = (reminder) =>
        {
            AllReminders.Add(reminder);
            ApplyFilter(_activeFilter);
        };

        await Navigation.PushAsync(new AddReminderPage());
    }

    private void OnAllClicked(object? sender, EventArgs e)
    {
        ApplyFilter("All");
    }

    private void OnTodayClicked(object? sender, EventArgs e)
    {
        ApplyFilter("Today");
    }

    private void OnNoAlertClicked(object? sender, EventArgs e)
    {
        ApplyFilter("No Alert");
    }

    private void OnImportantClicked(object? sender, EventArgs e)
    {
        ApplyFilter("Important");
    }

    private void OnCompletedClicked(object? sender, EventArgs e)
    {
        ApplyFilter("Completed");
    }

    private void OnPastClicked(object? sender, EventArgs e)
    {
        ApplyFilter("Past");
    }

    private void ApplyFilter(string filter)
    {
        _activeFilter = filter;

        switch (filter)
        {
            case "All":
                Reminders = AllReminders
                    .Where(r => r.HasAlert && !r.IsCompleted)
                    .OrderBy(r => r.ReminderTime)
                    .ToList();
                break;
            case "Today":
                Reminders = AllReminders
                    .Where(r => r.HasAlert &&
                                !r.IsCompleted &&
                                r.ReminderTime.HasValue &&
                                r.ReminderTime.Value.Date == DateTime.Today)
                    .OrderBy(r => r.ReminderTime)
                    .ToList();
                break;
            case "No Alert":
                Reminders = AllReminders
                    .Where(r => !r.HasAlert && !r.IsCompleted)
                    .OrderBy(r => r.Title)
                    .ToList();
                break;
            case "Important":
                Reminders = AllReminders
                    .Where(r => r.IsImportant && !r.IsCompleted)
                    .OrderBy(r => r.ReminderTime)
                    .ToList();
                break;
            case "Completed":
                Reminders = AllReminders
                    .Where(r => r.IsCompleted)
                    .OrderBy(r => r.ReminderTime)
                    .ToList();
                break;
            case "Past":
                Reminders = AllReminders
                    .Where(r => r.HasAlert &&
                                !r.IsCompleted &&
                                r.ReminderTime.HasValue &&
                                r.ReminderTime.Value < DateTime.Now)
                    .OrderBy(r => r.ReminderTime)
                    .ToList();
                break;
            default:
                Reminders = AllReminders
                    .Where(r => r.HasAlert)
                    .OrderBy(r => r.ReminderTime)
                    .ToList();
                break;
        }

        RefreshBinding();
    }

    private void RefreshBinding()
    {
        BindingContext = null;
        BindingContext = this;
    }

    private async void OnReminderCardTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is ReminderItem reminder)
        {
            AddReminderPage.OnReminderAdded = (updatedReminder) =>
            {
                int index = AllReminders.IndexOf(reminder);
                if (index >= 0)
                {
                    AllReminders[index] = updatedReminder;
                }

                ApplyFilter(_activeFilter);
            };

            AddReminderPage.OnReminderDeleted = (deletedReminder) =>
            {
                AllReminders.Remove(deletedReminder);
                ApplyFilter(_activeFilter);
            };

            await Navigation.PushAsync(new AddReminderPage(reminder));
        }
    }

    private async void OnCompleteTapped(object? sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is ReminderItem reminder)
        {
            button.Text = "✓";
            button.BackgroundColor = Color.FromArgb("#2E7D32");
            button.TextColor = Colors.White;
            button.BorderColor = Colors.Transparent;
            button.IsEnabled = false;

            if (button.Parent is Grid grid && grid.Parent is Border card)
            {
                await card.FadeToAsync(0, 300);
            }

            reminder.IsCompleted = true;
            await Task.Delay(300);
            ApplyFilter(_activeFilter);
        }
    }
}