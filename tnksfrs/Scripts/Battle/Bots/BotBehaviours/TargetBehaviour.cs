using System.Collections;
using UnityEngine;

class TargetBehaviour : BotBehaviour
{
    public TargetBehaviour(BotAI BotAi) : base(BotAi)
    {
    }

    public override float FireDelay
    {
        get
        {
            return MiscTools.random.Next(75, 100) * 0.001f;
        }
    }

    public override void Moving()
    {
        BotAI.Move();
    }

    public override IEnumerator FindingPosition()
    {
        while (BotAI.ThisVehicle != null && BotAI.ThisVehicle.IsAvailable)
        {
            FindPositionToMove();

            findingPosDelay = MiscTools.random.Next(100, 200) * 0.1f;
            yield return new WaitForSeconds(findingPosDelay);
        }
        Target = null;
    }

    public override IEnumerator FindingTarget()
    {
        if (!GameData.IsTeamMode)
        {
            yield break;
        }

        while (BotAI.ThisVehicle != null && BotAI.ThisVehicle.IsAvailable)
        {
            Target = BotAI.FindWeakestEnemyVehicle();
            findingTargetDelay = MiscTools.random.Next(100, 200) * 0.1f;
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

    public override void OnThisVehicleTakesDamage(int victimId, int attackerId)
    {
        base.OnThisVehicleTakesDamage(victimId, attackerId);

        if (MiscTools.random.Next(0, 100) < BotSettings.targetBotRevengeChance_s && BotAI.CurrentState != BotAI.RevengeState)
        {
            Target = XD.StaticContainer.BattleController.Units[attackerId];
            BotAI.SetState(BotAI.RevengeState);
        }
    }
}
