using System.Collections;
using UnityEngine;

public class BattleTutorialTanks_PC : BattleTutorial_PC_Part
{
    private BattleTutorialTanks battleTutorialTanks;

    public string KeyMove { get { return "tutorialMessageKey_86"; } }
    public string KeyTurret { get { return "tutorialMessageKey_87"; } }
    public string KeyFire { get { return "tutorialMessageKey_9"; } }

    protected override void Start()
    {
        base.Start();

        battleTutorialTanks = (BattleTutorialTanks) battleTutorial;
    }


    public override IEnumerator Lessons()
    {
        yield return StartCoroutine(battleTutorialTanks.GreetingsCommander());
        yield return StartCoroutine(MoveLesson());
        yield return StartCoroutine(RotateTurretLesson());
        yield return StartCoroutine(battleTutorialTanks.PickUpBonusLesson());
        yield return StartCoroutine(FireLesson());
        yield return StartCoroutine(battleTutorialTanks.KillEnemiesLesson());
        yield return StartCoroutine(battleTutorialTanks.ShowReminder());
    }

    public override IEnumerator MoveLesson()
    {
        battleTutorial.CurrentBattleLesson = BattleTutorial.BattleLessons.move;

        yield return StartCoroutine(battleTutorial.ShowTutorMessage(battleTutorial.KeyMove, (int)VoiceEventKey.TankMoveLessonButtons));

        SetBtnsSprite(spr, "arrowBtns");
        SetBtnsSprite(sprAlt, "wasdBtns");

        JoystickManager.Instance.joysticks[(int)JoystickManager.Joystics.left].IsOn = true;

        yield return StartCoroutine(battleTutorial.CheckIfMoveLessonIsDone());
        yield return StartCoroutine(HideTutorMessage());
    }

    protected IEnumerator RotateTurretLesson()
    {
        battleTutorialTanks.CurrentBattleLesson = BattleTutorial.BattleLessons.turret;

        yield return StartCoroutine(battleTutorialTanks.ShowTutorMessage(KeyTurret, (int)VoiceEventKey.TurretLessonButtons));

        SetBtnsSprite(spr, "qeBtns");
        SetBtnsSprite(sprAlt, "zxBtns");

        yield return StartCoroutine(battleTutorialTanks.CheckIfTurretLessonIsDone());
        yield return StartCoroutine(HideTutorMessage());
    }

    protected IEnumerator FireLesson()
    {
        battleTutorialTanks.CurrentBattleLesson = BattleTutorial.BattleLessons.fire;

        yield return StartCoroutine(battleTutorialTanks.ShowTutorMessage(KeyFire, (int)VoiceEventKey.FireLessonButtons));

        SetBtnsSprite(sprAlt, "spaceBtn");

        BattleController.MyVehicle.PrimaryFireIsOn = true;
        GUIControlSpriteGroups.SetActiveSpriteGroups(GUIControlSpriteGroups.GunSightCircleSprites, true);

        while (BattleStatisticsManager.BattleStats["Shoots"] < battleTutorialTanks.GoalShoots)
            yield return null;

        Dispatcher.Send(EventId.BattleLessonAccomplished, new EventInfo_I((int)BattleTutorial.BattleLessons.fire));

        battleTutorialTanks.StopBlinkingRoutines();

        MiscTools.SetSpritesAlpha(GUIControlSpriteGroups.PrimaryFireBtnSprites, 1);

        yield return StartCoroutine(HideTutorMessage());
    }
}
