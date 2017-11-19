using System.Collections;
using UnityEngine;

public class FireButtonBOH : FireButtonBase
{
    [Header("Настройки для BOH")]

    [Header("Ссылки")]
    public tk2dTextMesh lblTimer;
    public tk2dSprite sprMachineGunIcon;
    public tk2dSprite sprMissileIcon;
    public tk2dBaseSprite sprBg;

    [Header("Цвета")]
    public Color normalColor;
    public Color inactiveColor;
    public Color alertColor;

    private const float BLINKING_FREQUENCY = 0.1f;

    private bool launchRequired;
    private IEnumerator blinkingRoutine;

    private static Weapon Weapon
    {
        get { return BattleController.MyVehicle.GetWeapon(GunShellInfo.ShellType.Missile_SACLOS); }
    }

    protected override void Awake()
    {
        base.Awake();

        Dispatcher.Subscribe(EventId.SecondaryWeaponUsed, OnSecondaryWeaponUsed);
        Dispatcher.Subscribe(EventId.SACLOSLaunchRequired, OnSACLOSLaunchRequired);

        sprMachineGunIcon.gameObject.SetActive(true);
        sprMissileIcon.gameObject.SetActive(false);

        sprMachineGunIcon.color = sprBg.color = normalColor;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        Dispatcher.Unsubscribe(EventId.SecondaryWeaponUsed, OnSecondaryWeaponUsed);
        Dispatcher.Unsubscribe(EventId.SACLOSLaunchRequired, OnSACLOSLaunchRequired);
    }

    protected void Update()
    {
        SetTimerText();
    }

    protected override void FireButton_Down() { }

    protected override void FireButton_Up() { }

    private void OnSACLOSLaunchRequired(EventId id, EventInfo ei)
    {
        launchRequired = ((EventInfo_B)ei).bool1;

        if (blinkingRoutine != null)
            CoroutineHelper.Stop(blinkingRoutine);

        sprMachineGunIcon.gameObject.SetActive(true);
        sprMissileIcon.gameObject.SetActive(false);

        sprMachineGunIcon.color = normalColor;
        sprBg.color = normalColor;

        if (!launchRequired)
            return;

        blinkingRoutine = Blinking();

        CoroutineHelper.Start(blinkingRoutine);
    }

    private void OnSecondaryWeaponUsed(EventId id, EventInfo ei)
    {
        EventInfo_IIIV info = (EventInfo_IIIV)ei;

        if (info.int1 != BattleController.MyPlayerId)
            return;

        if ((GunShellInfo.ShellType)info.int2 != GunShellInfo.ShellType.Missile_SACLOS)
            return;

        if (blinkingRoutine != null)
            CoroutineHelper.Stop(blinkingRoutine);

        sprMachineGunIcon.gameObject.SetActive(true);
        sprMissileIcon.gameObject.SetActive(false);

        sprMachineGunIcon.color = normalColor;
        sprBg.color = normalColor;
    }

    private IEnumerator Blinking()
    {
        sprMissileIcon.gameObject.SetActive(true);
        sprMachineGunIcon.gameObject.SetActive(false);

        while (true)
        {
            sprBg.color = inactiveColor;
            sprMissileIcon.color = inactiveColor;
            yield return new WaitForSeconds(BLINKING_FREQUENCY);

            sprBg.color = alertColor;
            sprMissileIcon.color = alertColor;
            yield return new WaitForSeconds(BLINKING_FREQUENCY);
        }
    }

    private void SetTimerText()
    {
        if (BattleController.MyVehicle == null)
            return;

        if (Weapon.IsReady)
        {
            if (lblTimer.gameObject.activeSelf)
                lblTimer.gameObject.SetActive(false);

            return;
        }

        if (!lblTimer.gameObject.activeSelf)
            lblTimer.gameObject.SetActive(true);

        lblTimer.text = Mathf.RoundToInt(Weapon.ReloadRemainingSeconds).ToString();
    }
}
