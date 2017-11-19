using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Matchmaking;
using UnityEngine;
using XDevs.LiteralKeys;

public abstract class BotAI
{
    public List<BonusItem> inaccessibleBonuses = new List<BonusItem>();

    protected IEnumerator fireLagging;
    
    protected float botXAxisControl;
    protected float botYAxisControl;
    protected bool botFireButtonPressed;
    protected bool botSecondaryFireButtonPressed;
    protected float botTurretAxisControl;
    protected bool collisionAlert;

    protected BotState savedState;

    protected CoroutineController checkingIfStuckController;
    protected CoroutineController aimingController;
    protected CoroutineController gettingClosestVehicleController;
    protected CoroutineController gettingClosestBonusController;
    protected CoroutineController findingOneShotEnemyController;
    protected CoroutineController findTargetController;
    protected CoroutineController findingPositionController;
    protected CoroutineController pathUpdatingController;
    protected CoroutineController shootingController;
    protected Coroutine updatingRoutine;

    public CoroutineController CheckingIfStuckController { get { return checkingIfStuckController; } }
    public CoroutineController AimingController { get { return aimingController; } }
    public CoroutineController GettingClosestVehicleController { get { return gettingClosestVehicleController;} }
    public CoroutineController GettingClosestBonusController { get { return gettingClosestBonusController; } }
    public CoroutineController FindingOneShotEnemyController { get { return findingOneShotEnemyController; } }
    public CoroutineController FindTargetController { get { return findTargetController; } }
    public CoroutineController FindingPositionController { get { return findingPositionController; } }
    public CoroutineController PathUpdatingController { get { return pathUpdatingController; } }
    public CoroutineController ShootingController { get { return shootingController; } }

    public Coroutine UpdatingRoutine { get { return updatingRoutine; } }

    public Vector3 PositionToMove { get; set; }

    public VehicleController Target { get; set; }

    public PhotonView PhotonView { get { return ThisVehicle.PhotonView; } }

    public bool BotTargetAimed
    {
        get
        {
            return Target != null && ThisVehicle.TargetAimed &&
                    !StaticContainer.AreFriends(ThisVehicle, ThisVehicle.Target);
        }
    }

    #region BotControls

    public float XAxisControl { get { return botXAxisControl; }  }

    public float YAxisControl { get { return botYAxisControl; } }

    public bool FireButtonPressed { get { return botFireButtonPressed; } }

    public float TurretAxisControl
    {
        get { return botTurretAxisControl; }
        set { botTurretAxisControl = value; }
    }

    #endregion

    #region States

    public BotState CurrentState { get; private set; }

    public BotState StuckState { get; private set; }

    public BotState NormalState { get; private set; }

    public BotState StopState { get; private set; }

    public BotState TakingBonusState { get; private set; }

    public RevengeState RevengeState { get; private set; }

    public BotState OneShotKillState { get; private set; }

    public BotState DeadState { get; private set; }

    #endregion

    #region BotBehaviours

    public BotBehaviour CurrentBehaviour { get; private set; }

    public TargetBehaviour TargetBehaviour { get; private set; }

    public FighterBehaviour FighterBehaviour { get; private set; }

    public AgressorBehaviour AgressorBehaviour { get; private set; }

    public TutorialBehaviour TutorialBehaviour { get; private set; }

    #endregion

    #region Other

    public BonusItem ClosestBonus { get; private set; }

    public VehicleController ThisVehicle { get; private set; }

    public VehicleController ClosestEnemyVehicle { get; private set; }

    public VehicleController ClosestVehicle { get; private set; }

    public float StopSqrDistanceToOtherVehicles { get; private set; }

    public TankData VehicleData { get { return ThisVehicle.data; } }

    public float WeaponReloadingProgress { get { return ThisVehicle.WeaponReloadingProgress; } }

    //public AimPointInfo AimPoint { get { return ThisVehicle.Turret_.AimPoint; } }

    public int CurrentWaypoint { get; set; }

    public Vector3 CurrentWaypointPos { get; protected set; }

    #endregion

    #region AI Common

    protected BotAI(VehicleController vehicleController)
    {
        ThisVehicle = vehicleController;
    }

    public void ResetUpdatingCoroutine(IEnumerator ienumerator)
    {
        if (updatingRoutine != null)
        {
            ThisVehicle.StopCoroutine(updatingRoutine);
        }

        updatingRoutine = ThisVehicle.StartCoroutine(ienumerator);
    }

