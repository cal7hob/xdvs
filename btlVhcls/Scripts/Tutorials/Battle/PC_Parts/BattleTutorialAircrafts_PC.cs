using System.Collections;

using UnityEngine;

public class BattleTutorialAircrafts_PC : BattleTutorial_PC_Part
{
    private BattleTutorialAircrafts battleTutorialAircrafts;

    public string KeyThrottle { get { return "tutorialMessageKey_15"; } }
    public string KeyMove { get { return "tutorialMessageKey_14"; } }

    protected override void Start()
    {
        base.Start();
        battleTutorialAircrafts = (BattleTutorialAircrafts)battleTutorial;
    }

    public override IEnumerator Lessons()
    {
        yield return StartCoroutine(battleTutorialAircrafts.GreetingsCommander());
        yield return StartCoroutine(MoveLesson());
        yield return StartCoroutine(ThrottleLesson());
        yield return StartCoroutine(battleTutorialAircrafts.PickUpBonusLesson());
        yield return StartCoroutine(FireLesson());
        yield return StartCoroutine(battleTutorialAircrafts.KillEnemiesLesson());
        yield return StartCoroutine(battleTutorialAircrafts.ShowReminder());
    }

    public override IEnumerator MoveLesson()
    {
        battleTutorial.CurrentBattleLesson = BattleTutorial.BattleLessons.move;

        yield return StartCoroutine(battleTutorial.ShowTutorMessage(KeyMove, (int)VoiceEventKey.FlightMoveLessonButtons));

        SetBtnsSprite(spr, "arrowBtns");

        JoystickManager.Instance.joysticks[(int)JoystickManager.Joystics.left].IsOn = true;

        yield return StartCoroutine(battleTutorial.CheckIfMoveLessonIsDone());
        yield return StartCoroutine(HideTutorMessage());
    }

    private IEnumerator ThrottleLesson()
    {
        battleTutorialAircrafts.CurrentBattleLesson = BattleTutorial.BattleLessons.throttle;
        BattleGUI.Instance.ThrottleLevel.IsOn = true;

        yield return StartCoroutine(battleTutorialAircrafts.ShowTutorMessage(KeyThrottle, (int)VoiceEventKey.ThrottleLessonButtons));

        GUIControlSpriteGroups.SetActiveSpriteGroups(GUIControlSpriteGroups.ThrottleLevelSprites, true);

        battleTutorial.InitBlinkingRoutines(GUIControlSpriteGroups.ThrottleLevelSprites, BattleTutorial.BlinkingMode.hard);

        SetBtnsSprite(sprAlt, "wsBtns");

        yield return StartCoroutine(battleTutorialAircrafts.CheckIfThrottleLessonIsDone());
        yield return StartCoroutine(HideTutorMessage());
    }

    private IEnumerator FireLesson()
    {
        battleTutorial.CurrentBattleLesson = BattleTutorial.BattleLessons.fire;

        yield return StartCoroutine(battleTutorial.ShowTutorMessage(KeyFire, (int)VoiceEventKey.AircraftMissileLessonButtons));

        SetBtnsSprite(sprAlt, "spaceBtn");

        BattleController.MyVehicle.data.attack = 0;

        Transform spawnPoint = battleTutorialAircrafts.GetFireLessonSpawnPoint();

        BattleController.MyVehicle.MakeRespawn(spawnPoint.position, spawnPoint.rotation, true, true);

        yield return null;

        GUIControlSpriteGroups.SetActiveSpriteGroups(GUIControlSpriteGroups.ThrottleLevelSprites, false);

        foreach (var joystick in JoystickManager.Instance.joysticks)
            joystick.IsOn = false;

        BattleGUI.Instance.ThrottleLevel.IsOn = false;

        GUIControlSpriteGroups.SetActiveSpriteGroups(GUIControlSpriteGroups.PrimaryFireBtnSprites, true);

        BattleController.MyVehicle.PrimaryFireIsOn = true;

        GUIControlSpriteGroups.SetActiveSpriteGroups(GUIControlSpriteGroups.GunSightCircleSprites, true);

        BotDispatcher.Instance.CreateBotForCurrentProject();

        AircraftController bot = null;
        AircraftController myAircraft = (AircraftController)BattleController.MyVehicle;

        while (bot == null)
        {
            bot = (AircraftController)battleTutorialAircrafts.GetFirstBot();
            yield return null;
        }

        BattleController.MyVehicle.data.rocketAttack = bot.MaxArmor * 2;

        bot.MakeRespawn(
            position:       BattleController.MyVehicle.transform.position + BattleController.MyVehicle.transform.forward * 200.0f,
            rotation:       Quaternion.LookRotation(BattleController.MyVehicle.transform.forward), 
            restoreLife:    true,
            firstTime:      true);

        bot.minSpeed = myAircraft.minSpeed * 1.07f;

        while (BattleStatisticsManager.BattleStats["Shoots_SACLOS"] < battleTutorialAircrafts.GoalShoots)
            yield return null;

        yield return new WaitForSeconds(3.0f);

        foreach (var joystick in JoystickManager.Instance.joysticks)
            joystick.IsOn = true;

        BattleGUI.Instance.ThrottleLevel.IsOn = true;

        BattleController.MyVehicle.MakeRespawn(false, true, true);

        if (bot.Armor > 0)
            bot.MakeRespawn(false, false, true);

        Dispatcher.Send(EventId.BattleLessonAccomplished, new EventInfo_I((int)BattleTutorial.BattleLessons.fire));

        yield return StartCoroutine(HideTutorMessage());
    }
}
