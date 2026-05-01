using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Microsoft.Maui.ApplicationModel;
using Android.Util;

namespace RemindMe.Services;

public static class AndroidNotificationService
{
    public const string ChannelId = "remindme_channel";

    public static void ScheduleNotification(int id, string title, string description, DateTime notifyTime)
    {
        var context = Platform.AppContext;

        CreateNotificationChannel(context);

        var intent = new Intent(context, typeof(ReminderAlarmReceiver));
        intent.PutExtra("id", id);
        intent.PutExtra("title", title);
        intent.PutExtra("description", string.IsNullOrWhiteSpace(description) ? "Reminder" : description);

        var pendingIntent = PendingIntent.GetBroadcast(
            context,
            id,
            intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        var alarmManager = (AlarmManager?)context.GetSystemService(Context.AlarmService);

        if (alarmManager == null)
            return;

        if (Build.VERSION.SdkInt >= BuildVersionCodes.S && !alarmManager.CanScheduleExactAlarms())
        {
            var settingsIntent = new Intent(Settings.ActionRequestScheduleExactAlarm);
            settingsIntent.SetFlags(ActivityFlags.NewTask);
            context.StartActivity(settingsIntent);
            return;
        }

        long triggerTime = new DateTimeOffset(notifyTime).ToUnixTimeMilliseconds();

        Log.Debug("RemindMeAlarm", $"Scheduling alarm. Now={DateTime.Now}, Notify={notifyTime}, Trigger={triggerTime}");

        if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
        {
            alarmManager.SetExactAndAllowWhileIdle(
                AlarmType.RtcWakeup,
                triggerTime,
                pendingIntent);
        }
        else
        {
            alarmManager.SetExact(
                AlarmType.RtcWakeup,
                triggerTime,
                pendingIntent);
        }
    }

    public static void CancelNotification(int id)
    {
        var context = Platform.AppContext;

        var intent = new Intent(context, typeof(ReminderAlarmReceiver));

        var pendingIntent = PendingIntent.GetBroadcast(
            context,
            id,
            intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        var alarmManager = (AlarmManager?)context.GetSystemService(Context.AlarmService);
        alarmManager?.Cancel(pendingIntent);
    }

    public static void CreateNotificationChannel(Context context)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            return;

        var channel = new NotificationChannel(
            ChannelId,
            "RemindMe reminders",
            NotificationImportance.High);

        channel.Description = "Reminder notifications";

        var manager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
        manager?.CreateNotificationChannel(channel);
    }
}