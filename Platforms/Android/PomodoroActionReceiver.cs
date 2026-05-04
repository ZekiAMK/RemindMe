using Android.App;
using Android.Content;
using Microsoft.Maui.Storage;

namespace RemindMe.Services;

[BroadcastReceiver(Enabled = true, Exported = false)]
public class PomodoroActionReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null || intent == null)
            return;

        string action = intent.Action ?? "";

        int focusMinutes = Preferences.Get("PomodoroFocusMinutes", 25);
        int breakMinutes = Preferences.Get("PomodoroBreakMinutes", 5);

        if (action == "POMODORO_BREAK")
        {
            StartPomodoroState(false, breakMinutes);
            AndroidNotificationService.SchedulePomodoroNotification(
                DateTime.Now.AddMinutes(breakMinutes),
                false);
        }

        if (action == "POMODORO_CONTINUE")
        {
            StartPomodoroState(true, focusMinutes);
            AndroidNotificationService.SchedulePomodoroNotification(
                DateTime.Now.AddMinutes(focusMinutes),
                true);
        }

        if (action == "POMODORO_START_FOCUS")
        {
            StartPomodoroState(true, focusMinutes);
            AndroidNotificationService.SchedulePomodoroNotification(
                DateTime.Now.AddMinutes(focusMinutes),
                true);
        }

        var manager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
        manager?.Cancel(AndroidNotificationService.PomodoroNotificationId);
    }

    private static void StartPomodoroState(bool isFocusMode, int minutes)
    {
        Preferences.Set("PomodoroIsRunning", true);
        Preferences.Set("PomodoroIsFocusMode", isFocusMode);
        Preferences.Set("PomodoroRemainingSeconds", minutes * 60);
        Preferences.Set("PomodoroLastUpdated", DateTime.Now.ToString("O"));
    }
}