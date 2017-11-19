using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XD;

public abstract class BotBehaviour
{
    protected float                     findingTargetDelay = 0;
    protected float                     findingPosDelay = 0;
    protected float                     fireDelay = 0;
    protected Transform                 startPoint = null;

    public List<int>                    BroAttackers = new List<int>();
    public Dictionary<int, IEnumerator> broForgivenessRoutines = new Dictionary<int, IEnumerator>();
    
    protected Vector3 Epicenter
    {
        get
        {
            Vector3 res = new Vector3();           

            if (startPoint != null)
            {
                res = startPoint.position;

                if (Vector3.Distance(BotAI.ThisVehicle.Transform.position, startPoint.position) < 10)
                {
                    startPoint = null;
                }
            }
            else
            {
                res = BotAI.ThisVehicle.IsMainsFriend ? BotDispatcher.EnemiesEpicenter : BotDispatcher.FriendsEpicenter;
            }   
            
            return BotAI.FindRandomPointNearPosition(res, 20);           
        }
    }

    public BotAI BotAI
    {
        get; protected set;
    }

    public Vector3 PositionToMove
    {
        get; protected set;
    }

    public IEnumerator ShootingRoutine
    {
        get; protected set;
    }

    public IEnumerator FindingPositionRoutine
    {
        get; protected set;
    }

    public IEnumerator FindingTargetRoutine
    {
        get; protected set;
    }
    public IEnumerator PathUpdatingRoutine
    {
        get; protected set;
    }
    public IEnumerator UpdatingRoutine
    {
        get; protected set;
    }
    public IEnumerator SettingTargetPreference
    {
        get; protected set;
    }

    public VehicleController Target
    {
        get; set;
    }

    public bool HumanTargetPreference
    {
        get; protected set;
    }

    public abstract float FireDelay
    {
        get;
    }

    public float MainWeaponReloadTime
    {
        get; protected set;
    }

    protected BotBehaviour(BotAI botAI)
    {
        BotAI = botAI;
        MainWeaponReloadTime = BotAI.ThisVehicle.GetWeapon(GunShellInfo.ShellType.Usual).ReloadingTimeSeconds;

        if (BotAI.ThisVehicle != null)
        {
            GameObject[] points = GameObject.FindGameObjectsWithTag("BotPoint" + BotAI.ThisVehicle.Team);
            if (points != null && points.Length > 0)
            {
                startPoint = points.GetRandomItem().transform;
            }
        }
    }

    public virtual void Draw()
    {
        if (Target != null)
        {
#if UNITY_EDITOR
            UnityEditor.Handles.Label(Target.Transform.position, string.Format("Target {0}", Target.Name).FormatString(Color.magenta), GUIStyle.none);
            UnityEditor.Handles.Label(BotAI.ThisVehicle.Transform.position + Vector3.up * 2, string.Format("[{0}], {1}", BotAI.ThisVehicle.Team, BotAI.ThisVehicle.Name).FormatString(BotAI.ThisVehicle.Team == StaticContainer.GameManager.Team ? Color.green : Color.red), GUIStyle.none);
#endif

            Debug.DrawLine(Target.Transform.position, BotAI.ThisVehicle.Transform.position, Color.magenta);
        }
    }

    protected void FindPositionToMove()
    {
        if (BotAI.CurrentState != BotAI.TakingBonusState)
        {
            Vector3 pos;

            if (Target != null)
            {
                pos = BotAI.FindRandomPointNearPosition(Target.transform.position);
            }
            else if (GameData.IsTeamMode)
            {
                pos = Epicenter;                    
            }
            else
            {
                pos = BotAI.FindRandomPointNearPosition(BotAI.ThisVehicle.transform.position, radius: 40);
            }

            SetPositionToGo(pos);
        }
    }

    public void SetPositionToGo(Vector3 pos, bool findPath = true)
    {
        PositionToMove = pos;

        if (findPath)
            BotAI.FindPath();
    }

    private IEnumerator SetTargetPreference()
    {
        while (BotAI.ThisVehicle != null)
        {
            var rndVal = Random.Range(0, 100);
            HumanTargetPreference = rndVal < GameData.targetHumanChance;

            yield return new WaitForSeconds(BotSettings.setTargetPreferenceDelay_s);
        }
    }

