using System.Collections;
using UnityEngine;

public class IRCMButtonBOH : FireButtonBase
{
    [Header("Настройки для BOH")]

    [Header("Ссылки")]
    public tk2dTextMesh lblTimer;
    public tk2dBaseSprite sprIRCMIcon;
    public tk2dBaseSprite sprBg;
    public GameObject additionalElementsWrapper;

    [Header("Цвета")]
    public Color normalColor;
    public Color inactiveColor;
    public Color alertColor;
    public Color pushedColor;

    private const float BLINKING_FREQUENCY = 0.1f;

    private bool ircmLaunchRequired;

    private IEnumerator blinkingRoutine;

    private static Weapon Weapon
    {
        get { return BattleController.MyVehicle.GetWeapon(GunShellInfo.ShellType.IRCM); }
    }

    protected override void Awake()
    {
        base.Awake();

        Dispatcher.Subscribe(EventId.IRCMLaunchRequired, OnIRCMLaunchRequired);
        Dispatcher.Subscribe(EventId.WeaponReloaded, OnIRCMReloaded);
        Dispatcher.Subscribe(EventId.IRCMLaunched, OnIRCMLaunched);

        SetColor(inactiveColor);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        Dispatcher.Unsubscribe(EventId.IRCMLaunchRequired, OnIRCMLaunchRequired);
        Dispatcher.Unsubscribe(EventId.WeaponReloaded, OnIRCMReloaded);
        Dispatcher.Unsubscribe(EventId.IRCMLaunched, OnIRCMLaunched);
    }

    override protected void OnEnable()
    {
        base.OnEnable ();
        AdditionalWrapperSetActive(parentState: true, phase: false);
    }

    override protected void OnDisable ()
    {
        base.OnDestroy ();
        AdditionalWrapperSetActive(parentState: false, phase: false);
    }

    protected void Update()
    {
        SetTimerText();
    }

    protected override void FireButton_Down()
    {
        sprBg.color = pushedColor;
    }

    protected override void FireButton_Up()
    {
        sprBg.color = ircmLaunchRequired ? normalColor : inactiveColor;
    }

    private void OnIRCMLaunchRequired(EventId id, EventInfo ei)
    {
        ircmLaunchRequired = ((EventInfo_B)ei).bool1;

        SetColor(inactiveColor);
        AdditionalWrapperSetActive(parentState: true, phase: false);

        if (blinkingRoutine != null)
            CoroutineHelper.Stop(blinkingRoutine);

        if (!ircmLaunchRequired)
            return;

        blinkingRoutine = Blinking();

        CoroutineHelper.Start(blinkingRoutine);
    }

    private void OnIRCMReloaded(EventId id, EventInfo ei)
    {
        var eii = (EventInfo_I)ei;
        if ((GunShellInfo.ShellType)(eii.int1) != GunShellInfo.ShellType.IRCM)
            return;
        if (BattleController.MyVehicle == null || !Weapon.IsReady)
            return;

        SetColor(inactiveColor);
        AdditionalWrapperSetActive(parentState: true, phase: false);

        if (blinkingRoutine != null)
            CoroutineHelper.Stop(blinkingRoutine);

        if (!ircmLaunchRequired)
            return;

        blinkingRoutine = Blinking();

        CoroutineHelper.Start(blinkingRoutine);
    }

    private void OnIRCMLaunched(EventId id, EventInfo ei)
    {
        if (blinkingRoutine != null)
            CoroutineHelper.Stop(blinkingRoutine);

        SetColor(inactiveColor);

        AdditionalWrapperSetActive(parentState: true, phase: false);
    }

    private IEnumerator Blinking()
    {
        while (true)
        {
            SetColor(inactiveColor);
            AdditionalWrapperSetActive(parentState: true, phase: false);
            yield return new WaitForSeconds(BLINKING_FREQUENCY);

            SetColor(alertColor);
            AdditionalWrapperSetActive(parentState: true, phase: true);
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

    private void AdditionalWrapperSetActive(bool parentState, bool phase)
    {
        //DT.LogError("ircmLaunchRequired = {0}, parentState = {1}, phase = {2}", ircmLaunchRequired, parentState, phase);
        additionalElementsWrapper.SetActive(ircmLaunchRequired && parentState && phase);
    }

    private void SetColor(Color color)
    {
        sprIRCMIcon.color = color;
        sprBg.color = color;
    }
}
