using Microsoft.Maui.ApplicationModel;

namespace RemindMe.Services;

public static class NotificationService
{
    public static async Task ScheduleNotification(int id, string title, string description, DateTime notifyTime)
    {
        if (notifyTime <= DateTime.Now)
            return;

#if ANDROID
        var status = await Permissions.RequestAsync<Permissions.PostNotifications>();

        if (status != PermissionStatus.Granted)
            return;

        AndroidNotificationService.ScheduleNotification(id, title, description, notifyTime);
#endif
    }

    public static Task CancelNotification(int id)
    {
#if ANDROID
        AndroidNotificationService.CancelNotification(id);
#endif
        return Task.CompletedTask;
    }

    public static async Task SchedulePomodoroNotification(DateTime notifyTime, bool isFocusMode)
    {
        if (notifyTime <= DateTime.Now)
            return;

#if ANDROID
        var status = await Permissions.RequestAsync<Permissions.PostNotifications>();

        if (status != PermissionStatus.Granted)
            return;

        AndroidNotificationService.SchedulePomodoroNotification(notifyTime, isFocusMode);
#endif
    }

    public static Task CancelPomodoroNotification()
    {
#if ANDROID
        AndroidNotificationService.CancelPomodoroNotification();
#endif
        return Task.CompletedTask;
    }
}