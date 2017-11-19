using System.Collections;
using System.Linq;
using UnityEngine;

class FighterBehaviour : BotBehaviour
{
    public FighterBehaviour(BotAI BotAi) : base(BotAi)
    {
    }

    public override float FireDelay { get { return MiscTools.random.Next(50, 100) * 0.001f; } }

    public override IEnumerator FindingPosition()
    {
        while (BotAI.ThisVehicle != null && BotAI.ThisVehicle.IsAvailable)
        {
            FindPositionToMove();

            findingPosDelay = MiscTools.random.Next(50, 120) * 0.1f;
            yield return new WaitForSeconds(findingPosDelay);
        }
    }

    public override IEnumerator FindingTarget()
    {
        while (BotAI.ThisVehicle != null && BotAI.ThisVehicle.IsAvailable)
        {
            Target = BotAI.FindWeakestEnemyVehicle();

            findingTargetDelay = MiscTools.random.Next(50, 100) * 0.1f;

            yield return new WaitForSeconds(findingTargetDelay);
        }
        Target = null;
    }

    public override IEnumerator Shooting()
    {
        while (BotAI.ThisVehicle != null && BotAI.ThisVehicle.IsAvailable)
        {
            if (Target != null && BotAI.TargetAimed && Target.IsAvailable && BotAI.WeaponReloadingProgress >= 1)
            {
                Vector3 startPoint = BotAI.ThisVehicle.Transform.position + Vector3.up;
                Vector3 targetPoint = Target.Transform.position + Vector3.up;

                if (!Physics.Linecast(startPoint, targetPoint, BotAI.ThisVehicle.CheckObstacleMask))
                {
                    yield return BotAI.ThisVehicle.StartCoroutine(BotAI.Fire());
                    yield return new WaitForSeconds(MainWeaponReloadTime);
                }
            }

            yield return null;
        }
    }

    public override void Moving()
    {
        if (BotAI.TargetAimed && Target == BotAI.AimPoint.Target)
        {
            BotAI.SetState(BotAI.StopState, true);
        }
        else
        {
            BotAI.Move();
        }
    }
}
