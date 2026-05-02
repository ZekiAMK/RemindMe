using Android.App;
using Android.Content;

namespace RemindMe.Services;

[BroadcastReceiver(Enabled = true, Exported = false)]
public class ReminderActionReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null || intent == null)
            return;

        var action = intent.Action;
        int id = intent.GetIntExtra("id", -1);

        if (action == "COMPLETE")
        {
            ReminderStorageService.MarkReminderCompleted(id);

            var manager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
            manager?.Cancel(id);
        }

        if (action == "SNOOZE")
        {
            string title = intent.GetStringExtra("title") ?? "Reminder";
            string description = intent.GetStringExtra("description") ?? "Reminder";

            var newTime = DateTime.Now.AddMinutes(15);

            ReminderStorageService.SnoozeReminder(id, newTime);

            AndroidNotificationService.ScheduleNotification(
                id,
                title,
                description,
                newTime);

            var manager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
            manager?.Cancel(id);
        }
    }
}