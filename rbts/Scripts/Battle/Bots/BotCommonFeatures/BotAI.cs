using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XDevs.LiteralKeys;

public abstract class BotAI
{
    public const float NAVMESH_AGENT_RADIUS = 7f;

    public HashSet<BonusItem> inaccessibleBonuses = new HashSet<BonusItem>();
    protected float pathUpdatePeriod;
    protected float stuckTime;
    protected BotMadness botMadness;

    protected bool isWeaponReady = true;
    protected IEnumerator fireLagging;
    
    protected float botXAxisControl;
    protected float botYAxisControl;
    protected bool botFireButtonPressed;
    protected bool botSecondaryFireButtonPressed;
    protected float botTurretAxisControl;
        
    protected Stack<BotState> botStateStack = new Stack<BotState>();

    protected CoroutineController checkingIfStuckController;
    protected CoroutineController gettingClosestVehicleController;
    protected CoroutineController gettingClosestBonusController;
    protected CoroutineController findingOneShotEnemyController;
    protected CoroutineController findTargetController;
    protected CoroutineController findingPositionController;
    protected CoroutineController pathUpdatingController;
    protected CoroutineController shootingController;
    protected Coroutine updatingRoutine;
    
    protected Bounds bodyMeshBounds;

    public CoroutineController CheckingIfStuckController { get { return checkingIfStuckController; } }
    public CoroutineController GettingClosestVehicleController { get { return gettingClosestVehicleController;} }
    public CoroutineController GettingClosestBonusController { get { return gettingClosestBonusController; } }
    public CoroutineController FindingOneShotEnemyController { get { return findingOneShotEnemyController; } }
    public CoroutineController FindTargetController { get { return findTargetController; } }
    public CoroutineController FindingPositionController { get { return findingPositionController; } }
    public CoroutineController PathUpdatingController { get { return pathUpdatingController; } }
    public CoroutineController ShootingController { get { return shootingController; } }

    public Vector3 PositionToMove { get; set; }

    public VehicleController Target { get; set; }

    public PhotonView PhotonView { get { return ThisVehicle.PhotonView; } }

    public bool BotTargetAimed
    {
        get
        {
            return ThisVehicle.TargetAimed &&
                    ThisVehicle.Target == Target;
        }
    }

    #region BotControls

    public float XAxisControl { get { return ThisVehicle.Blinded ? botMadness.GetHorizAxis() : botXAxisControl; }  }

    public float YAxisControl { get { return ThisVehicle.Blinded ? botMadness.GetVertAxis() : botYAxisControl; } }

    public bool FireButtonPressed
    {
        get
        {
            return ThisVehicle.Blinded ? botMadness.GetFireStatus() : botFireButtonPressed;
        }
    }

    #endregion

    #region States

    public bool Stuck { get { return CurrentState == StuckState; } }

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

    public VehicleData VehicleData { get { return ThisVehicle.data; } }

    //public float WeaponReloadingProgress { get { return ThisVehicle.WeaponReloadingProgress; } }


    public int CurrentWaypointInd { get; protected set; }

    public Vector3 CurrentWaypointPos { get; protected set; }

    #endregion

    #region AI Common

    protected BotAI(VehicleController vehicleController, BotDispatcher.BotBehaviours botBehaviour)
    {
        ThisVehicle = vehicleController;
        bodyMeshBounds = ThisVehicle.BodyMesh.bounds;

        StopSqrDistanceToOtherVehicles = BotSettings.stopSqrDistancesToOtherVehicles_s.RandWithinRange;
        stuckTime = BotSettings.timesToStuck_s.RandWithinRange;
        pathUpdatePeriod = BotSettings.pathUpdateDelays_s.RandWithinRange;

        InitBotBehaviours(botBehaviour);
        InitBotStates();

        botMadness = new BotMadness();
    }

    public void ResetUpdatingCoroutine(IEnumerator ienumerator)
    {
        if (updatingRoutine != null)
        {
            ThisVehicle.StopCoroutine(updatingRoutine);
        }

        updatingRoutine = ThisVehicle.StartCoroutine(ienumerator);
    }

