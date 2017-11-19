using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Disconnect;
using UnityEngine;
using XDevs.LiteralKeys;

using Random = UnityEngine.Random;

public abstract class BotAI
{
    protected event Action<VehicleController> TargetChanged = delegate {}; 

    public List<BonusItem> inaccessibleBonuses = new List<BonusItem>();
    protected float pathUpdatePeriod;
    protected float stuckTime;

    protected bool isWeaponReady = true;
    protected IEnumerator fireLagging;
    
    protected float botXAxisControl;
    protected float botYAxisControl;
    protected float botXAxisAltControl;
    protected float botYAxisAltControl;
    protected bool botFireButtonPressed;
    protected bool botSecondaryFireButtonPressed;
    protected float botTurretAxisControl;
        
    protected Stack<BotState> botStateStack = new Stack<BotState>();

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

    private Vector3 positionToMove;
    private VehicleController target;

    public CoroutineController CheckingIfStuckController { get { return checkingIfStuckController; } }
    public CoroutineController AimingController { get { return aimingController; } }
    public CoroutineController GettingClosestVehicleController { get { return gettingClosestVehicleController;} }
    public CoroutineController GettingClosestBonusController { get { return gettingClosestBonusController; } }
    public CoroutineController FindingOneShotEnemyController { get { return findingOneShotEnemyController; } }
    public CoroutineController FindTargetController { get { return findTargetController; } }
    public CoroutineController FindingPositionController { get { return findingPositionController; } }
    public CoroutineController PathUpdatingController { get { return pathUpdatingController; } }
    public CoroutineController ShootingController { get { return shootingController; } }

    public Vector3 PositionToMove
    {
        get
        {
            return positionToMove;
        }
        set
        {
            //Debug.LogError(string.Format("<color=\"cyan\">PositionToMove</color> for {0} changed! value = {1}", ThisVehicle.name, value), ThisVehicle.gameObject);
            positionToMove = value;
        }
    }

    public VehicleController Target
    {
        get
        {
            return target;
        }
        set
        {
            bool targetChanged = value != target;

            target = value;

            if (targetChanged)
                TargetChanged(value);
        }
    }

    public PhotonView PhotonView { get { return ThisVehicle.PhotonView; } }

    public bool BotTargetAimed
    {
        get
        {
            return Target != null && ThisVehicle.TargetAimed &&
                    !VehicleController.AreFriends(ThisVehicle, ThisVehicle.Target);
        }
    }

    #region BotControls

    public float XAxisControl { get { return botXAxisControl; }  }

    public float YAxisControl { get { return botYAxisControl; } }

    public float XAxisAltControl { get { return botXAxisAltControl; } }

    public float YAxisAltControl { get { return botYAxisAltControl; } }

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

    public BotState RevengeState { get; private set; }

    public BotState OneShotKillState { get; private set; }

    #endregion

    #region BotBehaviours

    public BotBehaviour CurrentBehaviour { get; private set; }

    public BotBehaviour TargetBehaviour { get; private set; }

    public BotBehaviour FighterBehaviour { get; private set; }

    public BotBehaviour AgressorBehaviour { get; private set; }

    public BotBehaviour TutorialBehaviour { get; private set; }

    #endregion

    #region Other

    public BonusItem ClosestBonus { get; private set; }

    public VehicleController ThisVehicle { get; private set; }

    public VehicleController ClosestEnemyVehicle { get; private set; }

    public float StopSqrDistanceToOtherVehicles { get; private set; }

    public TankData VehicleData { get { return ThisVehicle.data; } }

    public float WeaponReloadingProgress { get { return ThisVehicle.WeaponReloadingProgress; } }

    public VehicleController.AimPointInfo AimPoint { get { return ThisVehicle.AimPoint; } }

    public int CurrentWaypoint { get; set; }

    public Vector3 CurrentWaypointPos { get; protected set; }

    #endregion