    public void OnVehicleTakesDamage(EventId id, EventInfo info)
    {
        var ei = info as EventInfo_U;

        var victimId = (int)ei[0];
        var attackerId = (int)ei[2];

        if (victimId != BotAI.VehicleData.playerId)
        {
            return;
        }

        OnThisVehicleTakesDamage(victimId, attackerId);
    }

    public void StartSettingHumanTargetPreference()
    {
        if (!GameData.IsTeamMode)
        {
            SettingTargetPreference = BotAI.RestartRoutine(SettingTargetPreference, BotAI.CurrentBehaviour.SetTargetPreference());
        }
    }

    public void StartFindingTarget()
    {
        FindingTargetRoutine = BotAI.RestartRoutine(FindingTargetRoutine, BotAI.CurrentBehaviour.FindingTarget());
    }

    public void StartFindingPosition()
    {
        FindingPositionRoutine = BotAI.RestartRoutine(FindingPositionRoutine, BotAI.CurrentBehaviour.FindingPosition());
    }

    public void StartPathUpdating()
    {
        PathUpdatingRoutine = BotAI.RestartRoutine(PathUpdatingRoutine, BotAI.PathUpdating());
    }

    public void StartUpdating()
    {
        UpdatingRoutine = BotAI.RestartRoutine(UpdatingRoutine, BotAI.CurrentState.Updating());
    }

    public virtual void OnVehicleLeftTheGame(EventId id, EventInfo ei)
    {
        var info = (EventInfo_I)ei;

        if (Target != null && info.int1 == Target.data.playerId)
        {
            Target = null;
            BotAI.SetState(BotAI.NormalState);
        }
    }

    public virtual void OnVehicleKilled(EventId id, EventInfo info)
    {
        var ei = (EventInfo_II)info;

        if (Target != null && ei.int1 == Target.data.playerId)
        {
            Target = null;
            BotAI.SetState(BotAI.NormalState);
        }
    }

    public void OnBonusDestroyed(EventId id, EventInfo info)
    {
        var ei = (EventInfo_II)info;

        if (BotAI.ClosestBonus != null && ei.int2 == BotAI.ClosestBonus.Id && BotAI.CurrentState == BotAI.TakingBonusState)
        {
            BotAI.SetState(BotAI.NormalState);
        }
    }

    public void StartShooting()
    {
        if (!PhotonNetwork.offlineMode)// бот не стреляет в туторе
        {
            ShootingRoutine = BotAI.RestartRoutine(ShootingRoutine, Shooting());
        }
    }

    public void OnCritHit(VehicleController attacker)
    {
        if (BotAI == null)
        {
            return;
        }

        Target = attacker;

        BotAI.SetState(BotAI.RevengeState);
    }

    public bool CheckIfBro(VehicleController potentialTarget)
    {
        if (GameData.IsTeamMode && potentialTarget.IsMainsFriend)
        {
            return true;
        }

        return !GameData.IsTeamMode && potentialTarget.data.country == BotAI.VehicleData.country && !BroAttackers.Contains(potentialTarget.data.playerId);
    }

    public virtual void OnThisVehicleTakesDamage(int victimId, int attackerId)
    {
        VehicleController vehicle;

        if (!XD.StaticContainer.BattleController.Units.TryGetValue(attackerId, out vehicle))
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

        if (!broForgivenessRoutines.ContainsKey(attackerId))
        {
            broForgivenessRoutines.Add(attackerId, BroForgivenessRoutine(attackerId));
            BattleController.Instance.StartCoroutine(broForgivenessRoutines[attackerId]);
        }
        else
        {
            BattleController.Instance.StopCoroutine(broForgivenessRoutines[attackerId]);
            broForgivenessRoutines[attackerId] = BroForgivenessRoutine(attackerId);
            BattleController.Instance.StartCoroutine(broForgivenessRoutines[attackerId]);
        }
    }

    public void ForgiveBro(int broId)
    {
        BroAttackers.Remove(broId);
        broForgivenessRoutines.Remove(broId);

        if (XD.StaticContainer.BattleController.Units.ContainsKey(broId) && Target == XD.StaticContainer.BattleController.Units[broId])
        {
            Target = null;
        }
    }

    public IEnumerator BroForgivenessRoutine(int broId)
    {
        yield return new WaitForSeconds(BotSettings.broForgivenessTimeout_s);

        ForgiveBro(broId);
    }

    public abstract IEnumerator FindingPosition();
    public abstract IEnumerator FindingTarget();
    public abstract IEnumerator Shooting();
    public abstract void Moving();
}
