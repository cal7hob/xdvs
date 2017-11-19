using System.Collections;
using System.Collections.Generic;
using Matchmaking;
using UnityEngine;
using XD;
using XDevs.LiteralKeys;

public abstract class BotAI
{
    protected float             pathUpdatePeriod = 0;
    protected float             stuckTime = 0;
    protected float             botXAxisControl;
    protected float             botYAxisControl;

    protected bool              isWeaponReady = true;
    protected bool              botFireButtonPressed;

    protected IEnumerator       fireLagging = null;

    protected Stack<BotState>   botStateStack = new Stack<BotState>();

    public IEnumerator AimingRoutine
    {
        get;
        protected set;
    }

    public PhotonView PhotonView
    {
        get
        {
            return ThisVehicle.PhotonView;
        }
    }

    #region BotControls
    public float XAxisControl
    {
        get
        {
            return botXAxisControl;
        }
    }

    public float YAxisControl
    {
        get
        {
            return botYAxisControl;
        }
    }

    public bool FireButtonPressed
    {
        get
        {
            return botFireButtonPressed;
        }
    }
    #endregion

    #region States
    public BotState CurrentState
    {
        get;
        private set;
    }

    public BotState StuckState
    {
        get;
        private set;
    }

    public BotState NormalState
    {
        get;
        private set;
    }

    public BotState RollBackState
    {
        get;
        private set;
    }

    public BotState StopState
    {
        get;
        private set;
    }

    public BotState TakingBonusState
    {
        get;
        private set;
    }

    public BotState RevengeState
    {
        get;
        private set;
    }

    public BotState OneShotKillState
    {
        get;
        private set;
    }

    public CloseToSomebodyState CloseToSomebodyState
    {
        get;
        private set;
    }
    #endregion

    #region BotBehaviours

    public BotBehaviour CurrentBehaviour
    {
        get; private set;
    }

    public BotBehaviour TargetBehaviour
    {
        get; private set;
    }

    public BotBehaviour FighterBehaviour
    {
        get; private set;
    }

    public BotBehaviour AgressorBehaviour
    {
        get; private set;
    }

    public BotBehaviour TutorialBehaviour
    {
        get; private set;
    }

    #endregion

    #region Other
    public IEnumerator LocalAvoidanceRoutine
    {
        get;
        protected set;
    }

    public bool VehicleInFront
    {
        get;
        protected set;
    }

    public BonusItem ClosestBonus
    {
        get;
        private set;
    }

    public VehicleController ThisVehicle
    {
        get;
        private set;
    }

    public VehicleController ClosestEnemyVehicle
    {
        get;
        private set;
    }

    public LayerMask EnemyLayerMask
    {
        get;
        private set;
    }

    public float StopDistanceToOtherVehicles
    {
        get;
        private set;
    }

    public TankData VehicleData
    {
        get
        {
            return ThisVehicle.data;
        }
    }

    public bool TargetAimed
    {
        get;
        protected set;
    }

    public float WeaponReloadingProgress
    {
        get
        {
            return ThisVehicle.WeaponReloadingProgress;
        }
    }

    public AimPointInfo AimPoint
    {
        get
        {
            return ThisVehicle.AimPoint;
        }
    }

    public int CurrentWaypoint
    {
        get;
        set;
    }

    public Vector3 CurrentWaypointPos
    {
        get;
        protected set;
    }
    #endregion

    #region Routines
    public IEnumerator GettingClosestVehicleRoutine
    {
        get;
        private set;
    }

    public IEnumerator CheckingIfStuckRoutine
    {
        get;
        private set;
    }

    public IEnumerator GettingClosestBonusRoutine
    {
        get;
        private set;
    }

    public IEnumerator FindingOneShotPlayerRoutine
    {
        get;
        private set;
    }
    #endregion

    #region AI Common
    public void Init(BotDispatcher.BotBehaviours botBehaviour)
    {
        StopDistanceToOtherVehicles = Random.Range(BotSettings.minStopDistanceToOtherVehicles_s, BotSettings.maxStopDistanceToOtherVehicles_s);
        stuckTime = Random.Range(BotSettings.minTimeToStuck_s, BotSettings.maxTimeToStuck_s);
        pathUpdatePeriod = Random.Range(BotSettings.minPathUpdatePeriod_s, BotSettings.maxPathUpdatePeriod_s);

        EnemyLayerMask = ThisVehicle.EnemyLayerMask;

        InitBotStates();
        InitBotBehaviours(botBehaviour);

        ThisVehicle.StartCoroutine(WaitForNavMesh());
    }

