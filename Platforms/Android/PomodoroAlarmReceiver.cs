using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using Microsoft.Maui.Storage;

namespace RemindMe.Services;

[BroadcastReceiver(Enabled = true, Exported = false)]
public class PomodoroAlarmReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null || intent == null)
            return;

        Log.Debug("RemindMePomodoro", "Pomodoro alarm fired!");

        AndroidNotificationService.CreateNotificationChannel(context);

        bool isFocusMode = intent.GetBooleanExtra("isFocusMode", true);

        Preferences.Set("PomodoroIsRunning", false);
        Preferences.Set("PomodoroRemainingSeconds", 0);
        Preferences.Set("PomodoroLastUpdated", DateTime.Now.ToString("O"));

        Notification.Builder builder;

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            builder = new Notification.Builder(context, AndroidNotificationService.ChannelId);
        else
            builder = new Notification.Builder(context);

        if (isFocusMode)
        {
            var breakIntent = new Intent(context, typeof(PomodoroActionReceiver));
            breakIntent.SetAction("POMODORO_BREAK");

            var breakPendingIntent = PendingIntent.GetBroadcast(
                context,
                910001,
                breakIntent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            var continueIntent = new Intent(context, typeof(PomodoroActionReceiver));
            continueIntent.SetAction("POMODORO_CONTINUE");

            var continuePendingIntent = PendingIntent.GetBroadcast(
                context,
                910002,
                continueIntent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            var notification = builder
                .SetContentTitle("Focus completed")
                .SetContentText("Take a break or continue your focus session.")
                .SetSmallIcon(Resource.Mipmap.appicon)
                .SetAutoCancel(true)
                .AddAction(0, "Break", breakPendingIntent)
                .AddAction(0, "Continue", continuePendingIntent)
                .Build();

            var manager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
            manager?.Notify(AndroidNotificationService.PomodoroNotificationId, notification);
        }
        else
        {
            var startFocusIntent = new Intent(context, typeof(PomodoroActionReceiver));
            startFocusIntent.SetAction("POMODORO_START_FOCUS");

            var startFocusPendingIntent = PendingIntent.GetBroadcast(
                context,
                910003,
                startFocusIntent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            var notification = builder
                .SetContentTitle("Break finished")
                .SetContentText("Time to get back to work.")
                .SetSmallIcon(Resource.Mipmap.appicon)
                .SetAutoCancel(true)
                .AddAction(0, "Start Focus", startFocusPendingIntent)
                .Build();

            var manager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
            manager?.Notify(AndroidNotificationService.PomodoroNotificationId, notification);
        }
    }
}