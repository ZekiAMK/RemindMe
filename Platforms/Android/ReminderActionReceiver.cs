using Android.App;
using Android.Content;
using RemindMe.Models;
using SQLite;

namespace RemindMe.Services;

[BroadcastReceiver(Enabled = true, Exported = false)]
public class ReminderActionReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null || intent == null)
            return;

        string dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "reminders.db");

        var db = new SQLiteConnection(dbPath);
        db.CreateTable<ReminderItem>();

        var action = intent.Action;
        int id = intent.GetIntExtra("id", -1);

        if (id == -1)
            return;

        var reminder = db.Table<ReminderItem>().FirstOrDefault(r => r.Id == id);

        if (reminder == null)
            return;

        var manager = (NotificationManager?)context.GetSystemService(Context.NotificationService);

        if (action == "COMPLETE")
        {
            reminder.IsCompleted = true;
            reminder.IsSelected = false;

            db.Update(reminder);
            manager?.Cancel(id);
        }

        if (action == "SNOOZE")
        {
            DateTime newTime = DateTime.Now.AddMinutes(15);

            reminder.ReminderTime = newTime;
            reminder.HasAlert = true;
            reminder.IsCompleted = false;
            reminder.IsSelected = false;

            db.Update(reminder);

            AndroidNotificationService.CancelNotification(id);
            AndroidNotificationService.ScheduleNotification(
                reminder.Id,
                reminder.Title,
                reminder.Description,
                newTime);

            manager?.Cancel(id);
        }
    }
}