    private IEnumerator WaitForNavMesh()
    {
        yield return new WaitForSeconds(1);
        SetState(NormalState);
    }

    public void SetBotBehaviour(BotBehaviour behaviour)
    {
        CurrentBehaviour = behaviour;
        ThisVehicle.StopAllCoroutines();
        SetState(NormalState);
    }

    public void StartCheckingIfStuck()
    {
        CheckingIfStuckRoutine = RestartRoutine(CheckingIfStuckRoutine, CheckingIfStuck());
    }

    public void StartGettingClosestVehicle()
    {
        GettingClosestVehicleRoutine = RestartRoutine(GettingClosestVehicleRoutine, GettingClosestVehicle());
    }
    
    public void StartFindingOneShotPlayer()
    {
        FindingOneShotPlayerRoutine = RestartRoutine(FindingOneShotPlayerRoutine, FindingOneShotEnemyVehicle());
    }

    public void TakeBonus(BonusItem.BonusType bonusType, int amount)
    {
    }

    public VehicleController GetClosestEnemyVehicle(ref float minDist)
    {
        if (ThisVehicle == null)
        {
            return null;
        }

        VehicleController ClosestEnemyVehicle = null;

        foreach (var vehicle in StaticContainer.BattleController.Units.Values)
        {
            if (VehicleController.AreFriends(vehicle, ThisVehicle))
            {
                continue;
            }

            if (vehicle == null || vehicle == ThisVehicle || !vehicle.IsAvailable /*|| CurrentBehaviour.CheckIfBro(vehicle)*/)
            {
                continue;
            }

            //Vector3 startPoint = ThisVehicle.Transform.position + Vector3.up;
            //Vector3 targetPoint = vehicle.Transform.position + Vector3.up;
            //if (Physics.Linecast(startPoint, targetPoint, ThisVehicle.CheckObstacleMask))
            //{
            //    continue;
            //}

            //if (CheckTargetPreference(vehicle))
            //{              
            //    continue;
            //}

            float dist = Vector3.SqrMagnitude(ThisVehicle.Transform.position - vehicle.Transform.position);

            if (dist < minDist)
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

        foreach (var bonusItem in BonusDispatcher.Instance.BonusItems)
        {
            var dist = Vector3.SqrMagnitude(ThisVehicle.transform.position - bonusItem.transform.position);

            if (dist < minDist)
            {
                minDist = dist;
                bonus = bonusItem;
            }
        }

        return bonus;
    }

    public IEnumerator Aiming()
    {
        while (ThisVehicle != null && ThisVehicle.IsAvailable)
        {
            ThisVehicle.AimingBehaviour.Aiming();
            yield return null;
        }
    }

    public void StartAiming()
    {
        //AimingRoutine = RestartRoutine(AimingRoutine, Aiming());
    }
    
    public IEnumerator CheckingIfStuck()
    {
        while (ThisVehicle != null && ThisVehicle.IsAvailable)
        {
            Vector3 savedPos = ThisVehicle.transform.position;
            Quaternion savedRotation = ThisVehicle.transform.rotation;

            yield return new WaitForSeconds(stuckTime);

            if ((ThisVehicle.transform.position - savedPos).sqrMagnitude < BotSettings.stuckSqrMagnitude_s &&
                Quaternion.Angle(savedRotation, ThisVehicle.transform.rotation) < 5)
            {
                SetState(StuckState, CurrentState != StuckState);
            }
        }
    }

    public IEnumerator RestartRoutine(IEnumerator savedRoutine, IEnumerator currentRoutine)
    {
        if (ThisVehicle == null)
        {
            return null;
        }

        if (savedRoutine != null)
        {
            ThisVehicle.StopCoroutine(savedRoutine);
        }

        ThisVehicle.StartCoroutine(currentRoutine);

        return currentRoutine;
    }

    public void SetState(BotState newState, bool saveCurrentState = false)
    {
        if (!ThisVehicle.IsAvailable)
        {
            return;
        }

        if (newState == null)
        {
            return;
        }

        if (CurrentState == newState)
        {
            return;
        }

        if (CurrentState != null)
        {
            CurrentState.OnFinish();
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

    public void InitBotStates()
    {
        NormalState = new NormalState(this);
        StuckState = new StuckState(this);
        CloseToSomebodyState = new CloseToSomebodyState(this);
        StopState = new StopState(this);
        TakingBonusState = new TakingBonusState(this);
        RollBackState = new RollBackState(this);
        RevengeState = new RevengeState(this);
        OneShotKillState = new OneShotKillState(this);
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

            case BotDispatcher.BotBehaviours.TutorialBot:
                SetBotBehaviour(TutorialBehaviour);
                break;
        }
    }

    public IEnumerator GettingClosestVehicle()
    {
        while (ThisVehicle != null && ThisVehicle.IsAvailable)
        {
            float minDist = float.MaxValue;
            VehicleController vehicle = GetClosestEnemyVehicle(ref minDist);

            if (ClosestEnemyVehicle != vehicle)
            {
                ClosestEnemyVehicle = vehicle;
                string myColor = ThisVehicle.Team != StaticContainer.GameManager.Team ? "red" : "green";
                string enemyColor = (vehicle != null && vehicle.Team != StaticContainer.GameManager.Team) ? "red" : "green";

                //Debug.LogFormat(ThisVehicle, "'{0}' set closest enemy '{1}'", ThisVehicle.GetName().RichString("color:" + myColor), ClosestEnemyVehicle.GetName().RichString("color:" + enemyColor));

                //if (minDist < StopDistanceToOtherVehicles || ClosestEnemyVehicle == null)  
                if (CurrentBehaviour.Target != ClosestEnemyVehicle)
                {
                    CurrentBehaviour.Target = ClosestEnemyVehicle;
                }
            }
            yield return new WaitForSeconds(Random.Range(2.5f, 4));
        }
    }
    
    public IEnumerator RandomlyChangingBehaviour()
    {
        while (ThisVehicle != null)
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

    public void StopVehicle()
    {
        botXAxisControl = 0;
        botYAxisControl = 0;
    }

    public void SetSpeed(float speed)
    {
        botYAxisControl = speed;
    }

    public void SetRotation(float rotation)
    {
        botXAxisControl = rotation;
    }

    private bool CheckTargetPreference(VehicleController vehicle)
    {
        return !GameData.IsTeamMode && vehicle.IsBot && CurrentBehaviour.HumanTargetPreference;
    }

    public VehicleController FindWeakestEnemyVehicle()
    {
        VehicleController weakestPlayer = null;
        float minHealth = float.MaxValue;

        foreach (var vehicle in StaticContainer.BattleController.Units.Values)
        {
            if (VehicleController.AreFriends(vehicle, ThisVehicle))
            {
                continue;
            }

            if (vehicle == ThisVehicle || !vehicle.IsAvailable/* || CurrentBehaviour.CheckIfBro(vehicle)*/)
            {
                continue;
            }

            //if (CheckTargetPreference(vehicle))
            //{
            //    continue;
            //}

            var currentVehicleArmor = vehicle.HPSystem.Armor;
            if (currentVehicleArmor < minHealth)
            {
                minHealth = currentVehicleArmor;
                weakestPlayer = vehicle;
            }
        }

        return weakestPlayer;
    }

    public IEnumerator FindingOneShotEnemyVehicle()
    {
        while (ThisVehicle != null)
        {
            if (Physics.CheckSphere(ThisVehicle.transform.position, BotSettings.oneShotTargetCheckDistance_s))
            {
                foreach (var vehicle in StaticContainer.BattleController.Units.Values)
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

                    if (sqrDist <= BotSettings.oneShotTargetCheckSqrDistance_s && vehicle.HPSystem.Armor < vehicle.Settings[Setting.Damage] && vehicle.IsAvailable)
                    {
                        CurrentBehaviour.Target = vehicle;

                        SetState(OneShotKillState, CurrentState != StuckState);
                        yield break;
                    }
                }
            }

            yield return new WaitForSeconds(Random.Range(2, 4));
        }
    }

    public IEnumerator FireLagging()
    {
        while (ThisVehicle != null)
        {
            isWeaponReady = Random.Range(0, 100) < 70;
            yield return new WaitForSeconds(Random.Range(0.1f, 0.3f));
        }
    }


    public virtual IEnumerator LocalAvoidance()
    {
        yield break;
    }

    public virtual void OnBotPhotonInstantiate()
    {
        //ThisVehicle.Rb.mass *= 100;
    }

    public abstract Vector3 FindRandomPointNearPosition(Vector3 vehiclePos, int radius = 10);
    public abstract void EffectItself(VehicleEffect effect, bool positive);
    public abstract void Move(bool forwards = true);
    public abstract void Move(float speed, float rotSpeed);
    public abstract void OnStateChange();
    public abstract IEnumerator NormalStateUpdating();
    public abstract IEnumerator CloseToSomebodyStateUpdating();
    public abstract IEnumerator OneShotStateUpdating();
    public abstract IEnumerator RevengeStateUpdating();
    public abstract IEnumerator RollbackStateUpdating();
    public abstract IEnumerator StopStateUpdating();
    public abstract IEnumerator StuckStateUpdating();
    public abstract IEnumerator TakingBonusStateUpdating();
    public abstract bool RotateToWaypoint();
    public abstract IEnumerator Fire();
    public abstract IEnumerator PathUpdating();
    public abstract void FindPath();

    #endregion

    protected BotAI(VehicleController vehicleController)
    {
        ThisVehicle = vehicleController;
    }

    ~BotAI()
    {
        Dispatcher.Unsubscribe(EventId.TankTakesDamage, CurrentBehaviour.OnVehicleTakesDamage);
        Dispatcher.Unsubscribe(EventId.TankKilled, CurrentBehaviour.OnVehicleKilled);
        Dispatcher.Unsubscribe(EventId.TankLeftTheGame, CurrentBehaviour.OnVehicleLeftTheGame);
        Dispatcher.Unsubscribe(EventId.BonusDestroyed, CurrentBehaviour.OnBonusDestroyed);
    }

    public void OnBotDestroy()
    {
        BotDispatcher.BotUnsubscribes(this);
    }

    public void OnBotTakesDamage(EventId id, EventInfo ei)
    {        
        if (!PhotonView.isMine)
        {
            return;
        }

        var info = (EventInfo_U)ei;
        int damage = (int)info[1];
        var vehicleData = ThisVehicle.data;

        if ((int)info[0] != vehicleData.playerId)
        {
            return;
        }

        int attackerId = (int)info[2];        

        if (ThisVehicle.IsAvailable && ThisVehicle.HPSystem.Armor <= 0)
        {
            Dispatcher.Send(EventId.TankKilled, new EventInfo_II(ThisVehicle.data.playerId, attackerId), Dispatcher.EventTargetType.ToAll);

            if (attackerId == PhotonNetwork.player.ID)
            {
                StaticContainer.BattleController.Units[attackerId].Event(Message.StatisticUpdate, StatisticParameter.Kills, ThisVehicle.UnitBattle.ID, 1f);
            }
        }

        Dispatcher.Send(EventId.TankHealthChanged, new EventInfo_II(ThisVehicle.data.playerId, ThisVehicle.HPSystem.Armor));
    }

    public void OnBotAimed(EventId id, EventInfo ei)
    {
        var info = (EventInfo_IIB)ei;
        var vehicleId = info.int1;

        if (vehicleId != ThisVehicle.data.playerId)
        {
            return;
        }

        TargetAimed = info.bool1;
    }

    public virtual void Draw()
    {
        CurrentBehaviour.Draw();
    }

    public void OnBotPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (ThisVehicle.IsExploded)
        {
            Debug.LogErrorFormat(ThisVehicle, "{0}: IsExploded!", ThisVehicle.name);
            return;
        }

        if (stream.isWriting)
        {
            stream.SendNext(ThisVehicle.transform.position);
            stream.SendNext(ThisVehicle.transform.rotation);
            stream.SendNext(ThisVehicle.Rb.velocity);

            if (ThisVehicle.Turret)
            {
                stream.SendNext(ThisVehicle.Turret.localEulerAngles.y);
            }
        }
        else
        {
            ThisVehicle.MarkActivity();

            int streamCount = stream.Count - stream.currentItem;

            if (streamCount > 0)
                ThisVehicle.CorrectPosition = (Vector3)stream.ReceiveNext();

            if (streamCount > 1)
                ThisVehicle.CorrectRotation = (Quaternion)stream.ReceiveNext();

            if (streamCount > 2)
                ThisVehicle.CorrectVelocity = (Vector3)stream.ReceiveNext();

            if (streamCount > 3)
                ThisVehicle.CorrectTurretAngle = (float)stream.ReceiveNext();

            if (ThisVehicle.SettingSpawnPosition)
            {
                ThisVehicle.transform.position = ThisVehicle.CorrectPosition;
                ThisVehicle.transform.rotation = ThisVehicle.CorrectRotation;

                if (ThisVehicle.Turret)
                    ThisVehicle.Turret.localEulerAngles = new Vector3(0, ThisVehicle.CurrentCorrection, 0);

                ThisVehicle.SettingSpawnPosition = false;
            }

            ThisVehicle.CurrentCorrection = 0;
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
    
    public void CheckIfFireNeed()
    {
        if (ThisVehicle.IsRequirePrimaryFire && ThisVehicle.PrimaryFire(ThisVehicle.ShotPoint.rotation))
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
}