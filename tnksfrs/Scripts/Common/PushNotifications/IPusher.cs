using System.Collections;

public interface IPusher
{
    void RegisterForPushNotifications();
    void Initialize();
    IEnumerator SchedulingLocalNotifications(NotificationParams notifParams);
    IEnumerator RemovingOldNotifications();
    event Pushwoosh.RegistrationSuccessHandler OnRegisteredForPushNotifications;
    event Pushwoosh.RegistrationErrorHandler OnFailedToRegisteredForPushNotifications;
    event Pushwoosh.NotificationHandler OnPushNotificationsReceived;
    string HWID { get; }
    string PushToken { get; }
}
