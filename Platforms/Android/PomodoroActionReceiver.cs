using Android.App;
using Android.Content;
using Microsoft.Maui.Storage;
using RemindMe.Models;
using SQLite;

namespace RemindMe.Services;

[BroadcastReceiver(Enabled = true, Exported = false)]
public class PomodoroActionReceiver : BroadcastReceiver
{
    private const string SessionStartedAtKey = "PomodoroSessionStartedAt";

    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null || intent == null)
            return;

        string action = intent.Action ?? "";

        int focusMinutes = Preferences.Get("PomodoroFocusMinutes", 25);
        int breakMinutes = Preferences.Get("PomodoroBreakMinutes", 5);

        if (action == "POMODORO_BREAK")
        {
            SaveFocusSession(focusMinutes, wasContinued: false);

            StartPomodoroState(false, breakMinutes);

            AndroidNotificationService.SchedulePomodoroNotification(
                DateTime.Now.AddMinutes(breakMinutes),
                false);
        }

        if (action == "POMODORO_CONTINUE")
        {
            SaveFocusSession(focusMinutes, wasContinued: true);

            StartPomodoroState(true, focusMinutes);

            AndroidNotificationService.SchedulePomodoroNotification(
                DateTime.Now.AddMinutes(focusMinutes),
                true);
        }

        if (action == "POMODORO_START_FOCUS")
        {
            SaveBreakSession(breakMinutes);

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
        Preferences.Set(SessionStartedAtKey, DateTime.Now.ToString("O"));
    }

    private static void SaveFocusSession(int focusMinutes, bool wasContinued)
    {
        DateTime endedAt = DateTime.Now;
        DateTime startedAt = GetSessionStartedAt();

        SaveSession(new PomodoroSession
        {
            StartedAt = startedAt,
            EndedAt = endedAt,
            FocusMinutes = focusMinutes,
            BreakMinutes = 0,
            CompletedFocus = true,
            CompletedBreak = false,
            WasContinued = wasContinued,
            SessionDate = endedAt.Date
        });
    }

    private static void SaveBreakSession(int breakMinutes)
    {
        DateTime endedAt = DateTime.Now;
        DateTime startedAt = GetSessionStartedAt();

        SaveSession(new PomodoroSession
        {
            StartedAt = startedAt,
            EndedAt = endedAt,
            FocusMinutes = 0,
            BreakMinutes = breakMinutes,
            CompletedFocus = false,
            CompletedBreak = true,
            WasContinued = false,
            SessionDate = endedAt.Date
        });
    }

    private static DateTime GetSessionStartedAt()
    {
        string startedAtText = Preferences.Get(SessionStartedAtKey, "");

        if (DateTime.TryParse(startedAtText, out DateTime startedAt))
            return startedAt;

        return DateTime.Now;
    }

    private static void SaveSession(PomodoroSession session)
    {
        string dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "reminders.db");

        using var db = new SQLiteConnection(dbPath);

        db.CreateTable<PomodoroSession>();
        db.Insert(session);
    }
}