    #region AI Common

    protected BotAI(VehicleController vehicleController)
    {
        ThisVehicle = vehicleController;
        TargetChanged += OnTargetChanged;
    }

     ~BotAI()
    {
        TargetChanged -= OnTargetChanged;
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
        StopSqrDistanceToOtherVehicles = BotSettings.stopSqrDistancesToOtherVehicles_s.RandWithinRange;
        stuckTime = BotSettings.timesToStuck_s.RandWithinRange;
        pathUpdatePeriod = BotSettings.pathUpdateDelays_s.RandWithinRange;

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
        ThisVehicle.TakeBonus(bonusType, amount);
    }

    public VehicleController GetClosestEnemyVehicle()
    {
        ClosestEnemyVehicle = null;
        var minDist = float.MaxValue;

        foreach (var vehicle in BattleController.allVehicles.Values)
        {
            if (VehicleController.AreFriends(vehicle, ThisVehicle))
            {
                continue;
            }

            if (vehicle == null || vehicle == ThisVehicle || !vehicle.IsAvailable || CurrentBehaviour.CheckIfBro(vehicle))
            {
                continue;
            }

            if (CheckIfHumanTargetPreference(vehicle))
            {
                continue;
            }

            var dist = Vector3.SqrMagnitude(ThisVehicle.transform.position - vehicle.transform.position);
            
            if (dist < minDist && !CurrentBehaviour.CheckIfBro(vehicle))
            {
                ClosestEnemyVehicle = vehicle;
                minDist = dist;
            }
        }
           
        if (minDist < StopSqrDistanceToOtherVehicles)
        {
            Target = ClosestEnemyVehicle;
        }

        return ClosestEnemyVehicle;
    }

