using System;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using StateMachines;
using UnityEngine;
using UnityEngine.AI;
using XDevs.LiteralKeys;

namespace Bots
{
    public abstract class BotAI : MonoBehaviour, IStateMachineSlave
    {
        protected BotTask myBotRootTask;
        protected BotTask botRootTask;
        

        protected double kickBotAt;

        protected VehicleController slaveController;
        protected VehicleController target;
        protected Vector3 goalPosition;

        protected float botXAxisControl;
        protected float botYAxisControl;
        protected float botTurretAxisControl;
        protected bool isFireBtnPressed;

        public BotBehaviour BotBehaviour { get; private set; }

        public BotSettings BotSettings { get; private set; }

        public BotTask BotRootTask { get { return botRootTask; } }

        public float BotXAxisControl { get { return botXAxisControl; } }

        public float BotYAxisControl { get { return botYAxisControl; } }

        public virtual int CurrentWaypointIndex { get { return 0; } }

        public float BotTurretAxisControl { get { return botTurretAxisControl; } }

        public bool IsFireBtnPressed { get { return isFireBtnPressed; } }

        public VehicleController SlaveController { get { return slaveController; } }

        public VehicleController Target { get { return target; } }

        public BonusItem ClosestBonus { get; protected set; }

        public virtual Vector3 CurrentWaypointPosition { get { return Vector3.zero; } }



        protected abstract Vector3 FindRandomPointNearPosition(Vector3 pos, int radius = 10);

        protected abstract void OnTakesDamage(int attackerId);

        public abstract void FindBotDestinationCloseToTarget();

        public abstract void FindRandomBotDestination();

        public abstract void FindBotDestinationTeam();

        public abstract void BotMove();

        public abstract void UpdatePath();

        public abstract void OnChangeAvailability(bool isAvailable);

        public abstract bool CheckIfPathEnded();

        //virtual: 



        //protected:

        protected void OnBotLeftTheGame(EventId id, EventInfo ei)
        {
            var info = ei as EventInfo_I;
            var goneBotId = info.int1;

            if (target != null && target.data.playerId != goneBotId)
            {
                target = null;
            }
        }

        protected void OnBotRespawned(EventId id, EventInfo ei)
        {
            var info = ei as EventInfo_I;
            var respawnedBotId = info.int1;

            if (slaveController.data.playerId != respawnedBotId)
            {
                return;
            }

            target = null;
        }

        protected void OnBotKilled(EventId id, EventInfo info)
        {
            var ei = info as EventInfo_II;
            var victimId = ei.int1;

            if (victimId == slaveController.data.playerId)
            {
                Stop();
                return;
            }

            if (target != null && victimId == target.data.playerId)
            {
                target = null;
            }
        }

        protected void OnBonusDestroyed(EventId id, EventInfo info)
        {
            
        }

        protected void OnBotTakesDamage(EventId id, EventInfo info)
        {
            var ei = info as EventInfo_U;

            var victimId = (int)ei[0];
            var attackerId = (int)ei[2];

            if (victimId != slaveController.data.playerId)
            {
                return;
            }

            OnTakesDamage(attackerId);
        }

        protected virtual void Awake()
        {
            Dispatcher.Subscribe(EventId.NowImMaster, OnGetMasterStatus);
        }

        protected virtual void OnDestroy()
        {
            Dispatcher.Unsubscribe(EventId.NowImMaster, OnGetMasterStatus);

            Unsubscribes();
        }

        protected virtual void Initialize()
        {
            slaveController = GetComponent<VehicleController>();

            SetBotBehaviour((BotDispatcher.BotBehaviours)slaveController.PhotonView.instantiationData[1]);
            kickBotAt = (double)slaveController.PhotonView.instantiationData[2];
            myBotRootTask = new MyBotRootTask(this);

            if (PhotonNetwork.isMasterClient)
            {
                ReanimateBot(); 
            }
            else
            {
                botRootTask = new ForeignBotRootTask(this);
            }
        }

        protected virtual void OnGetMasterStatus(EventId id, EventInfo ei)
        {
            slaveController.Rb.isKinematic = false;
            slaveController.Rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            botRootTask = myBotRootTask;
            ReanimateBot();
        }

