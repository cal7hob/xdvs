using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class BotBehaviour
{
    protected VehicleController myVehicle;
    protected float fireDelay;
    
    public List<int> BroAttackers = new List<int>();
    public Dictionary<int, IEnumerator> BroForgivenessRoutines = new Dictionary<int, IEnumerator>();

    public BotAI BotAI { get; protected set; }

    public BotSettings BotSettings { get; protected set; }

    public bool HumanTargetPreference { get; protected set; }

    public float FindingTargetDelay { get { return BotSettings.FindingTargetDelays.RandWithinRange; } }

    public float FindingPosDelay { get { return BotSettings.FindingPosDelays.RandWithinRange; } }

    public float FireDelay { get { return BotSettings.FireDelay.RandWithinRange; } }

    public float MainWeaponReloadTime { get; protected set; }

    public bool StopRotateTurret { get; protected set; }

    public abstract void FindTarget();

    protected BotBehaviour(BotAI botAI)
    {
        BotAI = botAI;
        MainWeaponReloadTime = BotAI.ThisVehicle.GetWeapon(ShellType.Usual).ReloadingTimeSeconds;
        myVehicle = BattleController.MyVehicle;
    }

    private IEnumerator SetTargetPreference()
    {
        while (true)
        {
            var rndVal = MiscTools.random.Next(0, 100);
            HumanTargetPreference = rndVal < GameData.targetHumanChance;

            yield return new WaitForSeconds(BotSettings.TargetPreferenceDelays.RandWithinRange);
        }  
    }

    public void OnBotTakesDamage(EventId id, EventInfo info)
    {
        
        var ei = info as EventInfo_U;

        var victimId = (int)ei[0];
        var attackerId = (int)ei[2];

        if (victimId != BotAI.VehicleData.playerId)
        {
            return;
        }

        OnDamage(attackerId);
    }

    public void StartSettingHumanTargetPreference()
    {
        if(!GameData.IsTeamMode)
        {
            BotAI.ThisVehicle.StartCoroutine(SetTargetPreference());
        }
    }

    public virtual void OnVehicleLeftTheGame(EventId id, EventInfo ei)
    {
        var info = ei as EventInfo_I;

        if (BotAI.Target != null && info.int1 == BotAI.Target.data.playerId)
        {
            BotAI.Target = null;
            BotAI.SetState(BotAI.NormalState);
        }
    }

    public virtual void OnBotRespawned(EventId id, EventInfo ei)
    {
        var botPlayerId = (ei as EventInfo_I).int1;

        if (botPlayerId == BotAI.VehicleData.playerId)
        {
            BotAI.SetState(BotAI.NormalState);
        }
    }

    public virtual void OnVehicleKilled(EventId id, EventInfo info)
    {
        var ei = info as EventInfo_II;
        var victimId = ei.int1;

        if (BotAI.Target != null && victimId == BotAI.Target.data.playerId)
        {
            BotAI.Target = null;
            BotAI.SetState(BotAI.NormalState);
        }

        if (victimId == BotAI.ThisVehicle.data.playerId)
        {
            BotAI.SetState(BotAI.DeadState, true);
        }
    }

    public void OnBonusDestroyed(EventId id, EventInfo info)
    {
        var ei = info as EventInfo_II;
        var bonusId = ei.int2;

        if (BotAI.CurrentState == BotAI.TakingBonusState && BotAI.ClosestBonus != null && bonusId == BotAI.ClosestBonus.Id)
        {
            BotAI.SetState(BotAI.NormalState);
        }

        foreach (var bonusItem in BotAI.inaccessibleBonuses.Where(bonusItem => bonusItem.Id == bonusId))
        {
            BotAI.inaccessibleBonuses.Remove(bonusItem);
            break;
        }
    }

    public void OnCritHit(VehicleController attacker)
    {
        if (BotAI == null)
            return;
        
        BotAI.Target = attacker;
        BotAI.SetState(BotAI.RevengeState);
        BotAI.GetDirectionsToTarget();
    }

    public bool CheckIfBro(VehicleController potentialTarget)
    {
        return !GameData.IsTeamMode && potentialTarget.data.country == BotAI.VehicleData.country && !BroAttackers.Contains(potentialTarget.data.playerId);
    }

    public virtual void OnDamage(int attackerId)
    {
       CheckAttacker(attackerId);
    }

    protected void CheckAttacker(int attackerId)
    {
        VehicleController vehicle;

        if (!BattleController.allVehicles.TryGetValue(attackerId, out vehicle) || vehicle.data.country != BotAI.VehicleData.country)
            return;

        if (!BroAttackers.Contains(attackerId))
            BroAttackers.Add(attackerId);

        if (!BroForgivenessRoutines.ContainsKey(attackerId))
        {
            BroForgivenessRoutines.Add(attackerId, BroForgivenessRoutine(attackerId));
            BattleController.Instance.StartCoroutine(BroForgivenessRoutines[attackerId]);
        }
        else
        {
            BattleController.Instance.StopCoroutine(BroForgivenessRoutines[attackerId]);
            BroForgivenessRoutines[attackerId] = BroForgivenessRoutine(attackerId);
            BattleController.Instance.StartCoroutine(BroForgivenessRoutines[attackerId]);
        }
    }

    public void ForgiveBro(int broId)
    {
        BroAttackers.Remove(broId);
        BroForgivenessRoutines.Remove(broId);

        if (BattleController.allVehicles.ContainsKey(broId) && BotAI.Target == BattleController.allVehicles[broId])
        {
            BotAI.Target = null;
        }
    }

    public IEnumerator BroForgivenessRoutine(int broId)
    {
        yield return new WaitForSeconds(BotSettings.BroForgivenessTimeout.RandWithinRange);

        ForgiveBro(broId);
    }

    public virtual IEnumerator Shoot()
    {
        StopRotateTurret = true;
        yield return new WaitForSeconds(FireDelay);
        StopRotateTurret = false;
        yield return BotAI.ThisVehicle.StartCoroutine(BotAI.Fire());
    }
}
