using System;
using System.Collections;
using System.Collections.Generic;
using BattleDroneEnemySearching;
using Facebook.Unity;
using Pool;
using StateMachines;
using UnityEngine;
using DemetriTools.Optimizations;

public class BattleDrone : BattleItem, IStateMachineControlled
{
    private float sqrWatchRadius;
    private Timer lifeTimer;
    [SerializeField] private AnimationCurve moveCurve;

    public int Damage { get; private set; }
    public SearchStrategy SearchStrategy { get; set; }
    public VehicleController Target { get { return target; } }

    [SerializeField] private float reloadTime = 0.5f;
    [SerializeField] private float rotationSpeed = 220f;
    [SerializeField] private FXInfo shell;
    [SerializeField] private FXInfo shotFX;
    [SerializeField] private FXInfo explosionFX;
    [SerializeField] private AudioClip launchSound;
    [SerializeField] private AudioClip shotSound;
    [SerializeField] private AudioClip explosionSound;
    [SerializeField] private Transform[] barrels;

    [SerializeField] private float fallingAcceleration = 10f;
    public float FallingAcceleration { get { return fallingAcceleration; } }

    [SerializeField] private AudioClip disablingSound;
    public AudioClip DisablingSound { get { return disablingSound; } }

    private StateMachine<BattleDroneState> stateMachine;
    private VehicleController target;
    private bool isOnline = true;

    #region IStateMachineControlled

    public void SetState(int stateId)
    {
        stateMachine.SetState(stateId);
    }

    public Dictionary<int, IState> GetStateCache()
    {
        return new Dictionary<int, IState>
        {
            { BattleDroneState.APPEARING_ID, new BattleDrone_Appearing(this)},
            { BattleDroneState.SEARCHING_ID, new BattleDrone_Searching(this, 0.3f)},
            { BattleDroneState.ATTACK_ID, new BattleDrone_Attack(this, reloadTime)},
            { BattleDroneState.WAITING_TARGET_ID, new BattleDrone_WaitTarget(this, 1.5f)},
            { BattleDroneState.CLONE_REGULAR_ID, new BattleDrone_CloneRegular(this, reloadTime)},
            { BattleDroneState.OFFLINE_ID, new BattleDrone_Offline(this)},
        };
    }
    
    #endregion

    public void SetTarget(VehicleController target)
    {
        if (target != this.target)
        {
            photonView.RPC("SetTarget_RPC", PhotonTargets.All, target != null ? target.data.playerId : -1);
        }
    }

    [PunRPC]
    private void SetTarget_RPC(int targetId)
    {
        BattleController.allVehicles.TryGetValue(targetId, out target);
    }

    protected override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);
        isOnline = true;
        target = null;

        if (barrels == null || barrels.Length == 0)
        {
            barrels = new[] {transform};
        }

        Damage = (int)consumableInfo.powerValue;
        lifeTimer = new Timer(consumableInfo.duration);
        sqrWatchRadius = consumableInfo.radius * consumableInfo.radius;
        SearchStrategy = new NearestEnemy(this);
        stateMachine = new StateMachine<BattleDroneState>(this);
        SetState(BattleDroneState.APPEARING_ID);
    }

    void Update()
    {
        if (owner.PhotonView.isMine)
        {
            CheckLifeTime();
        }
        stateMachine.Update();
    }

    private void CheckLifeTime()
    {
        if (isOnline && lifeTimer.TimeElapsed)
        {
            SetState(BattleDroneState.OFFLINE_ID);
        }
    }

    public void SwitchOff()
    {
        isOnline = false;
    }

    public IEnumerator Appearing()
    {
        AudioDispatcher.PlayClipAtPosition(launchSound, transform.position, transform);
        yield return StartCoroutine(SmoothMove(transform.position, CalculateAppearingPos(), 2.2f, 0.5f));
        StartCoroutine(SmoothMove(transform.position, transform.position + Vector3.down * 0.21f, 1.5f));

        if (owner.PhotonView.isMine)
        {
            SetState(BattleDroneState.SEARCHING_ID);
        }
        else
        {
            SetState(BattleDroneState.CLONE_REGULAR_ID);
        }
    }

    public IEnumerator SmoothMove(Vector3 startValue, Vector3 peakValue, float periodTime, float periods = -1f)
    {
        float threshold = periods > 0 ? periods : 1000000f;
        float t = 0;
        while (isOnline && !Mathf.Approximately(t, threshold))
        {
            t = Mathf.Clamp(t + Time.deltaTime / periodTime, -10f, threshold);
            transform.position = Vector3.Lerp(startValue, peakValue, moveCurve.Evaluate(t));
            yield return null;
        }
    }

    private Vector3 CalculateAppearingPos()
    {
        RaycastHit hit;
        if (!Physics.Raycast(transform.position, Vector3.up, out hit, 10f, BattleController.TerrainLayerMask))
        {
            return transform.position + Vector3.up * 9.5f;
        }

        return hit.point + Vector3.down * 0.5f;
    }

    public bool HasAccessibleTarget()
    {
        return CanCatch(target);
    }

    public bool CanCatch(VehicleController target)
    {
        return target != null
            && target.IsAvailable
            && Vector3.SqrMagnitude(transform.position - target.transform.position) < sqrWatchRadius
            && !Physics.Linecast(transform.position, target.Bounds.center, BattleController.ObstacleMask, QueryTriggerInteraction.Ignore);
    }

    public void ReturnToNormalRotation()
    {
        Transform trn = transform;
        Vector3 point = trn.position + trn.forward;
        point.y = trn.position.y;
        RotateToPoint(point);
    }

    public bool RotateToPoint(Vector3 point)
    {
        Vector3 targetDir = (point - transform.position).normalized;
        Quaternion trgDirQuat = Quaternion.LookRotation(targetDir, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, trgDirQuat, rotationSpeed * Time.deltaTime);

        return transform.rotation == trgDirQuat;
    }

    public void Attack()
    {
        foreach (var barrel in barrels)
        {
            PoolManager.GetObject<PoolEffect>(shotFX.GetResourcePath(owner.IsMain), barrel.position, barrel.rotation);
            Shell nextShell = PoolManager.GetObject<Shell>(shell.GetResourcePath(owner.IsMain), barrel.position, barrel.rotation);
            nextShell.Activate(owner, Damage, owner.HitMask, DamageSource.HenchmanDoes);
            AudioDispatcher.PlayClipAtPosition(shotSound, barrel.position, Settings.SoundVolume * SoundSettings.SHOT_VOLUME);
        }
    }

    public void Death()
    {
        if (owner.IsMain)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        PoolManager.GetObject<PoolEffect>(explosionFX.GetResourcePath(owner.IsMain), transform.position,
        transform.rotation);
        AudioDispatcher.PlayClipAtPosition(explosionSound, transform.position);
    }
}
