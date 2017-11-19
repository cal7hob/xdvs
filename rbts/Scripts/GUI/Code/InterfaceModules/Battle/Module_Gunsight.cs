using UnityEngine;

public class Module_Gunsight : InterfaceModuleBase
{
    private enum States
    {
        Static,
        Target,
        ReturnToStatic,
    }

    [SerializeField] GameObject gunsightWrapper;

    [SerializeField] GunsightBehaviour tankGunsightBehaviour;
    [SerializeField] GunsightBehaviour robotGunsightBehaviour;
    [SerializeField] private float lerpTime = 0.5f;
    [SerializeField] private float staticSpeed = 5f;

    private float currentLerpTime;

    private GunsightBehaviour gunsightBehaviour;

    private VehicleController lockedTarget = null;

    private States state = States.Static;
    private States State
    {
        get { return state; }
        set
        {
            if (state != value)
            {
                if (value == States.Static && state == States.Target)
                    state = States.ReturnToStatic;
                else
                    state = value;

                currentLerpTime = 0;

                //Debug.LogError("State: " + state);
            }
        }
    }

    private Camera mainCamera;
    private bool wasGunSightEnabledForTutorialLesson = false;
    private bool instantlySetPosition;
    private bool invisibleMode;

    protected override void Awake()
    {
        base.Awake();

        mainCamera = Camera.main;

        Messenger.Subscribe(EventId.MainTankAppeared, OnMainVehicleAppeared);
        Messenger.Subscribe(EventId.TargetAimed, OnTargetAimed);
        Messenger.Subscribe(EventId.VehicleKilled, OnVehicleKilled);
        Messenger.Subscribe(EventId.MyTankRespawned, OnVehicleRespawned);
        Messenger.Subscribe(EventId.BeforeReconnecting, OnReconnect);
        Messenger.Subscribe(EventId.BattleEnd, OnBattleEnd);
        Messenger.Subscribe(EventId.IsMainCameraSighted, OnMainCameraSighted);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        Messenger.Unsubscribe(EventId.MainTankAppeared, OnMainVehicleAppeared);
        Messenger.Unsubscribe(EventId.TargetAimed, OnTargetAimed);
        Messenger.Unsubscribe(EventId.VehicleKilled, OnVehicleKilled);
        Messenger.Unsubscribe(EventId.MyTankRespawned, OnVehicleRespawned);
        Messenger.Unsubscribe(EventId.BeforeReconnecting, OnReconnect);
        Messenger.Unsubscribe(EventId.BattleEnd, OnBattleEnd);
        Messenger.Unsubscribe(EventId.IsMainCameraSighted, OnMainCameraSighted);
    }

    private void Update()
    {
        if (!BattleController.MyVehicle)
            return;

        // More correct is to rise event when fire lesson appears
        if (!ProfileInfo.IsBattleTutorialCompleted && !wasGunSightEnabledForTutorialLesson && BattleController.MyVehicle.PrimaryFireIsOn)
        {
            wasGunSightEnabledForTutorialLesson = true;
            Show();
        }
    }

    private void LateUpdate()
    {
        if (!IsActive || !BattleController.MyVehicle)
            return;

        if (!BattleController.MyVehicle.IsMain || !BattleController.MyVehicle.IsAvailable)
            return;

        var position = new Vector3();

        float perc = 0;

        if (State == States.Static || State == States.ReturnToStatic)
        {
            position =
                    PositionFrom3DCameraToGUI(
                        BattleController.MyVehicle.AimingPoint.position
                            + BattleController.MyVehicle.AimingPoint.forward * 2000f);
        }

        switch (State)
        {
            case States.Static:
                if (instantlySetPosition)
                {
                    instantlySetPosition = false;
                    gunsightWrapper.transform.position = position;
                    return;
                }

                perc = staticSpeed * Time.smoothDeltaTime;

                break;

            case States.Target:
                if (lockedTarget)
                {
                    position = PositionFrom3DCameraToGUI(BattleController.MyVehicle.TargetPosition);
                }
                else
                    return;

                break;
        }

        if (State == States.Target || State == States.ReturnToStatic)
        {
            currentLerpTime = Mathf.MoveTowards(currentLerpTime, lerpTime, Time.smoothDeltaTime);

            if (State == States.ReturnToStatic && currentLerpTime == lerpTime)
            {
                State = States.Static;
            }

            // Easing interpolant
            perc = Mathf.SmoothStep(0, 1, Mathf.Clamp01(currentLerpTime / lerpTime));
        }

        gunsightWrapper.transform.position =
            Vector2.Lerp(gunsightWrapper.transform.position, position, perc);
    }

    private void Refresh()
    {
        if (!invisibleMode)
            Show();
        else
            Hide();
    }

    private Vector3 PositionFrom3DCameraToGUI(Vector3 position)
    {
        position = mainCamera.WorldToViewportPoint(position);
        position.z = 0;
        position = GameData.CurSceneGuiCamera.ViewportToWorldPoint(position);
        return position;
    }

    private void OnMainVehicleAppeared(EventId id, EventInfo info)
    {
        switch (BattleController.MyVehicle.VehicleType)
        {
            case VehicleInfo.VehicleType.Robot:
                gunsightBehaviour = robotGunsightBehaviour;
                break;
            case VehicleInfo.VehicleType.Tank:
                gunsightBehaviour = tankGunsightBehaviour;
                break;
        }

        instantlySetPosition = true;
        gunsightBehaviour.gameObject.SetActive(true);
        gunsightBehaviour.targetLockGunsight.SetActive(false);

        if (ProfileInfo.IsBattleTutorialCompleted)
            Show();
    }

    private void OnTargetAimed(EventId id, EventInfo ei)
    {
        var info = ei as EventInfo_IIB;

        if (info.int1 != BattleController.MyPlayerId)
            return;

        State = info.bool1 ? States.Target : States.Static;

        lockedTarget = info.bool1 && BattleController.allVehicles.ContainsKey(info.int2) ? BattleController.allVehicles[info.int2] : null;

        gunsightBehaviour.targetLockGunsight.SetActive(lockedTarget != null);
    }

    private void OnVehicleKilled(EventId eid, EventInfo ei)
    {
        var eventInfo = ei as EventInfo_II;

        int victimId = eventInfo.int1;

        if (victimId == BattleController.MyPlayerId)
            Hide();
    }

    private void OnVehicleRespawned(EventId eid, EventInfo ei)
    {
        State = States.Static;
        instantlySetPosition = true;
        Refresh();
    }

    private void OnReconnect(EventId id, EventInfo ei)
    {
        Hide();
    }

    private void OnBattleEnd(EventId id, EventInfo info)
    {
        Hide();
    }

    private void OnMainCameraSighted(EventId eid, EventInfo ei)
    {
        EventInfo_B info = (EventInfo_B)ei;

        invisibleMode = !info.bool1;
        Refresh();
    }
}

