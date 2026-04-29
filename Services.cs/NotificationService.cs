using Microsoft.Maui.ApplicationModel;

namespace RemindMe.Services;

public static class NotificationService
{
    public static async Task ScheduleNotification(int id, string title, string description, DateTime notifyTime)
    {
        if (notifyTime <= DateTime.Now.AddMinutes(1))
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
}