using System.Collections;
using UnityEngine;

public class BattleTutorialTanks : BattleTutorial
{
    [SerializeField] protected float goalAngleTurretLesson = 35;
    [SerializeField] protected SwipeLessonAnimation swipeLessonAnimation;

    public override IEnumerator Lessons()
    {
        yield return StartCoroutine(GreetingsCommander());
        yield return StartCoroutine(MoveLesson());
        yield return StartCoroutine(RotateTurretLesson());
        yield return StartCoroutine(PickUpBonusLesson());
        yield return StartCoroutine(FireLesson());
        yield return StartCoroutine(KillEnemiesLesson());
        yield return StartCoroutine(ShowReminder());
    }

    protected virtual IEnumerator RotateTurretLesson()
    {
        CurrentBattleLesson = BattleLessons.turret;

        yield return StartCoroutine(ShowTutorMessage(KeyTurret, (int)VoiceEventKey.TurretLessonTouch));

        swipeLessonAnimation.Play();

        yield return StartCoroutine(CheckIfTurretLessonIsDone());

        swipeLessonAnimation.Stop();

        yield return StartCoroutine(HideTutorMessage());
    }

    public virtual IEnumerator CheckIfTurretLessonIsDone()
    {
        if (!BattleController.MyVehicle)
            yield break;

        var storedRotation = BattleController.MyVehicle.Turret.localRotation;

        while (Quaternion.Angle(BattleController.MyVehicle.Turret.localRotation, storedRotation) < goalAngleTurretLesson)
            yield return null;

        Dispatcher.Send(EventId.BattleLessonAccomplished, new EventInfo_I((int)BattleLessons.turret));
    }
}
