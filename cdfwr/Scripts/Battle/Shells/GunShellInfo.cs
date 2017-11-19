using System;


public enum ShellType
{
    Usual = 0,
    Missile_Med = 1,
    Missile_SACLOS = 2,
    IRCM = 3,
    Landmine = 4,
}

[Serializable]
public struct GunShellInfo
{
    public ShellType type;
    public string shellPrefabName;
    public float shellRadius; // Радиус снаряда пока берется из префаба снаряда. Поле на будущее.
    public string itemName;
    public bool continuousFire;
    public float speed; // Скорость пока берется из префаба снаряда. Поле на будущее.
    public bool isPrimary;

    public GunShellInfo(ShellType type, string itemName, string shellPrefabName, float shellRadius, float speed, bool continuousFire, bool isPrimary)
    {
        this.type = type;
        this.itemName = itemName;
        this.shellPrefabName = shellPrefabName;
        this.continuousFire = continuousFire;
        this.shellRadius = shellRadius;
        this.speed = speed;
        this.isPrimary = isPrimary;
    }

    public static GunShellInfo UsualShell
    {
        get { return GetShellInfoForType(ShellType.Usual); }
    }

    public static GunShellInfo GetShellInfoForType(ShellType type)
    {
        switch (type)
        {
            case ShellType.Missile_Med:
                return new GunShellInfo(
                    type: ShellType.Missile_Med,
                    itemName: "missile",
                    shellPrefabName: GameManager.PrefabNamePrefix + "Shell_Missile",
                    shellRadius: 0.4f,
                    speed: 100,
                    continuousFire: false,
                    isPrimary: false);

            case ShellType.Missile_SACLOS:
                return new GunShellInfo(
                    type: ShellType.Missile_Med,
                    itemName: "missile",
                    shellPrefabName: GameManager.PrefabNamePrefix + "Shell_SACLOSMissile",
                    shellRadius: 0.4f,
                    speed: 100,
                    continuousFire: false,
                    isPrimary: false);

            case ShellType.IRCM:
                return new GunShellInfo(
                    type: ShellType.IRCM,
                    itemName: null,
                    shellPrefabName: GameManager.PrefabNamePrefix + "Shell_IRCM",
                    shellRadius: 0.4f,
                    speed: 100,
                    continuousFire: false,
                    isPrimary: false);

            default:
                return new GunShellInfo(
                    type: ShellType.Usual,
                    itemName: null,
                    shellPrefabName: GameManager.PrefabNamePrefix + "Shell_1",
                    shellRadius: 0.2f,
                    speed: 350,
                    continuousFire: true,
                    isPrimary: true);
        }
    }

    public override string ToString()
    {
        return string.Format(
                "GunShellInfo: T = {0}, IN = {1}, ShPN = {2}, CF = {3}, ShR = {4},  S = {5}, iP = {6}",
                type,
                itemName,
                shellPrefabName,
                continuousFire,
                shellRadius,
                speed,
                isPrimary);
    }
}