    public IEnumerator DelayAndStartBot()
    {
        yield return new WaitForSeconds(1);
        SetState(NormalState);
    }

    public void SetBotBehaviour(BotBehaviour behaviour)
    {
        CurrentBehaviour = behaviour;
        behaviour.Apply();
    }

    public abstract IEnumerator CheckingIfStuck();

    public void TakeBonus(BonusItem.BonusType bonusType, int amount)
    {
        ThisVehicle.TakeBonus(bonusType, amount);
    }

    public VehicleController RefreshClosestEnemy()
    {
        ClosestEnemyVehicle = null;
        var minDist = float.MaxValue;

        foreach (var vehicle in BattleController.allVehicles.Values)
        {
            if (vehicle == null
                || vehicle == ThisVehicle
                || !vehicle.IsAvailable
                || VehicleController.AreFriends(vehicle, ThisVehicle)
                || CurrentBehaviour.CheckIfBro(vehicle)
                || CheckIfHumanTargetPreference(vehicle))
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

    public void SetState(BotState newState, bool saveCurrentState = false)
    {
        if (newState == null || BattleController.MyVehicle == null || !PhotonNetwork.isMasterClient)
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

    public void CalcPathToTarget()
    {
        if (Target == null)
            return;

        PositionToMove = FindRandomPointNearPosition(Target.transform.position, 1f);
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
            RefreshClosestEnemy();
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

    private bool CheckIfHumanTargetPreference(VehicleController vehicle)
    {
        return !GameData.IsTeamMode && !vehicle.IsBot && CurrentBehaviour.HumanTargetPreference;
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

    protected virtual IEnumerator ShootCheck()
    {
        while (true)
        {
            botFireButtonPressed = BotTargetAimed;
            yield return null;
        }
    }

    protected abstract IEnumerator FindingPosition();

    public void StartBotAICoroutines()
    {
        ThisVehicle.StartCoroutineEx(PathUpdating(), out pathUpdatingController);
        ThisVehicle.StartCoroutineEx(CheckingIfStuck(), out checkingIfStuckController);
        ThisVehicle.StartCoroutineEx(GettingClosestVehicle(), out gettingClosestVehicleController);
        ThisVehicle.StartCoroutineEx(GettingClosestBonus(), out gettingClosestBonusController);
        ThisVehicle.StartCoroutineEx(FindingOneShotEnemyVehicle(), out findingOneShotEnemyController);
        ThisVehicle.StartCoroutineEx(FindingTarget(), out findTargetController);
        ThisVehicle.StartCoroutineEx(ShootCheck(), out shootingController);
        ThisVehicle.StartCoroutineEx(FindingPosition(), out findingPositionController);
    }

    public abstract void OnStateChange();
    public abstract Vector3 FindRandomPointNearPosition(Vector3 pos, float radius = 0f);
    public abstract void Move();
    public abstract void Move(float speed, float rotSpeed);
    public abstract IEnumerator NormalStateUpdating();
    public abstract IEnumerator OneShotStateUpdating();
    public abstract IEnumerator RevengeStateUpdating();
    public abstract IEnumerator StopStateUpdating();
    public abstract IEnumerator StuckStateUpdating();
    public abstract IEnumerator TakingBonusStateUpdating();
    public abstract IEnumerator PathUpdating();
    protected abstract bool RotateToWaypoint();
    protected abstract bool FindPath();

    #endregion

    public void OnBotDestroy()
    {
        BotDispatcher.BotUnsubscribes(this);
        CurrentBehaviour.OnBotDestroy();
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
            Messenger.Send(
                id: EventId.VehicleKilled,
                info: new EventInfo_II(ThisVehicle.data.playerId, attackerId),
                target: Messenger.EventTargetType.ToAll);

        Messenger.Send(EventId.TankHealthChanged, new EventInfo_II(ThisVehicle.data.playerId, ThisVehicle.Armor));
    }

    public abstract void FindPositionToMove();

    public override string ToString()
    {
        return string.Format("Name: {3}\nBehaviour: {0}\nState: {1}\nTarget: {2}",
            CurrentBehaviour,
            CurrentState,
            Target != null ? (string) Target.data.playerName : "",
            ThisVehicle.data.playerName);
    }
}