    public BonusItem GetClosestBonus()
    {
        if (!Physics.CheckSphere(ThisVehicle.transform.position, BotSettings.bonusSeekingRadius_s, MiscTools.GetLayerMask(Layer.Key.Bonus), QueryTriggerInteraction.Collide))
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
            ThisVehicle.Aiming();
            yield return null;
        }
    }

    public void SetState(BotState newState, bool saveCurrentState = false)
    {
        if (newState == null || BattleController.MyVehicle == null || !BattleConnectManager.IsMasterClient)
        {
            return;
        }

        if (saveCurrentState)
        {
            botStateStack.Push(CurrentState);
            CurrentState = newState;
        }
        else
        {
            CurrentState = botStateStack.Count > 0 ? botStateStack.Pop() : newState;
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
                break;
            case BotDispatcher.BotBehaviours.Agressor:
                SetBotBehaviour(AgressorBehaviour);
                break;
            case BotDispatcher.BotBehaviours.Tutorial:
                SetBotBehaviour(TutorialBehaviour);
                break;
        }
    }

    public IEnumerator GettingClosestVehicle()
    {
        while (true)
        {
            ClosestEnemyVehicle = GetClosestEnemyVehicle();
            yield return new WaitForSeconds(MiscTools.random.Next(50, 100) * 0.1f);
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

            yield return new WaitForSeconds(MiscTools.random.Next(30, 80) * 0.1f);
        }
    }

    public IEnumerator RandomlyChangingBehaviour()
    {
        while (true)
        {
            var delay = MiscTools.random.Next(30, 40);

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

    public void SetSpeed(float speed)
    {
        botYAxisControl = speed;
    }

    public void SetRotation(float rotation)
    {
        botXAxisControl = rotation;
    }

    private bool CheckIfHumanTargetPreference(VehicleController vehicle)
    {
        return !GameData.IsTeamMode && vehicle.IsBot && CurrentBehaviour.HumanTargetPreference;
    }

    public VehicleController FindWeakestEnemyVehicle()
    {
        VehicleController weakestPlayer = null;
        float minHealth = float.MaxValue;

        foreach (var vehicle in BattleController.allVehicles.Values)
        {
            if (VehicleController.AreFriends(vehicle, ThisVehicle))
            {
                continue;
            }

            if (vehicle == ThisVehicle || !vehicle.IsAvailable || CurrentBehaviour.CheckIfBro(vehicle))
            {
                continue;
            }

            if (CheckIfHumanTargetPreference(vehicle))
            {
                continue;
            }

            var currentVehicleArmor = vehicle.Armor;
            if (currentVehicleArmor < minHealth)
            {
                minHealth = currentVehicleArmor;
                weakestPlayer = vehicle;
            }
        }

        return weakestPlayer;
    }

    protected IEnumerator FindingOneShotEnemyVehicle()
    {
        yield return null;

        while (true)
        {
            if (Physics.CheckSphere(ThisVehicle.transform.position, BotSettings.oneShotTargetCheckDistance_s))
            {
                foreach (var vehicle in BattleController.allVehicles.Values)
                {
                    if (VehicleController.AreFriends(vehicle, ThisVehicle))
                    {
                        continue;
                    }

                    if (vehicle == null || vehicle == ThisVehicle || CurrentBehaviour.CheckIfBro(vehicle))
                    {
                        continue;
                    }

                    var sqrDist = Vector3.SqrMagnitude(ThisVehicle.transform.position - vehicle.transform.position);

                    if (sqrDist <= BotSettings.oneShotTargetCheckSqrDistance_s && vehicle.Armor < vehicle.data.attack && vehicle.IsAvailable)
                    {
                        Target = vehicle;
                        
                        SetState(OneShotKillState, CurrentState != StuckState);
                        yield break;
                    }
                }
            }

            yield return new WaitForSeconds(MiscTools.random.Next(50, 100) * 0.1f);
        } 
    }

    protected IEnumerator FireLagging()
    {
        while (true)
        {
            isWeaponReady = Random.Range(0, 100) < 70;
            yield return new WaitForSeconds(Random.Range(0.1f, 0.3f));
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

    protected abstract IEnumerator FindingPosition();

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

    public abstract void OnStateChange();
    public abstract Vector3 FindRandomPointNearPosition(Vector3 position, int radius = 10);
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

    public void OnBotTakesDamage(EventId id, EventInfo ei)
    {
        if (!PhotonView.isMine)
            return;

        var info = (EventInfo_U)ei;
        var vehicleData = ThisVehicle.data;

        if ((int)info[0] != vehicleData.playerId)
            return;

        int attackerId = (int)info[2];

        if (ThisVehicle.IsAvailable && ThisVehicle.Armor <= 0)
            Dispatcher.Send(
                id: EventId.TankKilled,
                info: new EventInfo_III(ThisVehicle.data.playerId, attackerId, (int)info[3]),
                target: Dispatcher.EventTargetType.ToAll);

        Dispatcher.Send(EventId.TankHealthChanged, new EventInfo_II(ThisVehicle.data.playerId, ThisVehicle.Armor));
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
        if (ThisVehicle.IsRequirePrimaryFire && ThisVehicle.PrimaryFire())
        {
            if (!ThisVehicle.Burst)
                ThisVehicle.StartBurst();
        }
        else
        {
            if (ThisVehicle.Burst)
                ThisVehicle.StopBurst();
        }
    }

    protected virtual void OnTargetChanged(VehicleController target) { }

    protected bool CheckBotLifetime()
    {
        if (ThisVehicle.IsBot && PhotonNetwork.time > ThisVehicle.KickBotAt)
        {
            if (BattleConnectManager.IsMasterClient)
                BotDispatcher.Instance.RemoveBot(ThisVehicle);
            return false;
        }

        return true;
    }

    protected void ReloadWeapons()
    {
        foreach (var weapon in ThisVehicle.weapons)
            weapon.Value.UpdateReloadingProgress();
    }

    public abstract void FindPositionToMove();
    
}
