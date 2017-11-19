using System.Collections;
using UnityEngine;

public class BattleTutorialFlights : BattleTutorial
{
    [SerializeField] protected float goalAngleMoveLesson;

    private const float MIN_THROTTLE_LEVEL = 0.1f;

    public virtual string KeyThrottle { get { return "tutorialMessage_15"; } }
    public override string KeyMove { get { return "tutorialMessage_14"; } }

    public override IEnumerator Lessons()
    {
        yield return StartCoroutine(GreetingsCommander());
        yield return StartCoroutine(MoveLesson());
        yield return StartCoroutine(ThrottleLesson());
        yield return StartCoroutine(PickUpBonusLesson());
        yield return StartCoroutine(FireLesson());
        yield return StartCoroutine(KillEnemiesLesson());
        yield return StartCoroutine(ShowReminder());
    }

    protected override IEnumerator MoveLesson()
    {
        CurrentBattleLesson = BattleLessons.move;

        BattleGUI.Instance.ThrottleLevel.IsOn = false;

        yield return StartCoroutine(ShowTutorMessage(KeyMove, (int)VoiceEventKey.FlightMoveLessonJoystick));

        GUIControlSpriteGroups.SetActiveSpriteGroups(GUIControlSpriteGroups.MoveJoystickSprites, true);

        InitBlinkingRoutines(GUIControlSpriteGroups.MoveJoystickSprites, BlinkingMode.hard);

        JoystickManager.Instance.joysticks[(int)JoystickManager.Joystics.left].IsOn = true;

        yield return StartCoroutine(CheckIfMoveLessonIsDone());
        yield return StartCoroutine(HideTutorMessage());
    }

    protected IEnumerator ThrottleLesson()
    {
        CurrentBattleLesson = BattleLessons.throttle;
        BattleGUI.Instance.ThrottleLevel.IsOn = true;

        yield return StartCoroutine(ShowTutorMessage(KeyThrottle, (int)VoiceEventKey.ThrottleLessonTouch));

        GUIControlSpriteGroups.SetActiveSpriteGroups(GUIControlSpriteGroups.ThrottleLevelSprites, true);

        InitBlinkingRoutines(GUIControlSpriteGroups.ThrottleLevelSprites, BlinkingMode.hard);

        yield return StartCoroutine(CheckIfThrottleLessonIsDone());
        yield return StartCoroutine(HideTutorMessage());
    }

    public IEnumerator CheckIfThrottleLessonIsDone()
    {
        if (!BattleController.MyVehicle)
            yield break;

        Vector3 storedPos = BattleController.MyVehicle.transform.position;
        float odometer = 0;

        while (odometer < goalDistanceMoveLesson)
        {
            if (ThrottleLevel.Value > MIN_THROTTLE_LEVEL)
                odometer += Vector3.Distance(storedPos, BattleController.MyVehicle.transform.position);

            storedPos = BattleController.MyVehicle.transform.position;

            yield return null;
        }

        StopBlinkingRoutines();

        MiscTools.SetSpritesAlpha(GUIControlSpriteGroups.ThrottleLevelSprites, 1);

        Dispatcher.Send(EventId.BattleLessonAccomplished, new EventInfo_I((int)BattleLessons.upDown));
    }

    protected override void SetActiveControls(bool activate)
    {
        base.SetActiveControls(activate);
        GUIControlSpriteGroups.SetActiveSpriteGroups(GUIControlSpriteGroups.ThrottleLevelSprites, activate);
        BattleGUI.Instance.ThrottleLevel.IsOn = activate;
    }

    public override IEnumerator CheckIfMoveLessonIsDone()
    {
        if (!BattleController.MyVehicle)
            yield break;

        float accumulatedAngle = 0;
        Quaternion storedRotation = BattleController.MyVehicle.transform.rotation;

        while (accumulatedAngle < goalAngleMoveLesson)
        {
            accumulatedAngle += Quaternion.Angle(storedRotation, BattleController.MyVehicle.transform.rotation);

            storedRotation = BattleController.MyVehicle.transform.rotation;

            yield return null;
        }

        StopBlinkingRoutines();

        MiscTools.SetSpritesAlpha(GUIControlSpriteGroups.MoveJoystickSprites, 1);
        Dispatcher.Send(EventId.BattleLessonAccomplished, new EventInfo_I((int)BattleLessons.move));
    }
}