        protected void ReanimateBot()
        {
            botRootTask = myBotRootTask;
            Subscribes();

            slaveController.Rb.mass *= 100;
            BotDispatcher.Instance.RegisterBotAI(this);
        }

        private void SetBotBehaviour(BotDispatcher.BotBehaviours behaviour)
        {
            SetBotSettings(behaviour.ToString());

            switch (behaviour)
            {
                case BotDispatcher.BotBehaviours.Tutorial:
                    BotBehaviour = new TutorialBehaviour(this);
                    break;
                case BotDispatcher.BotBehaviours.Target:
                    BotBehaviour = new TargetBehaviour(this);
                    break;
                case BotDispatcher.BotBehaviours.Fighter:
                    BotBehaviour = new FighterBehaviour(this);
                    break;
                case BotDispatcher.BotBehaviours.Agressor:
                    BotBehaviour = new AgressorBehaviour(this);
                    break;
            }
        }

        private void SetBotSettings(string behaviour)
        {
            BotSettings = Instantiate(
                    Resources.Load<BotSettings>(string.Format("{0}/ScriptableObjects/BotSettings/{1}BotSettings", GameManager.CurrentResourcesFolder, behaviour)));
        }

        public void FindClosestBonus()
        {
            if (!Physics.CheckSphere(slaveController.transform.position, BotSettings.BonusSeekingRadius.RandWithinRange, MiscTools.GetLayerMask(Layer.Key.Bonus), QueryTriggerInteraction.Collide))
            {
                return;
            }

            var minDist = float.MaxValue;
            ClosestBonus = null;

            foreach (var bonusItem in BonusDispatcher.Instance.BonusItems.Where(b => !b.IsTaken))
            {
                var dist = Vector3.SqrMagnitude(slaveController.transform.position - bonusItem.transform.position);

                if (dist < minDist)
                {
                    minDist = dist;
                    ClosestBonus = bonusItem;
                }
            }

            if (ClosestBonus == null)
            {
                return;
            }

            if (!CheckBonusAcessibility())
            {
                ClosestBonus = null;
            }
        }

        protected virtual bool CheckBonusAcessibility()
        {
            return true;
        }

        //public:

        public void FindTarget()
        {
            target = BotBehaviour.FindTarget();
        }

        public void GetBonusPosition()
        {
            goalPosition = ClosestBonus.transform.position;
        }

        public virtual void Subscribes()
        {
            Dispatcher.Subscribe(EventId.TankLeftTheGame, OnBotLeftTheGame);
            Dispatcher.Subscribe(EventId.TankRespawned, OnBotRespawned);
            Dispatcher.Subscribe(EventId.TankKilled, OnBotKilled);
            Dispatcher.Subscribe(EventId.BonusDestroyed, OnBonusDestroyed);
            Dispatcher.Subscribe(EventId.TankTakesDamage, OnBotTakesDamage);
        }

        public virtual void Unsubscribes()
        {
            Dispatcher.Unsubscribe(EventId.TankLeftTheGame, OnBotLeftTheGame);
            Dispatcher.Unsubscribe(EventId.TankRespawned, OnBotRespawned);
            Dispatcher.Unsubscribe(EventId.TankKilled, OnBotKilled);
            Dispatcher.Unsubscribe(EventId.BonusDestroyed, OnBonusDestroyed);
            Dispatcher.Unsubscribe(EventId.TankTakesDamage, OnBotTakesDamage);
        }

        public void OnBotInstantieted()
        {
            Initialize();
        }

        public void SetStartBotProperties()
        {
            Hashtable properties = new Hashtable
            {
                { slaveController[StatisticKey.Health], slaveController.MaxArmor },
                { slaveController[StatisticKey.Score], 0 },
                { slaveController[StatisticKey.Kills], 0 },
                { slaveController[StatisticKey.Deaths], 0 }
            };
            PhotonNetwork.room.SetCustomProperties(properties);
        }

        public void TryGetBotParams()
        {
            slaveController.SetParamToBC(StatisticKey.Health);
            slaveController.SetParamToBC(StatisticKey.Existance);
        }

        public void Stop()
        {
            botXAxisControl = 0;
            botYAxisControl = 0;
        }

        void Update()
        {
            if(slaveController.IsAvailable)
            {
                botRootTask.Update();
            }
        }
    }
}


