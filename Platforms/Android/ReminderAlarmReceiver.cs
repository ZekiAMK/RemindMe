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

        var notification = builder
            .SetContentTitle(title)
            .SetContentText(description)
            .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)
            .SetAutoCancel(true)
            .Build();

        var manager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
        manager?.Notify(id, notification);
    }
}