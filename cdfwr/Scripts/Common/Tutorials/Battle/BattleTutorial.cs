using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public abstract class BattleTutorial : MonoBehaviour
{
    public enum BattleLessons
    {
        greetings,
        move,
        turret,
        throttle,
        upDown,
        pickUpBonus,
        fire,
        useDefense,
        killEnemies
    }

    public enum AnimDirections
    {
        straight,
        reversed
    }

    public enum BlinkingMode
    {
        hard,
        smooth
    }

    protected const float BLINKING_INTERVAL = 0.2f;
    protected const float FADING_INTERVAL = 0.7f;
    protected const float DIRECTION_CHECK_TIME = 1.0f;

    protected static BattleLessons currentBattleLesson;

    [SerializeField] protected float goalDistanceMoveLesson;
    [SerializeField] protected int goalShoots;
    [SerializeField] protected int goalBotKills = 3;
    [SerializeField] protected float delayBetweenLessons = 2;
    [SerializeField] protected List<tk2dCameraAnchor> anchors;
    [SerializeField] protected Transform arrow;
    [SerializeField] protected Animation emersion;
    [SerializeField] protected tk2dTextMesh lblTutorMessage;
    [SerializeField] protected SkipLessonButton skipLessonButton;
    [SerializeField] protected BattleTutorial_PC_Part battleTutorial_PC_Part;

    protected bool allBotsDead;
    protected bool bonusLessonIsDone;
    protected int botKills;
    protected GameObject closestBonus;
    protected GameObject closestEnemy;
    protected Rigidbody rb;
    protected IEnumerator bonusFindingRoutine;
    protected IEnumerator enemyFindingRoutine;
    protected IEnumerator checkingIfBonusLessonIsDone;
    protected Queue<AnimDirections> queuedAnimationStates = new Queue<AnimDirections>();
    protected List<IEnumerator> blinkingRoutines = new List<IEnumerator>();

    public bool TutorMessageVisible { get; set; }

    public static BattleTutorial Instance
    {
        get; private set;
    }

    public Animation Emersion { get { return emersion; } }

    public int GoalShoots { get { return goalShoots; } }

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

    public static bool IsCompleted
    {
        get
        {
            return ProfileInfo.accomplishedTutorials[Tutorials.battleTutorial];
        }
    }

    public int GoalBotKills
    {
        get { return GameData.IsGame(Game.CodeOfWar) ? 2 : goalBotKills; }
    }

    public tk2dTextMesh LblTutorMessage { get { return lblTutorMessage; } }

    public virtual string KeyGreetings { get { return "tutorialMessage_5"; } }
    public virtual string KeyMove { get { return "tutorialMessage_86"; } }
    public virtual string KeyTurret { get { return "tutorialMessage_87"; } }
    public virtual string KeyUpDown { get { return "tutorialMessage_7"; } }
    public virtual string KeyPickUpBonus { get { return "tutorialMessage_8"; } }
    public virtual string KeyFire { get { return "tutorialMessage_9"; } }
    public virtual string KeyKillEnemies { get { return "tutorialMessage_11"; } }
    public virtual string KeyReminder { get { return "tutorialMessage_82"; } }
    public virtual VoiceEventKey KeyVoiceReminder { get { return VoiceEventKey.TanksAndFlightHealthbarLesson; } }

    protected Vector3 ArrowPosition
    {
        get
        {
            if (GameData.IsGame(Game.CodeOfWar))
                return new Vector3(0.0f, 3.0f, 9.0f);

            return new Vector3(0.0f, 3.5f, 9.0f);
        }
    }

    protected virtual void Awake()
    {
        Instance = this;

        Dispatcher.Subscribe(EventId.BattleGUIIntialized, Init, 4);
        Dispatcher.Subscribe(EventId.BattleEnd, OnBattleEnd);
        Dispatcher.Subscribe(EventId.TankKilled, CountBotKills);
        Dispatcher.Subscribe(EventId.OnStatTableChangeVisibility, OnStatTableChangeVisibility);
        Dispatcher.Subscribe(EventId.OnExitToHangar, Complete);
    }

    protected virtual void OnDestroy()
    {
        Instance = null;

        Dispatcher.Unsubscribe(EventId.BattleGUIIntialized, Init);
        Dispatcher.Unsubscribe(EventId.BattleEnd, OnBattleEnd);
        Dispatcher.Unsubscribe(EventId.ItemTaken, OnPickUpBonus);
        Dispatcher.Unsubscribe(EventId.TankKilled, CountBotKills);
        Dispatcher.Unsubscribe(EventId.OnStatTableChangeVisibility, OnStatTableChangeVisibility);
        Dispatcher.Unsubscribe(EventId.OnExitToHangar, Complete);
    }

    public void Skip(tk2dUIItem item)
    {
        StopAllCoroutines();
        StartCoroutine(Skipping());
    }

    protected void OnBattleEnd(EventId id, EventInfo info)
    {
        StopAllCoroutines();
        PhotonNetwork.offlineMode = false;
    }

    protected virtual void SetActiveControls(bool activate) 
    {
        GUIControlSpriteGroups.SetActiveSpriteGroups(GUIControlSpriteGroups.UpDownJoystickSprites, activate);

        foreach (var joystick in JoystickManager.Instance.joysticks)
            joystick.IsOn = activate;

        BattleController.MyVehicle.PrimaryFireIsOn = activate;
    }

    protected virtual void Init(EventId id, EventInfo info)
    {
        SetCameraToAnchors();

        rb = BattleController.MyVehicle.gameObject.transform.GetComponent<Rigidbody>();

        GUIControlSpriteGroups.SetActiveSpriteGroups(GUIControlSpriteGroups.AllBlinkingSprites, false);
        GUIControlSpriteGroups.SetActiveTextGroups(GUIControlSpriteGroups.AllTextLines, false);

        SetActiveControls(false);

        ProfileInfo.accomplishedTutorials[Tutorials.battleTutorial] = false;

        StartCoroutine(Tutorial());
    }

    private void SetCameraToAnchors()
    {
        var cam2D = tk2dCamera.Instance.GetComponent<Camera>();

        foreach (var anchor in anchors)
            anchor.AnchorCamera = cam2D;
    }

    protected IEnumerator Tutorial()
    {
        yield return StartCoroutine(BattleGUI.IsTargetPlatformForShowingJoysticks ? Lessons() : battleTutorial_PC_Part.Lessons());

        Complete();
        BattleController.EndBattle(BattleController.EndBattleCause.FinishedTutorial);
    }

    public abstract IEnumerator Lessons();

    public void SetAnimationDirection(AnimDirections direction)
    {
        List<AnimationState> states = new List<AnimationState>(emersion.Cast<AnimationState>());

        switch (direction)
        {
            case AnimDirections.straight:
                states[0].speed = 1;
                states[0].time = 0;
                break;

            case AnimDirections.reversed:
                states[0].speed = -1;
                states[0].time = states[0].length;
                break;
        }  
    }

    public void InitBlinkingRoutines(IEnumerable<tk2dBaseSprite> sprites, BlinkingMode mode)
    {
        blinkingRoutines = new List<IEnumerator>();

        foreach (var sprite in sprites)
        {
            IEnumerator routine;

            switch (mode)
            {
                case BlinkingMode.hard:
                    routine = MiscTools.BlinkingRoutine(sprite, BLINKING_INTERVAL);
                    break;

                case BlinkingMode.smooth:
                    routine = MiscTools.FadingRoutine(sprite, FADING_INTERVAL, 0);
                    break;

                default:
                    routine = MiscTools.BlinkingRoutine(sprite, BLINKING_INTERVAL);
                    break;
            }

            blinkingRoutines.Add(routine);
            StartCoroutine(routine);
        }
    }

    public void StopBlinkingRoutines()
    {
        foreach (var routine in blinkingRoutines)
            StopCoroutine(routine);
    }

    public virtual IEnumerator ShowTutorMessage(string lblName, int voiceEventId = -1)
    {
        yield return new WaitForSeconds(delayBetweenLessons);
        SetTutorialMessage(lblName, voiceEventId);
    }

    protected virtual void SetTutorialMessage(string lblName, int voiceEventId)
    {
        TutorMessageVisible = true;

        lblTutorMessage.text = Localizer.GetText(lblName);

        SetAnimationDirection(AnimDirections.straight);

        emersion.Play();

        Dispatcher.Send(EventId.VoiceRequired, new EventInfo_I(voiceEventId));
    }

    public virtual IEnumerator HideTutorMessage()
    {
        TutorMessageVisible = false;

        SetAnimationDirection(AnimDirections.reversed);

        emersion.Play();

        yield return new WaitForSeconds(emersion.clip.length);
    }

    protected virtual IEnumerator MoveLesson()
    {
        CurrentBattleLesson = BattleLessons.move;

        yield return StartCoroutine(ShowTutorMessage(KeyMove, (int)VoiceEventKey.TankMoveLessonJoystick));

        GUIControlSpriteGroups.SetActiveSpriteGroups(GUIControlSpriteGroups.MoveJoystickSprites, true);

        InitBlinkingRoutines(GUIControlSpriteGroups.MoveJoystickSprites, BlinkingMode.hard);

        JoystickManager.Instance.joysticks[(int)JoystickManager.Joystics.left].IsOn = true;

        yield return StartCoroutine(CheckIfMoveLessonIsDone());
        yield return StartCoroutine(HideTutorMessage());
    }

    public IEnumerator PickUpBonusLesson()
    {
        CurrentBattleLesson = BattleLessons.pickUpBonus;

        checkingIfBonusLessonIsDone = CheckIfPickUpBonusLessonIsDone();

        yield return StartCoroutine(ShowTutorMessage(KeyPickUpBonus, (int) VoiceEventKey.BonusPickUpLesson));
        yield return StartCoroutine(checkingIfBonusLessonIsDone);
        yield return StartCoroutine(HideTutorMessage());
    }

    private IEnumerator GetClosestBonus()
    {
        while (true)
        {
            if (BonusDispatcher.Instance.bonusObjects.Count > 0)
            {
                closestBonus = BonusDispatcher.Instance.bonusObjects[0];

                var minDist = float.MaxValue;

                foreach (var bonus in BonusDispatcher.Instance.bonusObjects)
                {
                    if (bonus == null)
                        continue;

                    var distToCurrentBonus = Vector3.SqrMagnitude(BattleController.MyVehicle.transform.position - bonus.transform.position);

                    if (minDist > distToCurrentBonus)
                    {
                        minDist = distToCurrentBonus;
                        closestBonus = bonus;
                    }
                }
            }

            yield return new WaitForSeconds(DIRECTION_CHECK_TIME);
        }
    }

    private IEnumerator Skipping()
    {
        Dispatcher.Send(EventId.BattleTutorialSkipping, new EventInfo_SimpleEvent());

        if (arrow.gameObject.activeSelf)
            arrow.gameObject.SetActive(false);

        if (TutorMessageVisible)
            yield return StartCoroutine(HideTutorMessage());

        BattleController.EndBattle(BattleController.EndBattleCause.FinishedTutorial);
    }

    protected void OnPickUpBonus(EventId id, EventInfo info)
    {
        if (((EventInfo_III)info).int3 == BattleController.MyPlayerId)
            bonusLessonIsDone = true;
    }

    protected void CountBotKills(EventId id, EventInfo info)
    {
        allBotsDead = (++botKills >= GoalBotKills);
    }

    protected virtual void SetUpArrow()
    {
        arrow.parent = BattleCamera.Instance.transform;
        arrow.localPosition = ArrowPosition;
        arrow.localRotation = Quaternion.LookRotation(Vector3.forward);
    }

    protected void TurnArrow(Transform target)
    {
        arrow.rotation
            = Quaternion.Lerp(
                a:  arrow.rotation,
                b:  Quaternion.LookRotation((target.position - arrow.position).normalized),
                t:  0.1f);
    }

    protected IEnumerator GetClosestEnemy()
    {
        while (true)
        {
            closestEnemy = null;
            var minDist = float.MaxValue;

            foreach (var vehicle in BattleController.allVehicles.Where(vehicle => vehicle.Value.IsAvailable))
            {
                if (vehicle.Value.IsMain || !vehicle.Value.IsAvailable)
                    continue;

                var distToCurrentVehicle = Vector3.SqrMagnitude(BattleController.MyVehicle.transform.position - vehicle.Value.transform.position);

                if (minDist > distToCurrentVehicle)
                {
                    minDist = distToCurrentVehicle;
                    closestEnemy = vehicle.Value.gameObject;
                }
            }

            yield return new WaitForSeconds(DIRECTION_CHECK_TIME);
        }
    }

    protected IEnumerator CheckIfPickUpBonusLessonIsDone()
    {
        bonusFindingRoutine = GetClosestBonus();

        StartCoroutine(bonusFindingRoutine);

        Dispatcher.Subscribe(EventId.ItemTaken, OnPickUpBonus);

        while (closestBonus == null)
            yield return null;

        SetUpArrow();

        while (!bonusLessonIsDone)
        {
            while (closestBonus.gameObject == null)
            {
                if (arrow.gameObject.activeSelf)
                    arrow.gameObject.SetActive(false);

                yield return new WaitForFixedUpdate();
            }

            if (!arrow.gameObject.activeSelf)
                arrow.gameObject.SetActive(true);

            TurnArrow(closestBonus.transform);

            yield return new WaitForFixedUpdate();
        }

        arrow.gameObject.SetActive(false);

        StopCoroutine(bonusFindingRoutine);

        Dispatcher.Send(EventId.BattleLessonAccomplished, new EventInfo_I((int)BattleLessons.pickUpBonus));
        Dispatcher.Unsubscribe(EventId.ItemTaken, OnPickUpBonus);
    }

    public virtual IEnumerator CheckIfMoveLessonIsDone()
    {
        if (!BattleController.MyVehicle)
            yield break;

        var storedPos = BattleController.MyVehicle.transform.position;

        float odometerXZ = 0;

        while (odometerXZ < goalDistanceMoveLesson)
        {
            odometerXZ += Vector3.Magnitude(Vector3.ProjectOnPlane(BattleController.MyVehicle.transform.position - storedPos, Vector3.up));
            storedPos = BattleController.MyVehicle.transform.position;

            yield return null;
        }

        StopBlinkingRoutines();

        MiscTools.SetSpritesAlpha(GUIControlSpriteGroups.MoveJoystickSprites, 1);
        Dispatcher.Send(EventId.BattleLessonAccomplished, new EventInfo_I((int)BattleLessons.move));
    }

    protected virtual IEnumerator FireLesson()
    {
        CurrentBattleLesson = BattleLessons.fire;

        yield return StartCoroutine(ShowTutorMessage(KeyFire, (int)VoiceEventKey.FireLessonJoystick));

        GUIControlSpriteGroups.SetActiveSpriteGroups(GUIControlSpriteGroups.PrimaryFireBtnSprites, true);

        InitBlinkingRoutines(GUIControlSpriteGroups.PrimaryFireBtnSprites, BlinkingMode.hard);

        BattleController.MyVehicle.PrimaryFireIsOn = true;

        GUIControlSpriteGroups.SetActiveSpriteGroups(GUIControlSpriteGroups.GunSightCircleSprites, true);

        while (BattleStatisticsManager.BattleStats["Shoots"] < goalShoots)
            yield return null;

        Dispatcher.Send(EventId.BattleLessonAccomplished, new EventInfo_I((int)BattleLessons.fire));

        StopBlinkingRoutines();

        MiscTools.SetSpritesAlpha(GUIControlSpriteGroups.PrimaryFireBtnSprites, 1);

        yield return StartCoroutine(HideTutorMessage());
    }

    public virtual IEnumerator KillEnemiesLesson()
    {
        GUIControlSpriteGroups.SetActiveSpriteGroups(GUIControlSpriteGroups.GunSightCircleSprites, true);

        for (int i = 0; i < GoalBotKills; i++)
            BotDispatcher.Instance.CreateBotForCurrentProject();

        enemyFindingRoutine = GetClosestEnemy();

        StartCoroutine(enemyFindingRoutine);

        CurrentBattleLesson = BattleLessons.killEnemies;

        yield return StartCoroutine(ShowTutorMessage(KeyKillEnemies, (int)VoiceEventKey.KillEnemiesLesson));

        SetUpArrow();

        while (!allBotsDead)
        {
            while (closestEnemy == null || closestEnemy.gameObject == null)
            {
                if (arrow.gameObject.activeSelf)
                    arrow.gameObject.SetActive(false);

                yield return new WaitForFixedUpdate();
            }

            if (!arrow.gameObject.activeSelf)
                arrow.gameObject.SetActive(true);

            TurnArrow(closestEnemy.transform);

            yield return new WaitForFixedUpdate();
        }

        arrow.gameObject.SetActive(false);

        StopCoroutine(enemyFindingRoutine);

        Dispatcher.Send(EventId.BattleLessonAccomplished, new EventInfo_I((int)BattleLessons.killEnemies));

        yield return StartCoroutine(HideTutorMessage());
    }

    public IEnumerator GreetingsCommander()
    {
        yield return StartCoroutine(ShowTutorMessage(KeyGreetings));
        Dispatcher.Send(EventId.VoiceRequired, new EventInfo_I((int)VoiceEventKey.GreetingsComander));
        yield return new WaitForSeconds(4);

        Dispatcher.Send(EventId.BattleLessonAccomplished, new EventInfo_I((int)BattleLessons.greetings));

        yield return StartCoroutine(HideTutorMessage());
    }

    public IEnumerator ShowReminder()
    {
        yield return StartCoroutine(ShowTutorMessage(KeyReminder, (int)KeyVoiceReminder));
        yield return new WaitForSeconds(10);

        yield return StartCoroutine(HideTutorMessage());
        yield return new WaitForSeconds(emersion.clip.length);
    }

    private void OnStatTableChangeVisibility(EventId id, EventInfo ei)
    {
        EventInfo_B info = (EventInfo_B)ei;

        bool tableIsVisible = info.bool1;

        lblTutorMessage.transform.parent.gameObject.SetActive(!tableIsVisible);

        if (skipLessonButton != null)
            skipLessonButton.gameObject.SetActive(!tableIsVisible);
    }

    protected void Complete(EventId id = 0, EventInfo info = null)
    {
        ProfileInfo.accomplishedTutorials[Tutorials.battleTutorial] = true;

        PhotonNetwork.offlineMode = false;

        BattleStatisticsManager.BattleStats["ProperEndBattle"] = 1;

        ProfileInfo.SaveToServer();

        HangarController.HangarReenter();
    }
}
