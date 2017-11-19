using System.Collections;

public interface IPusher
{
    void RegisterForPushNotifications();
    void Initialize();
    void SchedulingLocalNotifications();
    void RemovingOldNotifications();
    event Pushwoosh.RegistrationSuccessHandler OnRegisteredForPushNotifications;
    string HWID { get; }
    string PushToken { get; }
}
