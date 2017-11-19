using System.Collections;

public class BattleTutorialFlights_PC : BattleTutorial_PC_Part
{
    private BattleTutorialFlights battleTutorialFlights;

    public string KeyThrottle { get { return "tutorialMessageKey_15"; } }
    public string KeyMove { get { return "tutorialMessageKey_14"; } }

    protected override void Start()
    {
        base.Start();
        battleTutorialFlights = (BattleTutorialFlights)battleTutorial;
    }

    public override IEnumerator Lessons()
    {
        yield return StartCoroutine(battleTutorialFlights.GreetingsCommander());
        yield return StartCoroutine(MoveLesson());
        yield return StartCoroutine(ThrottleLesson());
        yield return StartCoroutine(battleTutorialFlights.PickUpBonusLesson());
        yield return StartCoroutine(FireLesson());
        yield return StartCoroutine(battleTutorialFlights.KillEnemiesLesson());
        yield return StartCoroutine(battleTutorialFlights.ShowReminder());
    }

    public override IEnumerator MoveLesson()
    {
        battleTutorial.CurrentBattleLesson = BattleTutorial.BattleLessons.move;

        yield return StartCoroutine(battleTutorial.ShowTutorMessage(KeyMove, (int)VoiceEventKey.FlightMoveLessonButtons));

        SetBtnsSprite(spr, "arrowBtns");
        SetBtnsSprite(sprAlt, "AD_Btns");

        JoystickManager.Instance.joysticks[(int)JoystickManager.Joystics.left].IsOn = true;

        yield return StartCoroutine(battleTutorial.CheckIfMoveLessonIsDone());
        yield return StartCoroutine(HideTutorMessage());
    }

    private IEnumerator ThrottleLesson()
    {
        battleTutorialFlights.CurrentBattleLesson = BattleTutorial.BattleLessons.throttle;
        BattleGUI.Instance.ThrottleLevel.IsOn = true;

        yield return StartCoroutine(battleTutorialFlights.ShowTutorMessage(KeyThrottle, (int)VoiceEventKey.ThrottleLessonButtons));

        SetBtnsSprite(sprAlt, "WS_Btns");

        yield return StartCoroutine(battleTutorialFlights.CheckIfThrottleLessonIsDone());
        yield return StartCoroutine(HideTutorMessage());
    }

    private IEnumerator FireLesson()
    {
        battleTutorialFlights.CurrentBattleLesson = BattleTutorial.BattleLessons.fire;

        yield return StartCoroutine(battleTutorialFlights.ShowTutorMessage(KeyFire, (int)VoiceEventKey.FireLessonJoystick));

        SetBtnsSprite(sprAlt, "spaceBtn");

        GUIControlSpriteGroups.SetActiveSpriteGroups(GUIControlSpriteGroups.PrimaryFireBtnSprites, true);

        battleTutorialFlights.InitBlinkingRoutines(GUIControlSpriteGroups.PrimaryFireBtnSprites, BattleTutorial.BlinkingMode.hard);

        BattleController.MyVehicle.PrimaryFireIsOn = true;

        GUIControlSpriteGroups.SetActiveSpriteGroups(GUIControlSpriteGroups.GunSightCircleSprites, true);

        while (BattleStatisticsManager.BattleStats["Shoots"] < battleTutorialFlights.GoalShoots)
            yield return null;

        Dispatcher.Send(EventId.BattleLessonAccomplished, new EventInfo_I((int)BattleTutorial.BattleLessons.fire));

        battleTutorialFlights.StopBlinkingRoutines();

        MiscTools.SetSpritesAlpha(GUIControlSpriteGroups.PrimaryFireBtnSprites, 1);

        yield return StartCoroutine(HideTutorMessage());
    }
}
