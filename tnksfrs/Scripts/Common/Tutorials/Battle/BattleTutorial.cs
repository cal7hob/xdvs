using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace XD
{
    /// <summary>
    /// Tutorial in battle
    /// </summary>
    public abstract class BattleTutorial : MonoBehaviour, IBattleTutorial
    {
        #region IStatic
        public bool IsEmpty
        {
            get
            {
                return false;
            }
        }

        public StaticType StaticType
        {
            get
            {
                return StaticType.BattleTutorial;
            }
        }

        public void SaveInstance()
        {
            StaticContainer.Set(StaticType, this);
        }

        public void DeleteInstance()
        {
            StaticContainer.Set(StaticType, null);
        }
        #endregion

        #region ISender
        public string Description
        {
            get
            {
                return "[BattleTutorial] " + name;
            }

            set
            {
                name = value;
            }
        }

        private List<ISubscriber> subscribers = null;

        public List<ISubscriber> Subscribers
        {
            get
            {
                if (subscribers == null)
                {
                    subscribers = new List<ISubscriber>();
                }
                return subscribers;
            }
        }

        public void AddSubscriber(ISubscriber subscriber)
        {
            if (Subscribers.Contains(subscriber))
            {
                return;
            }
            Subscribers.Add(subscriber);
        }

        public void RemoveSubscriber(ISubscriber subscriber)
        {
            Subscribers.Remove(subscriber);
        }

        public void Event(Message message, params object[] parameters)
        {
            for (int i = 0; i < Subscribers.Count; i++)
            {
                Subscribers[i].Reaction(message, parameters);
            }
        }
        #endregion

        #region ISubscriber       
        public void Reaction(Message message, params object[] parameters)
        {
            switch (message)
            {
                case Message.BattleLayoutInited:
                    battleLayoutInited = true;
                    Init();
                    break;

                case Message.Button:
                    switch (parameters.Get<ButtonKey>())
                    {
                        case ButtonKey.SkipTutor:
                            if (parameters.Get<ButtonMode>() == ButtonMode.Down)
                            {
                                Skip();
                            }
                            break;
                    }
                    break;

                case Message.UnitBattleCreated:
                    IUnitBehaviour unit = parameters.Get<IUnitBehaviour>();

                    if (unit.IsMine)
                    {
                        ApplyUnitRatios(unit);
                        vehicleCreated = true;
                        Init();
                    }
                    break;

                case Message.LoadMapComplete:
                    if (StaticContainer.SceneManager.InBattle)
                    {
                        if (StaticContainer.Profile.BattleTutorialCompleted)
                        {
                            Destroy(gameObject);
                        }
                    }
                    break;

                case Message.QuitToHangar:
                    if (currentRoutine != null)
                    {
                        StopCoroutine(currentRoutine);
                    }
                    break;

                case Message.WindowAppeared:
                    if (currentStep == null)
                    {
                        return;
                    }

                    if (waitForWindow == parameters.Get<PSYWindow>())
                    {
                        SendTutorial(currentStep);
                    }
                    break;
            }
        }
        #endregion

        protected BattleLessons                     currentBattleLesson = BattleLessons.Greetings;

        [SerializeField]
        protected float                             goalDistanceMoveLesson = 10f;
        [SerializeField]
        protected int                               goalShoots = 1;
        [SerializeField]
        protected float                             goalAngleTurretLesson = 35;

        [SerializeField]
        protected float                             angleLookAtTarget = 15;
        [SerializeField]
        private float                               timeLookAtLesson = 0.5f;

        [SerializeField]
        protected Transform                         arrow = null;
        [SerializeField]
        protected TutorStep[]                       steps = null;
        [SerializeField]
        private ButtonKey[]                         denyButtons = null;
        [SerializeField]
        private Axis[]                              denyAxises = null;   

        [Header("Добавить расходники")]
        [SerializeField]
        private int[]                               consumableIds = null;

        [Header("Коэффициенты для параметров нашей техники")]
        [SerializeField]
        private Settings                            ratiosForMe = new Settings();

        [Header("Коэффициенты для параметров ботов")]
        [SerializeField]
        private TutorBotRatios[]                    botRatios = {};

        private int                                 currentStepIndex = -1;
        private TutorStep                           currentStep = null;

        private List<ICheckPoint>                   checkPoints = null;
        private Dictionary<int, ITutorialTarget>    tutorialTargets = null;
        private Coroutine                           currentRoutine = null;
        private bool                                taskWaiter = false;
        private bool                                inited = false;
        private bool                                battleLayoutInited = false;
        private bool                                vehicleCreated = false;
        private IIndicator                          currentMapIndicator = null;


        private PSYWindow                           waitForWindow = PSYWindow.None;

        private bool TaskWaiter
        {
            get
            {
                return taskWaiter;
            }

            set
            {
                taskWaiter = value;
            }
        }

        public int[] ConsumableIds
        {
            get
            {
                return consumableIds;
            }
        }

        public List<ICheckPoint> CheckPoints
        {
            get
            {
                if (checkPoints == null)
                {
                    checkPoints = new List<ICheckPoint>();
                }

                return checkPoints;
            }
        }

        public Dictionary<int, ITutorialTarget> TutorialTargets
        {
            get
            {
                if (tutorialTargets == null)
                {
                    tutorialTargets = new Dictionary<int, ITutorialTarget>();
                }

                return tutorialTargets;
            }
        }

        public bool TutorMessageVisible
        {
            get;
            set;
        }

        public int GoalShoots
        {
            get
            {
                return goalShoots;
            }
        }

        public BattleLessons CurrentBattleLesson
        {
            get
            {
                return currentBattleLesson;
            }
            set
            {
                currentBattleLesson = value;
                Dispatcher.Send(EventId.BattleLessonStarted, new EventInfo_I((int)currentBattleLesson));
            }
        }

        public bool IsCompleted
        {
            get
            {
                var tutorialIndex = (int)Tutorials.BattleTutorial;
                return ProfileInfo.accomplishedTutorials.Contains(tutorialIndex);
            }
        }

        public virtual VoiceEventKey KeyVoiceReminder
        {
            get
            {
                return VoiceEventKey.TanksAndFlightHealthbarLesson;
            }
        }

        protected Vector3 ArrowPosition
        {
            get
            {
                if (GameData.IsGame(Game.Armada2))
                {
                    return new Vector3(0.0f, 3.0f, 9.0f);
                }

                return new Vector3(0.0f, 3.5f, 9.0f);
            }
        }

        protected virtual void Awake()
        {
            DontDestroyOnLoad(this);
            SaveInstance();

            Dispatcher.Subscribe(EventId.BattleEnd, OnBattleEnd);
            Dispatcher.Subscribe(EventId.TankKilled, CountBotKills);

            for (int i = 0; i < botRatios.Length; i++)
            {
                botRatios[i].Ratios.Init();
            }

            ratiosForMe.Init();
        }

        protected virtual void OnDestroy()
        {
            for (int i = 0; i < denyAxises.Length; i++)
            {
                Event(Message.EnableAxis, denyAxises[i], true);
            }

            for (int i = 0; i < denyButtons.Length; i++)
            {
                Event(Message.EnableButton, denyButtons[i], true);
            }

            DeleteInstance();

            Dispatcher.Unsubscribe(EventId.BattleEnd, OnBattleEnd);
            Dispatcher.Unsubscribe(EventId.TankKilled, CountBotKills);

            StaticType.Input.RemoveSubscriber(this);
            StaticType.UI.RemoveSubscriber(this);
            StaticType.SceneManager.RemoveSubscriber(this);
        }

        public void Skip()
        {
            StopAllCoroutines();
            StartCoroutine(Skipping());
        }

        private void Start()
        {
            StaticType.Input.AddSubscriber(this);
            StaticType.UI.AddSubscriber(this);
            StaticType.SceneManager.AddSubscriber(this);

            AddSubscriber(StaticType.Input.Instance());
            AddSubscriber(StaticType.UI.Instance());

            if (StaticContainer.Profile.BattleTutorialCompleted)
            {
                Destroy(gameObject);
            }
        }

        protected void OnBattleEnd(EventId id, EventInfo info)
        {
            StopAllCoroutines();
            PhotonNetwork.offlineMode = false;
        }

        private void ApplyUnitRatios(IUnitBehaviour unit)
        {
            if (vehicleCreated)
            {
                return;
            }

            Setting paramName;

            for (int i = 0; i < ratiosForMe.Count; i++)
            {
                paramName = ratiosForMe.GetName(i);

                if (unit.Settings.Contains(paramName))
                {
                    unit.Settings[paramName].Multiply(ratiosForMe[paramName]);
                }
            }
        }

        protected virtual void Init()
        {
            if (!battleLayoutInited || !vehicleCreated)
            {
                return;
            }

            if (inited)
            {
                return;
            }

            inited = true;

            ProfileInfo.accomplishedTutorials.Remove((int)Tutorials.BattleTutorial);

            for (int i = 0; i < denyAxises.Length; i++)
            {
                Event(Message.EnableAxis, denyAxises[i], false);
            }

            for (int i = 0; i < denyButtons.Length; i++)
            {
                Event(Message.EnableButton, denyButtons[i], false);
            }

            NextStep();
        }

        private TutorBotRatios GetBotRatios(int id)
        {
            for (int i = 0; i < botRatios.Length; i++)
            {
                if (botRatios[i].ID == id)
                {
                    return botRatios[i];
                }
            }

            Debug.LogError("[BattleTutorial] BotRatios " + id + " not fount!");
            return null;
        }

        private void NextStep()
        {
            currentStepIndex++;

            if (steps.Length > currentStepIndex)
            {
                currentStep = steps[currentStepIndex];
                StartCoroutine(DelayBeforeStartStep(currentStep));
                //StartStep(currentStep);
            }
            else
            {
                Complete();
            }
        }

        private void AdditionalActions(TutorStep step)
        {
            for (int i = 0; i < step.EnableTaskTargets.Length; i++)
            {
                ITutorialTarget target = GetTutorialTarget(step.EnableTaskTargets[i]);

                if (target == null)
                {
                    continue;
                }

                target.SetActive(true);
            }

            for (int i = 0; i < step.DisableTaskTargets.Length; i++)
            {
                ITutorialTarget target = GetTutorialTarget(step.DisableTaskTargets[i]);

                if (target == null)
                {
                    continue;
                }

                target.SetActive(false);
            }

            bool isMobile = StaticType.Input.Instance<IInput>().IsMobile;

            TutorUIElemetnActivater element = null;
            for (int i = 0; i < step.EnableElements.Length; i++)
            {
                element = step.EnableElements[i];

                if (element.ForMobile != isMobile)
                {
                    continue;
                }

                //Debug.LogFormat("Send enable element '{0}' to interface by event '{1}".FormatString("color:green"), element.Window, element.WindowElementID);
                Event(Message.WndActivatedAlpha, element.Window, element.Enabled);
            }

            for (int i = 0; i < step.AllowAxises.Length; i++)
            {
                Event(Message.EnableAxis, step.AllowAxises[i], true);
            }

            for (int i = 0; i < step.AllowButtons.Length; i++)
            {
                Event(Message.EnableButton, step.AllowButtons[i], true);
            }
        }

        private IEnumerator DelayBeforeStartStep(TutorStep step)
        {
            RemoveMapIndicator(currentMapIndicator);
            yield return new WaitForSeconds(step.StartDelay);
            StartStep(step);
        }

        private void StartStep(TutorStep step)
        {
            //Debug.LogError("StartStep: " + currentStep.ID);

            if (currentStep.WaitForWindow == PSYWindow.None)
            {
                SendTutorial(step);
            }
            else
            {
                waitForWindow = step.WaitForWindow;
            }

            if (step.CameraQuake)
            {
                StaticType.MainCamera.Instance<IMainCamera>().Quake();
            }

            BuffOn(step);
            ITutorialTarget target = GetTutorialTarget(step.TargetID);
            IEnumerator iEnumerator = null;

            if (step.TargetID != -1)
            {
                if (step.StepType != TutorStepType.CheckPoints)
                {
                    AddMapIndicator(target);
                    currentMapIndicator = target;
                }

                switch (step.StepType)
                {
                    case TutorStepType.Rotate:
                        iEnumerator = CheckIfTurretLessonIsDone();
                        break;

                    case TutorStepType.Move:
                        iEnumerator = MoveLesson();
                        break;

                    case TutorStepType.LookAt:
                        iEnumerator = LookAt(target.Root);
                        break;

                    case TutorStepType.CheckPoints:
                        ICheckPoint[] icheckPointsArray = target.Root.GetComponentsInChildren<ICheckPoint>(true);
                        iEnumerator = CheckPointsLesson(icheckPointsArray.ToList());
                        break;

                    case TutorStepType.Attack:
                        iEnumerator = AttackLesson();
                        break;

                    case TutorStepType.Repair:
                        iEnumerator = RepairLesson();
                        break;

                    case TutorStepType.Shoot:
                        iEnumerator = ShootLesson();
                        break;

                    case TutorStepType.Zoom:
                        iEnumerator = ZoomLesson();
                        break;
                }
            }

            StartCoroutine(WaitForComplete(step, iEnumerator));

            if (target != null)
            {
                target.Complete();
            }
        }

        private void AddMapIndicator(IIndicator target)
        {
            if (!currentStep.ShowOnMinimap)
            {
                return;
            }

            currentMapIndicator = target;

            if (currentMapIndicator != null)
            {
                Event(Message.AddMapIndicator, currentMapIndicator, new List<PSYWindow> { PSYWindow.BattleMiniMap });
            }
        }

        private void RemoveMapIndicator(IIndicator target)
        {
            if (target != null)
            {
                Event(Message.RemoveIndicator, target, new List<PSYWindow> { PSYWindow.BattleMiniMap });
            }

            currentMapIndicator = null;
        }

        private void BuffOn(TutorStep step)
        {
            if (step.BuffOn.Type == Setting.None)
            {
                return;
            }

            IUnitBehaviour unit = StaticContainer.BattleController.CurrentUnit;
            StaticType.BuffDispatcher.Instance<IBuffDispatcher>().AddBuff((VehicleController)unit, step.BuffOn);
        }

        public virtual IEnumerator CheckIfTurretLessonIsDone()
        {
            if (StaticContainer.BattleController.CurrentUnit == null)
            {
                yield break;
            }

            //TaskWaiter = true;
            Quaternion storedRotation = StaticContainer.BattleController.CurrentUnit.Turret.localRotation;
            
            while (Quaternion.Angle(StaticContainer.BattleController.CurrentUnit.Turret.localRotation, storedRotation) > goalAngleTurretLesson)
            {
                yield return null;
            }

            Dispatcher.Send(EventId.BattleLessonAccomplished, new EventInfo_I((int)BattleLessons.Turret));
            TaskWaiter = false;
        }

        private IEnumerator ShootLesson()
        {
            IConsumableBattle curShells = StaticContainer.BattleController.CurrentUnit.GetWeapon(GunShellInfo.ShellType.Usual).consumable;
            int startShells = (int)curShells.Amount;
            //TaskWaiter = true;

            while (curShells.Amount == startShells)
            {
                yield return new WaitForSeconds(1);
            }

            TaskWaiter = false;
        }

        private IEnumerator ZoomLesson()
        {
            IMainCamera cam = StaticType.MainCamera.Instance<IMainCamera>();
            //TaskWaiter = true;
            
            while (!cam.Zoom)
            {
                yield return new WaitForSeconds(0.5f);
            }

            TaskWaiter = false;
        }

        private ITutorialTarget GetTutorialTarget(int id)
        {
            ITutorialTarget target = null;
            if (!TutorialTargets.TryGetValue(id, out target))
            {
                //Debug.LogError(name + " targetID is not found!!! '" + id + "'", null);
            }
            return target;
        }

        private IEnumerator WaitForComplete(TutorStep step, IEnumerator iEnumerator)
        {
            yield return new WaitForSeconds(0.3f);
            AdditionalActions(step);

            ///Debug.LogError("WaitForComplete delay: " + step.CompleteDelay + ", " + step.ID);

            if (iEnumerator != null)
            {
                TaskWaiter = true;
                if (step.CompleteDelay <= 0)
                {
                    currentRoutine = StartCoroutine(iEnumerator);
                    yield return currentRoutine; // ожидается выполнение задачи
                }
                else
                {
                    currentRoutine = StartCoroutine(iEnumerator); // запускается проверка выполнения задачи без ожидания
                }
            }

            if (step.CompleteDelay > 0)
            {
                float timer = 0;

                //Debug.LogError(step.ID + ", CompleteDelay: " + step.CompleteDelay + ", waiter: " + TaskWaiter);
                while (timer < step.CompleteDelay)
                {
                    timer += Time.deltaTime;
                    yield return new WaitForEndOfFrame();

                    if (iEnumerator != null && !TaskWaiter)
                    {
                        break;
                    }
                }
            }

            if (currentRoutine != null)
            {
                StopCoroutine(currentRoutine);
            }

            if (step.Complete)
            {
                Event(Message.TutorialTask, step.ID, step.ElementID, TaskState.End);
            }

            NextStep();
        }

        private IEnumerator LookAt(Transform target)
        {
            Transform turret = StaticContainer.BattleController.CurrentUnit.Turret;
            SetUpArrow();
            float timer = timeLookAtLesson;
            //TaskWaiter = true;

            while (true)
            {
                TurnArrow(target);
                if (Vector3.Angle(turret.forward, target.position - turret.position) < 15f)
                {
                    timer -= Time.deltaTime;
                }
                else
                {
                    timer = timeLookAtLesson;
                }

                if (timer < 0)
                {
                    break;
                }

                yield return null;
            }

            arrow.gameObject.SetActive(false);
            TaskWaiter = false;
        }

        private IEnumerator RepairLesson()
        {
            IUnitBehaviour unit = StaticContainer.BattleController.CurrentUnit;
            WaitForSeconds waiter = new WaitForSeconds(1);

            while (true)
            {
                yield return waiter;

                if (unit.ActiveDebuffs.Count == 0)
                {
                    TaskWaiter = false;
                    yield break;
                }
            }
        }

        private IEnumerator AttackLesson()
        {
            IUnitBehaviour unit = BotDispatcher.Instance.CreateBotForCurrentProject();
            yield return new WaitForSeconds(2f);

            if (currentStep.BotRatiosID != -1)
            {
                Settings botSetts = GetBotRatios(currentStep.BotRatiosID).Ratios;
                Setting paramName;

                for (int i = 0; i < botSetts.Count; i++)
                {
                    paramName = botSetts.GetName(i);

                    if (unit.Settings.Contains(paramName))
                    {
                        switch (paramName)
                        {
                            case Setting.HP:
                                unit.HPSystem.SetArmor(Mathf.RoundToInt(unit.Settings[paramName].Current * botSetts[paramName].Current), -1);
                                break;
                            default:
                                unit.Settings[paramName].Multiply(botSetts[paramName]);
                                break;
                        }
                    }
                }
            }

            if (!GameData.IsGame(Game.Armada2))
            {
                SetAgressorBots();
            }

            CurrentBattleLesson = BattleLessons.KillEnemies;
            arrow.gameObject.SetActive(true);

            while (unit.IsAvailable)
            {
                TurnArrow(unit.Transform);
                yield return new WaitForFixedUpdate();
            }

            arrow.gameObject.SetActive(false);
            Dispatcher.Send(EventId.BattleLessonAccomplished, new EventInfo_I((int)BattleLessons.KillEnemies));
            TaskWaiter = false;
        }

        public void SendTutorial(TutorStep step)
        {
            //Debug.LogError("Tutorial DataResponse next step: " + step.ElementID + ", " + step.Text);
            Event(Message.TutorialTask, step.ID, step.ElementID, step.Text, step.TextDuration, step.TaskText, step.Panel, step.Dark, TaskState.Begin);
        }

        public virtual IEnumerator HideTutorMessage()
        {
            TutorMessageVisible = false;
            yield return new WaitForSeconds(4);
        }

        protected virtual IEnumerator MoveLesson()
        {
            //TaskWaiter = true;
            CurrentBattleLesson = BattleLessons.Move;
            yield return StartCoroutine(CheckIfMoveLessonIsDone());
            TaskWaiter = false;
        }

        private IEnumerator Skipping()
        {
            Complete();
            yield return null;
        }

        protected void CountBotKills(EventId id, EventInfo info)
        {
        }

        protected virtual void SetUpArrow()
        {
            arrow.parent = StaticType.MainCamera.Instance<IMainCamera>().Camera.transform;
            arrow.localPosition = ArrowPosition;
            arrow.localRotation = Quaternion.LookRotation(Vector3.forward);
        }

        protected void TurnArrow(Transform target)
        {
            arrow.rotation
                = Quaternion.Lerp(
                    a: arrow.rotation,
                    b: Quaternion.LookRotation((target.position - arrow.position).normalized),
                    t: 0.1f);

            Vector3 eulerAngles = arrow.eulerAngles;
            eulerAngles.x = 0;
            eulerAngles.z = 0;
            arrow.eulerAngles = eulerAngles;
        }

        public virtual IEnumerator CheckIfMoveLessonIsDone()
        {
            if (StaticContainer.BattleController.CurrentUnit == null)
            {
                Debug.LogError("BattleController.CurrentUnit is null!", this);
                yield break;
            }

            Vector3 storedPos = StaticContainer.BattleController.CurrentUnit.Transform.position;
            float odometerXZ = 0;

            while (odometerXZ < goalDistanceMoveLesson)
            {
                odometerXZ += Vector3.Magnitude(Vector3.ProjectOnPlane(StaticContainer.BattleController.CurrentUnit.Transform.position - storedPos, Vector3.up));
                storedPos = StaticContainer.BattleController.CurrentUnit.Transform.position;

                yield return null;
            }

            Dispatcher.Send(EventId.BattleLessonAccomplished, new EventInfo_I((int)BattleLessons.Move));
        }

        protected virtual IEnumerator FireLesson()
        {
            CurrentBattleLesson = BattleLessons.Fire;

            while (BattleStatisticsManager.BattleStats["Shoots"] < goalShoots)
            {
                yield return null;
            }

            Dispatcher.Send(EventId.BattleLessonAccomplished, new EventInfo_I((int)BattleLessons.Fire));
        }
        
        public virtual IEnumerator CheckPointsLesson(List<ICheckPoint> checkPoints)
        {
            if (checkPoints == null)
            {
                Debug.LogError("Lesson checkPoints is null!!!", this);
                yield break;
            }

            if (checkPoints.Count == 0)
            {
                Debug.LogError("Lesson checkPoints is zero!!!", this);
                yield break;
            }

            checkPoints.Sort(
                delegate (ICheckPoint one, ICheckPoint two)
                {
                    return one.ID.CompareTo(two.ID);
                });

            SetUpArrow();

            bool needIndicator = true;

            while (checkPoints.Count > 0)
            {
                ICheckPoint checkPoint = checkPoints[0];

                if (needIndicator)
                {
                    AddMapIndicator(checkPoint);
                    needIndicator = false;
                }

                if (!arrow.gameObject.activeSelf)
                {
                    arrow.gameObject.SetActive(true);
                    //ColoredDebug.Log("Set arrow enabled ", this, Color.green);
                }

                TurnArrow(checkPoint.Transform);
                checkPoint.SetActive(true);

                if (checkPoint.Check(StaticContainer.BattleController.CurrentUnit.Transform.position))
                {
                    checkPoint.SetActive(false);
                    checkPoint.Apply();
                    checkPoints.RemoveAt(0);
                    RemoveMapIndicator(currentMapIndicator);
                    needIndicator = true;
                    //ColoredDebug.Log("CheckPoint '" + checkPoint.Transform.name + "' checked! Points '" + checkPoints.Count + "'", checkPoint.Transform, Color.green);
                }

                yield return null;
            }

            TaskWaiter = false;
            arrow.gameObject.SetActive(false);
        }

        public virtual void AddTutorialTarget(ITutorialTarget target)
        {
            if (TutorialTargets.ContainsKey(target.ID))
            {
                Debug.LogError("TutorialTargets already Contains Key '" + target.ID + "' names " + target.Root.name, target.Root);
                return;
            }

            TutorialTargets.Add(target.ID, target);
        }

        public virtual void AddCheckPoint(ICheckPoint checkPoint)
        {
            if (checkPoints == null)
            {
                checkPoints = new List<ICheckPoint>();
            }

            int index = checkPoints.Count;
            int minID = 1000;
            for (int i = 0; i < checkPoints.Count; i++)
            {
                if (checkPoints[i].ID < checkPoint.ID)
                {
                    continue;
                }

                if (minID < checkPoints[i].ID)
                {
                    continue;
                }

                minID = checkPoints[i].ID;
                index = i;
            }

            checkPoints.Insert(index, checkPoint);
            //ColoredDebug.Log("CheckPoint '" + checkPoint.Transform.name + "' inserted into '" + index + "' position from '" + checkPoints.Count + "'", checkPoint.Transform, Color.yellow);
        }

        public IEnumerator GreetingsComander()
        {
            Dispatcher.Send(EventId.VoiceRequired, new EventInfo_I((int)VoiceEventKey.GreetingsComander));
            yield return new WaitForSeconds(4);

            Dispatcher.Send(EventId.BattleLessonAccomplished, new EventInfo_I((int)BattleLessons.Greetings));

            yield return StartCoroutine(HideTutorMessage());
            SetTutorialBots();
        }

        public IEnumerator ShowReminder()
        {
            yield return new WaitForSeconds(5);

            yield return StartCoroutine(HideTutorMessage());
            yield return new WaitForSeconds(/*emersion.clip.length*/4);
        }
        
        protected void Complete(EventId id = 0, EventInfo info = null)
        {
            ProfileInfo.TutorialIndex = (int)Tutorials.BattleTutorial + 1;

            if (!ProfileInfo.accomplishedTutorials.Contains((int)Tutorials.BattleTutorial))
            {
                ProfileInfo.accomplishedTutorials.Add((int)Tutorials.BattleTutorial);
            }
            
            BattleStatisticsManager.BattleStats["ProperEndBattle"] = 1;
            ProfileInfo.SaveToServer();
            HangarController.HangarReenter();

            StaticType.BattleController.Instance<IBattleController>().EndBattle(EndBattleCause.FinishedTutorial);

            PhotonNetwork.offlineMode = false;
            Destroy(gameObject);
        }

        protected void SetTutorialBots()
        {
            if (BotDispatcher.Instance == null)
            {
                return;
            }

            foreach (var botAi in BotDispatcher.Instance.botDict)
            {
                botAi.Value.SetBotBehaviour(botAi.Value.TutorialBehaviour);
            }
        }

        private void SetAgressorBots()
        {
            foreach (var botAi in BotDispatcher.Instance.botDict)
            {
                botAi.Value.SetBotBehaviour(botAi.Value.AgressorBehaviour);
                botAi.Value.CurrentBehaviour.Target = StaticContainer.BattleController.CurrentUnit;
            }
        }
    }

    [System.Serializable]
    public class TutorBotRatios
    {
        [SerializeField]
        private int         id = -1;
        [SerializeField]
        private Settings    ratios = new Settings();

        public Settings Ratios
        {
            get
            {
                return ratios;
            }
        }

        public int ID
        {
            get
            {
                return id;
            }
        }
    }
}