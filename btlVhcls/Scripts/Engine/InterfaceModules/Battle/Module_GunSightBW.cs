using UnityEngine;

public class Module_GunSightBW : InterfaceModuleBase, IGunSight
{
    public enum States
    {
        Static,
        Target,
    }

    [SerializeField] private GameObject staticGunSightWrapper;
    [SerializeField] private GameObject targetGunSightWrapper;
    [SerializeField] private ProgressBarSectored targetLockedBar;

    public IProgressBar TargetLockedProgressBar{get { return targetLockedBar; }}

    private States state = States.Static;
    private States State
    {
        get { return state; }
        set
        {
            state = value;
            targetGunSightWrapper.SetActive(state == States.Target);
        }
    }

    private bool wasGunSightEnabledForTutorialLesson = false;

    protected override void Awake()
    {
        base.Awake();
        Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Dispatcher.Subscribe(EventId.BattleEnd, OnBattleEnd);
        Dispatcher.Subscribe(EventId.TargetAimed, OnTargetAimed);
        Dispatcher.Subscribe(EventId.TankKilled, OnTankKilled);
        State = States.Static;//hide target gunsight
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Dispatcher.Unsubscribe(EventId.BattleEnd, OnBattleEnd);
        Dispatcher.Unsubscribe(EventId.TargetAimed, OnTargetAimed);
        Dispatcher.Unsubscribe(EventId.TankKilled, OnTankKilled);
    }

    private void OnMainTankAppeared(EventId id, EventInfo info)
    {
        if(ProfileInfo.IsBattleTutorialCompleted)
            Show();
    }

    private void OnBattleEnd(EventId id, EventInfo info)
    {
        Hide();
    }

    private Vector3 Move(Vector3 position)
    {
        position = Camera.main.WorldToViewportPoint(position);
        position.z = 0;
        position = GameData.CurSceneGuiCamera.ViewportToWorldPoint(position);
        return position;
    }

    public void ShowTargetGunSight(Vector3 position, float distance)
    {
        position = Move(position);

        targetGunSightWrapper.transform.position
            = Vector3.SqrMagnitude(position - wrapper.transform.position) > 40000f
                ? position
                : Vector3.Lerp(wrapper.transform.position, position, 0.1f);
    }

    public void ShowStaticGunSight(Vector3 position)
    {
        staticGunSightWrapper.SetActive(true);
        staticGunSightWrapper.transform.position = Move(position);
    }

    public void HideTargetGunSight()
    {
        targetGunSightWrapper.SetActive(false);
        ((ProgressBarSectored)TargetLockedProgressBar).OnHideGunSight();
    }

    public void HideStaticGunSight()
    {

    }

    private void OnTargetAimed(EventId id, EventInfo ei)
    {
        EventInfo_IIB info = (EventInfo_IIB)ei;

        if (((EventInfo_IIB)ei).int1 != BattleController.MyPlayerId)
            return;

        State = info.bool1 ? States.Target : States.Static;
    }

    private void OnTankKilled(EventId id, EventInfo ei)
    {
        var info = (EventInfo_III)ei;

        if (info.int1 != BattleController.MyPlayerId)//victim
            return;

        staticGunSightWrapper.SetActive(false);
    }

    private void Update()
    {
        if (!BattleController.MyVehicle)
            return;

        //More correct is to rise event when fire lesson appears
        if (!ProfileInfo.IsBattleTutorialCompleted && !wasGunSightEnabledForTutorialLesson && BattleController.MyVehicle.PrimaryFireIsOn)
        {
            wasGunSightEnabledForTutorialLesson = true;
            Show();
        }
    }
}

