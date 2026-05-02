using RemindMe.Models;
using Microsoft.Maui.Storage;
using System.Text.Json;

namespace RemindMe.Services;

public static class ReminderStorageService
{
    private const string StorageKey = "reminders";

    public static List<ReminderItem> LoadReminders()
    {
        var json = Preferences.Get(StorageKey, string.Empty);

        if (string.IsNullOrWhiteSpace(json))
            return new List<ReminderItem>();

        return JsonSerializer.Deserialize<List<ReminderItem>>(json) ?? new List<ReminderItem>();
    }

    public static void SaveReminders(List<ReminderItem> reminders)
    {
        foreach (var reminder in reminders)
            reminder.IsSelected = false;

        var json = JsonSerializer.Serialize(reminders);
        Preferences.Set(StorageKey, json);
    }

    public static void MarkReminderCompleted(int id)
    {
        var reminders = LoadReminders();
        var reminder = reminders.FirstOrDefault(r => r.Id == id);

        if (reminder == null)
            return;

        reminder.IsCompleted = true;
        reminder.IsSelected = false;

        SaveReminders(reminders);
    }

    public static void SnoozeReminder(int id, DateTime newTime)
    {
        var reminders = LoadReminders();
        var reminder = reminders.FirstOrDefault(r => r.Id == id);

        if (reminder == null)
            return;

        reminder.ReminderTime = newTime;
        reminder.HasAlert = true;
        reminder.IsCompleted = false;
        reminder.IsSelected = false;

        SaveReminders(reminders);
    }
}