using UnityEngine;

public class Module_GunSightIT : InterfaceModuleBase, IGunSight
{
    public enum States
    {
        Standart,
        Zoomed,
    }

    [SerializeField] private tk2dBaseSprite targetGunSight;
    [SerializeField] private float minGunsightScale = 0.8f;
    [SerializeField] private float maxGunsightScale = 1f;


    private States state = States.Standart;
    private States State
    {
        get { return state; }
        set
        {
            state = value;
            targetGunSight.SetSprite("gunsight_" + state.ToString().ToLower());
            if (state == States.Zoomed)
                targetGunSight.scale = new Vector3(2,2,1);
        }
    }

    private bool wasGunSightEnabledForTutorialLesson = false;

    protected override void Awake()
    {
        base.Awake();
        Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Dispatcher.Subscribe(EventId.BattleEnd, OnBattleEnd);
        Dispatcher.Subscribe(EventId.TankKilled, OnTankKilled);
        Dispatcher.Subscribe(EventId.ZoomStateChanged, OnZoomStateChanged);
        State = States.Standart;
        HideTargetGunSight();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Dispatcher.Unsubscribe(EventId.BattleEnd, OnBattleEnd);
        Dispatcher.Unsubscribe(EventId.TankKilled, OnTankKilled);
        Dispatcher.Unsubscribe(EventId.ZoomStateChanged, OnZoomStateChanged);
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

    private void OnZoomStateChanged(EventId id, EventInfo ei)
    {
        EventInfo_B info = (EventInfo_B)ei;
        State = info.bool1 ? States.Zoomed : States.Standart;
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
        if(!targetGunSight.gameObject.activeSelf)
            targetGunSight.gameObject.SetActive(true);

        if (State == States.Standart)
        {
            float koef = Mathf.Clamp(25 / distance, minGunsightScale, maxGunsightScale);
            targetGunSight.scale = new Vector3(2f * koef, 2f * koef, 1);
        }

        position = Move(position);

        targetGunSight.transform.position
            = Vector3.SqrMagnitude(position - wrapper.transform.position) > 40000f
                ? position
                : Vector3.Lerp(wrapper.transform.position, position, 0.1f);
    }

    public void ShowStaticGunSight(Vector3 position)
    {
    }

    public void HideTargetGunSight()
    {
        if (targetGunSight.gameObject.activeSelf)
            targetGunSight.gameObject.SetActive(false);
    }

    public void HideStaticGunSight()
    {
    }

    private void OnTankKilled(EventId id, EventInfo ei)
    {
        var info = (EventInfo_III)ei;

        if (info.int1 != BattleController.MyPlayerId)//victim
            return;

        targetGunSight.gameObject.SetActive(false);
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

    // Not Used
    public IProgressBar TargetLockedProgressBar { get { return null; } }
}