    public virtual void Init(BotDispatcher.BotBehaviours botBehaviour)
    {
        StopSqrDistanceToOtherVehicles = Mathf.Pow(ThisVehicle.BodyMeshBounds.size.z, 2)*3;  // три длины боди

        InitBotBehaviours(botBehaviour);
        InitBotStates(); 
    }

    public IEnumerator DelayAndStartBot()
    {
        yield return new WaitForSeconds(1);
        SetState(NormalState);
    }

    public void SetBotBehaviour(BotBehaviour behaviour)
    {
        CurrentBehaviour = behaviour;
    }

    public abstract IEnumerator CheckingIfStuck();

    public void TakeBonus(BonusItem.BonusType bonusType, int amount)
    {
        StaticContainer.TakeBonus(ThisVehicle, bonusType, amount);
    }

    public VehicleController GetClosestEnemyVehicle()
    {
        ClosestVehicle = null;
        ClosestEnemyVehicle = null;

        var minDist = float.MaxValue;

        foreach (var currentVehicle in BattleController.allVehicles.Values)
        {
            if (currentVehicle == null || currentVehicle == ThisVehicle || !currentVehicle.IsAvailable)
            {
                continue;
            }

            var distToVehicle = (ThisVehicle.transform.position - currentVehicle.transform.position).sqrMagnitude;

            if (minDist > distToVehicle)
            {
                ClosestVehicle = currentVehicle;
                minDist = distToVehicle;

                if (!StaticContainer.AreFriends(currentVehicle, ThisVehicle))
                {
                    if (CheckIfHumanTargetPreference())
                    {
                        if (!currentVehicle.IsBot)
                        {
                            ClosestEnemyVehicle = currentVehicle;
                        }
                    }
                    else
                    {
                        ClosestEnemyVehicle = currentVehicle;
                    }

                    if (minDist < StopSqrDistanceToOtherVehicles)
                    {
                        Target = ClosestEnemyVehicle;
                    }
                }
            }
        }

        return ClosestEnemyVehicle;
    }

    public BonusItem GetClosestBonus()
    {
        if (!Physics.CheckSphere(ThisVehicle.transform.position, CurrentBehaviour.BotSettings.BonusSeekingRadius.RandWithinRange, MiscTools.GetLayerMask(Layer.Key.Bonus), QueryTriggerInteraction.Collide))
        {
            return null;
        }

        var minDist = float.MaxValue;
        BonusItem bonus = null;

        foreach (var bonusItem in BonusDispatcher.Instance.BonusItems.Where(b => !b.IsTaken))
        {
            var dist = Vector3.SqrMagnitude(ThisVehicle.transform.position - bonusItem.transform.position);

            if (dist < minDist && !inaccessibleBonuses.Contains(bonusItem))
            {
                minDist = dist;
                bonus = bonusItem;
            }
        }

        return bonus;
    }

    public IEnumerator Aiming()
    {
        while (true)
        {
            ThisVehicle.aimingController.Aiming();
            yield return null;
        }
    }

    public void SetState(BotState newState, bool saveCurrentState = false)
    {
        if (CurrentState == newState || newState == null || 
            BattleController.MyVehicle == null || !PhotonNetwork.isMasterClient || !newState.CanSwitchToThisState)
            return;

        if (saveCurrentState)
        {
            savedState = CurrentState;
            CurrentState = newState;
        }
        else if(savedState != null)
        {
            CurrentState = savedState;
            savedState = null;
        }
        else
        {
            CurrentState = newState;
        }

        CurrentState.OnStart();
    }

    public void GetDirectionsToTarget()
    {
        PositionToMove = FindRandomPointNearPosition(Target.transform.position);
        FindPath();
    }

    public void InitBotStates() 
    {
        NormalState = new NormalState(this);
        StuckState = new StuckState(this);
        StopState = new StopState(this);
        TakingBonusState = new TakingBonusState(this);
        RevengeState = new RevengeState(this);
        OneShotKillState = new OneShotKillState(this);
        DeadState = new DeadState(this);

        CurrentState = NormalState;
    }

    public void InitBotBehaviours(BotDispatcher.BotBehaviours botBehaviour)
    {
        TargetBehaviour = new TargetBehaviour(this); 
        FighterBehaviour = new FighterBehaviour(this);
        AgressorBehaviour = new AgressorBehaviour(this);
        TutorialBehaviour = new TutorialBehaviour(this);

        switch (botBehaviour)
        {
            case BotDispatcher.BotBehaviours.Target:
                SetBotBehaviour(TargetBehaviour);
                break;
            case BotDispatcher.BotBehaviours.Fighter:
                SetBotBehaviour(FighterBehaviour);
                FighterBehaviour.BotAI.ThisVehicle.StartCoroutine(FighterBehaviour.CheckingIfTargetAimed());
                break;
            case BotDispatcher.BotBehaviours.Agressor:
                SetBotBehaviour(AgressorBehaviour);
                break;
            case BotDispatcher.BotBehaviours.Tutorial:
                SetBotBehaviour(TutorialBehaviour);
                break;
        }

        CurrentBehaviour.StartSettingHumanTargetPreference();
    }

