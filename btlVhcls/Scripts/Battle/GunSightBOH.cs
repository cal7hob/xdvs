using UnityEngine;

public class GunSightBOH : MonoBehaviour
{
    public tk2dSlicedSprite sprMissileAimed;
    public Color missileAimedActiveColor;
    public Color missileAimedInactiveColor;

    private bool launchRequired;

    private static Weapon Weapon
    {
        get { return BattleController.MyVehicle.GetWeapon(GunShellInfo.ShellType.Missile_SACLOS); }
    }

    void Awake()
    {
        Dispatcher.Subscribe(EventId.SecondaryWeaponUsed, OnSecondaryWeaponUsed);
        Dispatcher.Subscribe(EventId.SACLOSLaunchRequired, OnSACLOSLaunchRequired);

        sprMissileAimed.color = missileAimedInactiveColor;
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.SecondaryWeaponUsed, OnSecondaryWeaponUsed);
        Dispatcher.Unsubscribe(EventId.SACLOSLaunchRequired, OnSACLOSLaunchRequired);
    }

    private void OnSACLOSLaunchRequired(EventId id, EventInfo ei)
    {
        launchRequired = ((EventInfo_B)ei).bool1;

        sprMissileAimed.color
            = launchRequired && Weapon.IsReady
                ? missileAimedActiveColor
                : missileAimedInactiveColor;
    }

    private void OnSecondaryWeaponUsed(EventId id, EventInfo ei)
    {
        EventInfo_IIIV info = (EventInfo_IIIV)ei;

        if (info.int1 != BattleController.MyPlayerId)
            return;

        if ((GunShellInfo.ShellType)info.int2 != GunShellInfo.ShellType.Missile_SACLOS)
            return;

        sprMissileAimed.color = missileAimedInactiveColor;
    }
}
