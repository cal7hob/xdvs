using System.Collections;
using UnityEngine;

class BattleTutorialHelicopters_PC : BattleTutorialHelicopters
{
    [SerializeField] private StretchImageByText imageStretcher;
    [SerializeField] private float sprXPadding = 20;

    private float defaultAddToTextLength;

    [Header("PC control sprites holder")]
    [SerializeField] protected tk2dBaseSprite spr;

    public override string KeyMove { get { return "tutorialMessageKey_6"; } }
    public override string KeyUpDown { get { return "tutorialMessageKey_7"; } }
    public override string KeyFire { get { return "tutorialMessageKey_9"; } }
    public override string KeyUseDefense { get { return "tutorialMessageKey_10"; } }
    public override string KeyReminder { get { return "tutorialMessage_12"; } }

    void Start()
    {
        defaultAddToTextLength = imageStretcher.addToTextLength;
    }

    private void Align(string tutorMessage)
    {
        spr.transform.localPosition = Vector3.zero;
        var sprRenderer = spr.GetComponent<Renderer>();

        var stringSize = lblTutorMessage.GetEstimatedMeshBoundsForString(tutorMessage);
        spr.transform.localPosition += Vector3.right * (stringSize.size.x + sprRenderer.bounds.extents.x + sprXPadding);
        imageStretcher.addToTextLength += sprRenderer.bounds.extents.x + sprXPadding * 0.5f;
        imageStretcher.StretchImage(0, null);
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.A))
            imageStretcher.StretchImage(0, null);
    }

    private void SetBtnsSprite(string spriteName)
    {
        spr.gameObject.SetActive(true);
        spr.SetSprite(spriteName);
        Align(lblTutorMessage.text);
    }

    protected override IEnumerator MoveLesson()
    {
        CurrentBattleLesson = BattleLessons.move;

        yield return StartCoroutine(ShowTutorMessage(KeyMove, (int)VoiceEventKey.HelicopterMoveLessonButtons));

        SetBtnsSprite("arrowBtns");

        JoystickManager.Instance.joysticks[(int)JoystickManager.Joystics.left].IsOn = true;

        yield return StartCoroutine(CheckIfMoveLessonIsDone());
        yield return StartCoroutine(HideTutorMessage());
    }

    protected override IEnumerator FireLesson()
    {
        CurrentBattleLesson = BattleLessons.fire;

        yield return StartCoroutine(ShowTutorMessage(KeyFire, (int)VoiceEventKey.FireLessonButtons));

        SetBtnsSprite("spaceBtn");

        BattleController.MyVehicle.PrimaryFireIsOn = true;
        GUIControlSpriteGroups.SetActiveSpriteGroups(GUIControlSpriteGroups.GunSightCircleSprites, true);

        while (BattleStatisticsManager.BattleStats["Shoots"] < goalShoots)
            yield return null;

        Dispatcher.Send(EventId.BattleLessonAccomplished, new EventInfo_I((int)BattleLessons.fire));

        StopBlinkingRoutines();

        MiscTools.SetSpritesAlpha(GUIControlSpriteGroups.PrimaryFireBtnSprites, 1);

        yield return StartCoroutine(HideTutorMessage());
    }

    protected override IEnumerator UsingDefenceLesson()
    {
        CurrentBattleLesson = BattleLessons.useDefense; 

        yield return StartCoroutine(ShowTutorMessage(KeyUseDefense, (int)VoiceEventKey.UsingDefenceLessonButtons));

        SetBtnsSprite("enterBtn");

        BattleController.MyVehicle.SecondaryFireIsOn = true;
        Dispatcher.Send(EventId.IRCMLaunchRequired, new EventInfo_B(false));

        while (BattleStatisticsManager.BattleStats["Shoots_IRCM"] < goalIRCM)
            yield return null;

        Dispatcher.Send(EventId.BattleLessonAccomplished, new EventInfo_I((int)BattleLessons.useDefense));

        StopBlinkingRoutines();

        MiscTools.SetSpritesAlpha(GUIControlSpriteGroups.SecondaryFireBntSprites, 1);

        yield return StartCoroutine(HideTutorMessage());
    }

    protected override IEnumerator UpDownLesson()
    {
        CurrentBattleLesson = BattleLessons.upDown;   

        yield return StartCoroutine(ShowTutorMessage(KeyUpDown, (int)VoiceEventKey.HelicopterUpDownLessonButtons));

        SetBtnsSprite("wasdBtns");

        JoystickManager.Instance.joysticks[(int)JoystickManager.Joystics.right].IsOn = true;

        yield return StartCoroutine(CheckIfUpDownLessonIsDone());
        yield return StartCoroutine(HideTutorMessage());
    }

    public override IEnumerator HideTutorMessage()
    {
        SetAnimationDirection(AnimDirections.reversed);
        emersion.Play();

        yield return new WaitForSeconds(emersion.clip.length);

        spr.gameObject.SetActive(false);
        imageStretcher.addToTextLength = defaultAddToTextLength;
    }
}