    public IEnumerator GettingClosestVehicle()
    {
        while (true)
        {
            ClosestEnemyVehicle = GetClosestEnemyVehicle();
            yield return new WaitForSeconds(CurrentBehaviour.BotSettings.GettingClosestVehicleDelay.RandWithinRange);
        }
    }

    public IEnumerator GettingClosestBonus()
    {
        yield return null;

        while (true)
        {
            if (CurrentState != RevengeState && CurrentState != StuckState)
            {
                ClosestBonus = GetClosestBonus();
            }

            if (ClosestBonus != null)
            {
                SetState(TakingBonusState, true);
            }

            yield return new WaitForSeconds(CurrentBehaviour.BotSettings.GettingClosestBonusDelays.RandWithinRange);
        }
    }

    public IEnumerator RandomlyChangingBehaviour()
    {
        while (true)
        {
            var delay = CurrentBehaviour.BotSettings.ChangingBehaviourDelays.RandWithinRange;

            yield return new WaitForSeconds(delay);

            var botType = BotDispatcher.SelectRandomBotType();

            switch (botType)
            {
                case BotDispatcher.BotBehaviours.Agressor:
                    SetBotBehaviour(AgressorBehaviour);
                    break;
                case BotDispatcher.BotBehaviours.Fighter:
                    SetBotBehaviour(FighterBehaviour);
                    break;
                case BotDispatcher.BotBehaviours.Target:
                    SetBotBehaviour(TargetBehaviour);
                    break;
            }
        }
    }

    private bool CheckIfHumanTargetPreference()
    {
        return !GameData.IsTeamMode && CurrentBehaviour.HumanTargetPreference;
    }

    public VehicleController FindWeakestEnemyVehicle()
    {
        VehicleController weakestPlayer = null;
        float minHealth = float.MaxValue;

        foreach (var vehicle in BattleController.allVehicles.Values)
        {
            if (StaticContainer.AreFriends(vehicle, ThisVehicle))
            {
                continue;
            }

            if (vehicle == ThisVehicle || !vehicle.IsAvailable || CurrentBehaviour.CheckIfBro(vehicle))
            {
                continue;
            }

            if (CheckIfHumanTargetPreference())
            {
                if (!vehicle.IsBot)
                {
                    var currentVehicleArmor = vehicle.Armor;
                    if (currentVehicleArmor < minHealth)
                    {
                        minHealth = currentVehicleArmor;
                        weakestPlayer = vehicle;
                    }
                }
            }
            else if (vehicle.IsBot)
            {
                var currentVehicleArmor = vehicle.Armor;
                if (currentVehicleArmor < minHealth)
                {
                    minHealth = currentVehicleArmor;
                    weakestPlayer = vehicle;
                }
            } 
        }

        return weakestPlayer;
    }

    protected IEnumerator FindingOneShotEnemyVehicle()
    {
        yield return null;

        while (true)
        {
            foreach (var vehicle in BattleController.allVehicles.Values)
            {
                if (StaticContainer.AreFriends(vehicle, ThisVehicle))
                {
                    continue;
                }

                if (vehicle == null || vehicle == ThisVehicle || CurrentBehaviour.CheckIfBro(vehicle))
                {
                    continue;
                }

                var sqrDist = Vector3.SqrMagnitude(ThisVehicle.transform.position - vehicle.transform.position);

                if (sqrDist <= CurrentBehaviour.BotSettings.OneShotTargetCheckSqrDistance && vehicle.Armor < ThisVehicle.data.attack && vehicle.IsAvailable)
                {
                    Target = vehicle;
                        
                    SetState(OneShotKillState, CurrentState != StuckState);
                    yield break;
                }
             }

            yield return new WaitForSeconds(CurrentBehaviour.BotSettings.FindingOneShotEnemyDelays.RandWithinRange);
        } 
    }

    protected IEnumerator FindingTarget()
    {
        while (true)
        {
            CurrentBehaviour.FindTarget();
            yield return new WaitForSeconds(CurrentBehaviour.FindingTargetDelay);
        }
    }

