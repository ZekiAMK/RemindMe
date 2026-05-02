using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;

namespace RemindMe.Services;

[BroadcastReceiver(Enabled = true, Exported = false)]
public class ReminderAlarmReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null || intent == null)
            return;

        Log.Debug("RemindMeAlarm", "Receiver fired!");

        AndroidNotificationService.CreateNotificationChannel(context);

        int id = intent.GetIntExtra("id", Random.Shared.Next(1000, 9999));
        string title = intent.GetStringExtra("title") ?? "Reminder";
        string description = intent.GetStringExtra("description") ?? "Reminder";

        Notification.Builder builder;

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            builder = new Notification.Builder(context, AndroidNotificationService.ChannelId);
        }
        else
        {
            builder = new Notification.Builder(context);
        }

        // COMPLETE intent
        var completeIntent = new Intent(context, typeof(ReminderActionReceiver));
        completeIntent.SetAction("COMPLETE");
        completeIntent.PutExtra("id", id);

        var completePendingIntent = PendingIntent.GetBroadcast(
            context,
            id + 1000,
            completeIntent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        // SNOOZE intent
        var snoozeIntent = new Intent(context, typeof(ReminderActionReceiver));
        snoozeIntent.SetAction("SNOOZE");
        snoozeIntent.PutExtra("id", id);
        snoozeIntent.PutExtra("title", title);
        snoozeIntent.PutExtra("description", description);

        var snoozePendingIntent = PendingIntent.GetBroadcast(
            context,
            id + 2000,
            snoozeIntent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        // notification builder
        var notification = builder
            .SetContentTitle(title)
            .SetContentText(description)
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetAutoCancel(true)
            .AddAction(0, "Complete", completePendingIntent)
            .AddAction(0, "Snooze 15 min", snoozePendingIntent)
            .Build();

        var manager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
        manager?.Notify(id, notification);
    }
}