using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class BotBehaviour
{
    protected float fireDelay;
    
    public List<int> BroAttackers = new List<int>();
    public Dictionary<int, IEnumerator> BroForgivenessRoutines = new Dictionary<int, IEnumerator>();

    public BotAI BotAI { get; protected set; }

    public bool HumanTargetPreference { get; protected set; }

    public abstract float FindingTargetDelay { get; }

    public abstract float FindingPosDelay { get; }

    public abstract float FireDelay { get; }

    protected BotBehaviour(BotAI botAI)
    {
        BotAI = botAI;
        StartSettingHumanTargetPreference();
    }

    public virtual void Apply(){ }

    public virtual void OnBotDestroy() { }

    private IEnumerator SetTargetPreference()
    {
        while (true)
        {
            var rndVal = Random.Range(0, 100);
            HumanTargetPreference = rndVal < GameData.targetHumanChance;

            yield return new WaitForSeconds(BotSettings.setTargetPreferenceDelay_s);
        }  
    }

    public void OnBotTakesDamage(EventId id, EventInfo info)
    {
        var ei = info as EventInfo_U;

        var victimId = (int)ei[0];
        var attackerId = (int)ei[2];

        if (victimId != BotAI.VehicleData.playerId)
            return;

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
        var info = (EventInfo_I) ei;

        if (BotAI.CurrentState != BotAI.OneShotKillState && BotAI.CurrentState != BotAI.RevengeState)
        {
            return;
        }

        if (BotAI.Target != null && info.int1 == BotAI.Target.data.playerId)
        {
            BotAI.Target = null;
            BotAI.SetState(BotAI.NormalState);
        }
    }

    public virtual void OnVehicleKilled(EventId id, EventInfo info)
    {
        var ei = (EventInfo_II) info;
        var victimId = ei.int1;

        if (BotAI.CurrentState != BotAI.OneShotKillState && BotAI.CurrentState != BotAI.RevengeState)
        {
            return;
        }

        if (BotAI.Target != null && victimId == BotAI.Target.data.playerId)
        {
            BotAI.Target = null;
            BotAI.SetState(BotAI.NormalState);
        }
    }

    public void OnBonusDestroyed(EventId id, EventInfo info)
    {
        var ei = (EventInfo_II) info;
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
        {
            return;
        }

        BotAI.Target = attacker;
        BotAI.SetState(BotAI.RevengeState);
        BotAI.CalcPathToTarget();
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

        if (!BattleController.allVehicles.TryGetValue(attackerId, out vehicle))
        {
            return;
        }

        if (vehicle.data.country != BotAI.VehicleData.country)
        {
            return;
        }

        if (!BroAttackers.Contains(attackerId))
        {
            BroAttackers.Add(attackerId);
        }

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
        yield return new WaitForSeconds(BotSettings.broForgivenessTimeout_s);

        ForgiveBro(broId);
    }

    public abstract void FindTarget();

    public override string ToString()
    {
        return GetType().Name;
    }
}
