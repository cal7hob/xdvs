using UnityEngine;
using System.Collections;

public class BattleTutorialHelicopters : BattleTutorial
{
    protected HelicopterController helicopterController;

    [SerializeField] protected int goalIRCM = 1;
    [SerializeField] protected float goalDistanceUpDownLesson;

    public virtual string KeyUseDefense { get { return "tutorialMessage_10"; } }
    public override string KeyMove { get { return "tutorialMessage_6"; } }
    public override string KeyUpDown { get { return "tutorialMessage_7"; } }
    public override string KeyFire { get { return "tutorialMessage_9"; } }
    public override string KeyReminder { get { return "tutorialMessage_12"; } }
    public override VoiceEventKey KeyVoiceReminder { get { return VoiceEventKey.HelicopterHealthbarLesson; } }

    protected override void SetActiveControls(bool activate)
    {
        base.SetActiveControls(activate); 
        BattleController.MyVehicle.SecondaryFireIsOn = activate;
    }

    protected override void Init(EventId id, EventInfo info)
    {
        base.Init(id, info);
        helicopterController = BattleController.MyVehicle.gameObject.GetComponent<HelicopterController>();
    }

    protected override void SetUpArrow()
    {
        arrow.parent = FlightCameraController.FlightCamInstance.gameObject.GetComponentInChildren<Camera>().transform;

        arrow.localPosition = ArrowPosition;
        arrow.localRotation = Quaternion.LookRotation(Vector3.forward);
    }

    public override IEnumerator Lessons()
    {
        yield return StartCoroutine(GreetingsCommander());
        yield return StartCoroutine(MoveLesson());
        yield return StartCoroutine(UpDownLesson());
        yield return StartCoroutine(PickUpBonusLesson());
        yield return StartCoroutine(FireLesson());
        yield return StartCoroutine(UsingDefenceLesson());
        yield return StartCoroutine(KillEnemiesLesson());
        yield return StartCoroutine(ShowReminder());
    }

    protected virtual IEnumerator UpDownLesson()
    {
        CurrentBattleLesson = BattleLessons.upDown;

        yield return StartCoroutine(ShowTutorMessage(KeyUpDown, (int)VoiceEventKey.HelicopterUpDownLessonJoystick));

        GUIControlSpriteGroups.SetActiveSpriteGroups(
                    ProfileInfo.isSliderControl
                        ? GUIControlSpriteGroups.ThrottleLevelSprites
                        : GUIControlSpriteGroups.UpDownJoystickSprites, true);

        InitBlinkingRoutines(
            sprites:    ProfileInfo.isSliderControl
                            ? GUIControlSpriteGroups.ThrottleLevelSprites
                            : GUIControlSpriteGroups.UpDownJoystickSprites,
            mode:       BlinkingMode.hard);

        JoystickManager.Instance.joysticks[(int)JoystickManager.Joystics.right].IsOn = true;

        yield return StartCoroutine(CheckIfUpDownLessonIsDone());
        yield return StartCoroutine(HideTutorMessage());
    }

    protected virtual IEnumerator CheckIfUpDownLessonIsDone()
    {
        if(!BattleController.MyVehicle)
            yield break;

        float storedPosY = BattleController.MyVehicle.transform.position.y;
        float odometerY = 0;

        while (odometerY < goalDistanceUpDownLesson)
        {
            odometerY += Mathf.Abs(BattleController.MyVehicle.transform.position.y - storedPosY);
            storedPosY = BattleController.MyVehicle.transform.position.y;

            yield return null;
        }

        StopBlinkingRoutines();

        MiscTools.SetSpritesAlpha(ProfileInfo.isSliderControl ?
            GUIControlSpriteGroups.ThrottleLevelSprites :
            GUIControlSpriteGroups.UpDownJoystickSprites, 1);

        Dispatcher.Send(EventId.BattleLessonAccomplished, new EventInfo_I((int)BattleLessons.upDown));
    }

    protected virtual IEnumerator UsingDefenceLesson()
    {
        CurrentBattleLesson = BattleLessons.useDefense;

        yield return StartCoroutine(ShowTutorMessage(KeyUseDefense, (int)VoiceEventKey.UsingDefenceLessonJoystick));

        GUIControlSpriteGroups.SetActiveSpriteGroups(GUIControlSpriteGroups.SecondaryFireBntSprites, true);

        InitBlinkingRoutines(GUIControlSpriteGroups.SecondaryFireBntSprites, BlinkingMode.hard);

        BattleController.MyVehicle.SecondaryFireIsOn = true;

        Dispatcher.Send(EventId.IRCMLaunchRequired, new EventInfo_B(false));

        while (BattleStatisticsManager.BattleStats["Shoots_IRCM"] < goalIRCM)
            yield return null;

        Dispatcher.Send(EventId.BattleLessonAccomplished, new EventInfo_I((int)BattleLessons.useDefense));

        StopBlinkingRoutines();

        MiscTools.SetSpritesAlpha(GUIControlSpriteGroups.SecondaryFireBntSprites, 1);

        yield return StartCoroutine(HideTutorMessage());
    }
}