    protected virtual IEnumerator Shooting()
    {
        while (true)
        {
            if (BotTargetAimed && WeaponReloadingProgress >= 1)
            {
                yield return ThisVehicle.StartCoroutine(CurrentBehaviour.Shoot());
                yield return new WaitForSeconds(CurrentBehaviour.MainWeaponReloadTime);
            }

            yield return null;
        }
    }

    public void StartBotAICoroutines()
    {
        ThisVehicle.StartCoroutineEx(PathUpdating(), out pathUpdatingController);
        ThisVehicle.StartCoroutineEx(CheckingIfStuck(), out checkingIfStuckController);
        ThisVehicle.StartCoroutineEx(Aiming(), out aimingController);
        ThisVehicle.StartCoroutineEx(GettingClosestVehicle(), out gettingClosestVehicleController);
        ThisVehicle.StartCoroutineEx(GettingClosestBonus(), out gettingClosestBonusController);
        ThisVehicle.StartCoroutineEx(FindingOneShotEnemyVehicle(), out findingOneShotEnemyController);
        ThisVehicle.StartCoroutineEx(FindingTarget(), out findTargetController);
        ThisVehicle.StartCoroutineEx(Shooting(), out shootingController);
        ThisVehicle.StartCoroutineEx(FindingPosition(), out findingPositionController);
    }

    public void EffectItself(VehicleEffect effect, bool positive)
    {
        ThisVehicle.EffectItself(effect, positive);
    }

    public void StopVehicle()
    {
        botXAxisControl = 0;
        botYAxisControl = 0;
    }

    public abstract void OnStateChange();
    public abstract Vector3 FindRandomPointNearPosition(Vector3 pos, int radius = 10);
    public abstract void Move(bool forwards = true);
    public abstract void Move(float speed, float rotSpeed);
    public abstract IEnumerator NormalStateUpdating();
    public abstract IEnumerator OneShotStateUpdating();
    public abstract IEnumerator RevengeStateUpdating();
    public abstract IEnumerator StopStateUpdating();
    public abstract IEnumerator StuckStateUpdating();
    public abstract IEnumerator TakingBonusStateUpdating();
    public abstract IEnumerator PathUpdating();
    public abstract bool RotateToWaypoint();
    public abstract IEnumerator Fire(); 
    public abstract bool FindPath();
    protected abstract IEnumerator FindingPosition();

    #endregion

    public void OnBotDestroy()
    {
        BotDispatcher.BotUnsubscribes(this);
    }

    public void OnBotBehaviourTakesDamage(EventId id, EventInfo info)
    {
        CurrentBehaviour.OnBotTakesDamage(id, info);
    }

    public void OnBotBehaviourVehicleKilled(EventId id, EventInfo info)
    {
        CurrentBehaviour.OnVehicleKilled(id, info);
    }

    public void OnBotBehaviourVehicleLeft(EventId id, EventInfo info)
    {
        CurrentBehaviour.OnVehicleLeftTheGame(id, info);
    }

    public void OnBotBehaviourBonusDestroyed(EventId id, EventInfo info)
    {
        CurrentBehaviour.OnBonusDestroyed(id, info);
    }

    public void OnBotBehaviourTankRespawned(EventId id, EventInfo info)
    {
        CurrentBehaviour.OnBotRespawned(id, info);
    }

    public virtual void MyBotUpdate()
    { 
        ReloadWeapons();
        CheckIfFireNeed();
        CheckBotLifetime();
    }

    public void OthersBotUpdate()
    {
        if (!PhotonView.isMine)
        {
            ThisVehicle.MoveClone();
            ThisVehicle.AnimateClone();
            ThisVehicle.StoreCloneRotation();
        }
    }

    protected virtual void CheckIfFireNeed()
    {
        if (ThisVehicle.IsRequirePrimaryFire && ThisVehicle.turretController.PrimaryFire())
        {
            if (!ThisVehicle.Burst)
            {
                ThisVehicle.StartBurst();
            }
        }
        else
        {
            if (ThisVehicle.Burst)
            {
                ThisVehicle.StopBurst();
            }
        }
    }

    protected bool CheckBotLifetime()
    {
        if (ThisVehicle.IsBot && PhotonNetwork.time > ThisVehicle.KickBotAt)
        {
            if (PhotonNetwork.isMasterClient)
                BotDispatcher.Instance.RemoveBot(ThisVehicle);
            return false;
        }

        return true;
    }

    protected void ReloadWeapons()
    {
        ThisVehicle.turretController.FullRealoadingUpdate();
    }

    public abstract void FindPositionToMove();
    public abstract void MoveToBonus();

}
