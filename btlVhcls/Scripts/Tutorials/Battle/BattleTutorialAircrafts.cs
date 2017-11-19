using System.Collections;
using UnityEngine;

public class BattleTutorialAircrafts : BattleTutorialFlights
{
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

    public override IEnumerator KillEnemiesLesson()
    {
        for (int i = 0; i < GoalBotKills - 1; i++) // Костыль (GoalBotKills - 1) потому, что ещё один бот появляется ранее.
            BotDispatcher.Instance.CreateBotForCurrentProject();

        enemyFindingRoutine = GetClosestEnemy();

        StartCoroutine(enemyFindingRoutine);

        CurrentBattleLesson = BattleLessons.killEnemies;

        yield return StartCoroutine(ShowTutorMessage(KeyKillEnemies, (int)VoiceEventKey.KillEnemiesLesson));

        GUIControlSpriteGroups.SetActiveSpriteGroups(GUIControlSpriteGroups.PrimaryFireBtnSprites, true);
        GUIControlSpriteGroups.SetActiveSpriteGroups(GUIControlSpriteGroups.GunSightCircleSprites, true);

        BattleController.MyVehicle.PrimaryFireIsOn = true;

        SetUpArrow();

        while (!allBotsDead)
        {
            while (closestEnemy == null || closestEnemy.gameObject == null)
            {
                if (arrow.gameObject.activeSelf)
                    arrow.gameObject.SetActive(false);

                yield return new WaitForFixedUpdate();
            }

            if (!arrow.gameObject.activeSelf)
                arrow.gameObject.SetActive(true);

            TurnArrow(closestEnemy.transform);

            yield return new WaitForFixedUpdate();
        }

        arrow.gameObject.SetActive(false);

        StopCoroutine(enemyFindingRoutine);

        Dispatcher.Send(EventId.BattleLessonAccomplished, new EventInfo_I((int)BattleLessons.killEnemies));

        yield return StartCoroutine(HideTutorMessage());
    }

    /// <summary>
    /// Всё в костылях – осторожно!
    /// </summary>
    /// <returns></returns>
    protected override IEnumerator FireLesson()
    {
        CurrentBattleLesson = BattleLessons.fire;

        yield return StartCoroutine(ShowTutorMessage(KeyFire, (int)VoiceEventKey.AircraftMissileLessonTouch));

        BattleController.MyVehicle.data.attack = 0;

        Transform spawnPoint = GetFireLessonSpawnPoint();

        BattleController.MyVehicle.MakeRespawn(spawnPoint.position, spawnPoint.rotation, true, true);

        yield return null;

        GUIControlSpriteGroups.SetActiveSpriteGroups(GUIControlSpriteGroups.ThrottleLevelSprites, false);
        GUIControlSpriteGroups.SetActiveSpriteGroups(GUIControlSpriteGroups.MoveJoystickSprites, false);

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
            bot = (AircraftController)GetFirstBot();
            yield return null;
        }

        BattleController.MyVehicle.data.rocketAttack = bot.MaxArmor * 2;

        bot.MakeRespawn(
            position:       BattleController.MyVehicle.transform.position + BattleController.MyVehicle.transform.forward * 200.0f,
            rotation:       Quaternion.LookRotation(BattleController.MyVehicle.transform.forward), 
            restoreLife:    true,
            firstTime:      true);

        bot.minSpeed = myAircraft.minSpeed * 1.07f;

        while (BattleStatisticsManager.BattleStats["Shoots_SACLOS"] < goalShoots)
            yield return null;

        yield return new WaitForSeconds(3.0f);

        GUIControlSpriteGroups.SetActiveSpriteGroups(GUIControlSpriteGroups.ThrottleLevelSprites, true);
        GUIControlSpriteGroups.SetActiveSpriteGroups(GUIControlSpriteGroups.MoveJoystickSprites, true);

        foreach (var joystick in JoystickManager.Instance.joysticks)
            joystick.IsOn = true;

        BattleGUI.Instance.ThrottleLevel.IsOn = true;

        BattleController.MyVehicle.MakeRespawn(false, true, true);

        if (bot.Armor > 0)
            bot.MakeRespawn(false, false, true);

        Dispatcher.Send(EventId.BattleLessonAccomplished, new EventInfo_I((int)BattleLessons.fire));

        yield return StartCoroutine(HideTutorMessage());
    }

    public VehicleController GetFirstBot()
    {
        foreach (var vehicle in BattleController.allVehicles)
        {
            if (vehicle.Value.IsBot)
                return vehicle.Value;
        }

        return null;
    }

    public Transform GetFireLessonSpawnPoint()
    {
        GameObject spawnPointParent = GameObject.Find("SpawnPoints_Tutorial");
        return spawnPointParent.transform.GetChild(MiscTools.random.Next(0, spawnPointParent.transform.childCount - 1));
    }
}
