using System.Collections;

public interface IPusher
{
    void RegisterForPushNotifications();
    void Initialize();
    IEnumerator SchedulingLocalNotifications();
    IEnumerator RemovingOldNotifications();
    event Pushwoosh.RegistrationSuccessHandler OnRegisteredForPushNotifications;
    string HWID { get; }
    string PushToken { get; }
}
